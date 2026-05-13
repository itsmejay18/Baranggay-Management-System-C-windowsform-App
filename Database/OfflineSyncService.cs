using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database
{
    internal static class OfflineSyncService
    {
        private static readonly Regex InsertRegex = new(@"^\s*(INSERT|REPLACE)\s+INTO\s+([`\w.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex UpdateRegex = new(@"^\s*UPDATE\s+([`\w.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DeleteRegex = new(@"^\s*DELETE\s+FROM\s+([`\w.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void QueueChange(string sql, IReadOnlyList<MySqlParameter> parameters)
        {
            if (!ShouldQueue(sql))
            {
                return;
            }

            try
            {
                if (!OfflineDatabaseSupport.EnsureInitialised())
                {
                    return;
                }

                using var conn = OfflineDatabaseSupport.GetConnection();
                string operation = ResolveOperation(sql);
                string table = ResolveTableName(sql, operation);
                string parameterJson = SerializeParameters(parameters);
                string dedupeKey = BuildDedupeKey(sql, parameterJson);

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT OR IGNORE INTO sync_queue
(table_name, operation, sql_text, parameter_json, created_at, dedupe_key, retry_count)
VALUES ($tableName, $operation, $sqlText, $parameterJson, CURRENT_TIMESTAMP, $dedupeKey, 0);";
                cmd.Parameters.AddWithValue("$tableName", table);
                cmd.Parameters.AddWithValue("$operation", operation);
                cmd.Parameters.AddWithValue("$sqlText", sql);
                cmd.Parameters.AddWithValue("$parameterJson", parameterJson);
                cmd.Parameters.AddWithValue("$dedupeKey", dedupeKey);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning("[OfflineSync] Failed to queue offline change.", ex);
            }
        }

        public static int TrySyncPendingChanges()
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                return 0;
            }

            try
            {
                if (!OfflineDatabaseSupport.EnsureInitialised())
                {
                    return 0;
                }

                if (!DBConnection.TryOpenCurrent(out _))
                {
                    return 0;
                }

                using var sqliteConn = OfflineDatabaseSupport.GetConnection();
                using var mysqlConn = DBConnection.GetConnection();
                mysqlConn.Open();

                List<QueueItem> queueItems = LoadQueueItems(sqliteConn);
                int synced = 0;

                foreach (QueueItem item in queueItems)
                {
                    try
                    {
                        using var cmd = new MySqlCommand(item.SqlText, mysqlConn);
                        ApplyParameters(cmd, item.ParameterJson);
                        cmd.ExecuteNonQuery();
                        DeleteQueueItem(sqliteConn, item.QueueId);
                        synced++;
                    }
                    catch (Exception ex)
                    {
                        MarkQueueFailure(sqliteConn, item.QueueId, ex);
                    }
                }

                return synced;
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning("[OfflineSync] Failed to replay queued changes.", ex);
                return 0;
            }
        }

        private static bool ShouldQueue(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return false;
            }

            string trimmed = sql.TrimStart();
            bool isWrite = trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                           || trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                           || trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
                           || trimmed.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase);

            if (!isWrite)
            {
                return false;
            }

            return trimmed.IndexOf("sync_queue", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static string ResolveOperation(string sql)
        {
            string trimmed = sql.TrimStart();
            if (trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase))
            {
                return "INSERT";
            }

            if (trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                return "UPDATE";
            }

            if (trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                return "DELETE";
            }

            return "UNKNOWN";
        }

        private static string ResolveTableName(string sql, string operation)
        {
            Match match = operation switch
            {
                "INSERT" => InsertRegex.Match(sql),
                "UPDATE" => UpdateRegex.Match(sql),
                "DELETE" => DeleteRegex.Match(sql),
                _ => Match.Empty
            };

            if (!match.Success || match.Groups.Count < 3 && operation == "INSERT")
            {
                return "unknown";
            }

            string raw = operation == "INSERT"
                ? match.Groups[2].Value
                : match.Groups[1].Value;

            return raw.Trim('`');
        }

        private static string SerializeParameters(IReadOnlyList<MySqlParameter> parameters)
        {
            var records = parameters.Select(ToRecord).ToList();
            return JsonSerializer.Serialize(records);
        }

        private static SyncParameterRecord ToRecord(MySqlParameter parameter)
        {
            object? value = parameter.Value;
            if (value == null || value == DBNull.Value)
            {
                return new SyncParameterRecord
                {
                    Name = parameter.ParameterName,
                    Kind = "null",
                    Value = null
                };
            }

            return value switch
            {
                bool b => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "bool", Value = b ? "1" : "0" },
                byte bt => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "byte", Value = bt.ToString(CultureInfo.InvariantCulture) },
                short s => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "int16", Value = s.ToString(CultureInfo.InvariantCulture) },
                int i => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "int32", Value = i.ToString(CultureInfo.InvariantCulture) },
                long l => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "int64", Value = l.ToString(CultureInfo.InvariantCulture) },
                decimal d => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "decimal", Value = d.ToString(CultureInfo.InvariantCulture) },
                float f => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "float", Value = f.ToString(CultureInfo.InvariantCulture) },
                double db => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "double", Value = db.ToString(CultureInfo.InvariantCulture) },
                DateTime dt => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "datetime", Value = dt.ToString("o", CultureInfo.InvariantCulture) },
                byte[] bytes => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "bytes", Value = Convert.ToBase64String(bytes) },
                _ => new SyncParameterRecord { Name = parameter.ParameterName, Kind = "string", Value = Convert.ToString(value, CultureInfo.InvariantCulture) }
            };
        }

        private static void ApplyParameters(MySqlCommand cmd, string? parameterJson)
        {
            if (string.IsNullOrWhiteSpace(parameterJson))
            {
                return;
            }

            List<SyncParameterRecord>? records = JsonSerializer.Deserialize<List<SyncParameterRecord>>(parameterJson);
            if (records == null)
            {
                return;
            }

            foreach (SyncParameterRecord record in records)
            {
                cmd.Parameters.AddWithValue(record.Name, FromRecord(record));
            }
        }

        private static object FromRecord(SyncParameterRecord record)
        {
            if (string.Equals(record.Kind, "null", StringComparison.OrdinalIgnoreCase))
            {
                return DBNull.Value;
            }

            if (string.Equals(record.Kind, "bool", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(record.Value, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(record.Value, "true", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(record.Kind, "byte", StringComparison.OrdinalIgnoreCase))
            {
                return byte.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value) ? value : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "int16", StringComparison.OrdinalIgnoreCase))
            {
                return short.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short value) ? value : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "int32", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "int64", StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "decimal", StringComparison.OrdinalIgnoreCase))
            {
                return decimal.TryParse(record.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "float", StringComparison.OrdinalIgnoreCase))
            {
                return float.TryParse(record.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "double", StringComparison.OrdinalIgnoreCase))
            {
                return double.TryParse(record.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "datetime", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.TryParse(record.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime value)
                    ? value
                    : (object)DBNull.Value;
            }

            if (string.Equals(record.Kind, "bytes", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(record.Value))
                {
                    return Array.Empty<byte>();
                }

                return Convert.FromBase64String(record.Value);
            }

            return record.Value ?? string.Empty;
        }

        private static string BuildDedupeKey(string sql, string parameterJson)
        {
            string input = sql + "\n" + parameterJson + "\n" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + Guid.NewGuid().ToString("N");
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static List<QueueItem> LoadQueueItems(SqliteConnection conn)
        {
            var items = new List<QueueItem>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT queue_id, sql_text, parameter_json
FROM sync_queue
ORDER BY queue_id ASC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new QueueItem
                {
                    QueueId = reader.GetInt64(0),
                    SqlText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    ParameterJson = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }

            return items;
        }

        private static void DeleteQueueItem(SqliteConnection conn, long queueId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM sync_queue WHERE queue_id = $id";
            cmd.Parameters.AddWithValue("$id", queueId);
            cmd.ExecuteNonQuery();
        }

        private static void MarkQueueFailure(SqliteConnection conn, long queueId, Exception ex)
        {
            string message = ex.Message;
            if (message.Length > 900)
            {
                message = message.Substring(0, 900);
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE sync_queue
SET retry_count = IFNULL(retry_count, 0) + 1,
    last_error = $error
WHERE queue_id = $id";
            cmd.Parameters.AddWithValue("$error", message);
            cmd.Parameters.AddWithValue("$id", queueId);
            cmd.ExecuteNonQuery();

            AppLogger.LogWarning($"[OfflineSync] Queue item {queueId} failed and will retry later. {message}");
        }

        private sealed class QueueItem
        {
            public long QueueId { get; set; }
            public string SqlText { get; set; } = string.Empty;
            public string? ParameterJson { get; set; }
        }

        private sealed class SyncParameterRecord
        {
            public string Name { get; set; } = string.Empty;
            public string Kind { get; set; } = "string";
            public string? Value { get; set; }
        }
    }
}
