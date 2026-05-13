using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class OfflineSyncService
{
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

	private static readonly Regex InsertRegex = new Regex("^\\s*(INSERT|REPLACE)\\s+INTO\\s+([`\\w.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex UpdateRegex = new Regex("^\\s*UPDATE\\s+([`\\w.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex DeleteRegex = new Regex("^\\s*DELETE\\s+FROM\\s+([`\\w.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
			SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
			try
			{
				string text = ResolveOperation(sql);
				string text2 = ResolveTableName(sql, text);
				string text3 = SerializeParameters(parameters);
				string text4 = BuildDedupeKey(sql, text3);
				SqliteCommand val = connection.CreateCommand();
				try
				{
					((DbCommand)(object)val).CommandText = "INSERT OR IGNORE INTO sync_queue\r\n(table_name, operation, sql_text, parameter_json, created_at, dedupe_key, retry_count)\r\nVALUES ($tableName, $operation, $sqlText, $parameterJson, CURRENT_TIMESTAMP, $dedupeKey, 0);";
					val.Parameters.AddWithValue("$tableName", (object)text2);
					val.Parameters.AddWithValue("$operation", (object)text);
					val.Parameters.AddWithValue("$sqlText", (object)sql);
					val.Parameters.AddWithValue("$parameterJson", (object)text3);
					val.Parameters.AddWithValue("$dedupeKey", (object)text4);
					((DbCommand)(object)val).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("[OfflineSync] Failed to queue offline change.", ex);
		}
	}

	public static int TrySyncPendingChanges()
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
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
			if (!DBConnection.TryOpenCurrent(out string _))
			{
				return 0;
			}
			SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
			try
			{
				MySqlConnection connection2 = DBConnection.GetConnection();
				try
				{
					((DbConnection)(object)connection2).Open();
					List<QueueItem> list = LoadQueueItems(connection);
					int num = 0;
					foreach (QueueItem item in list)
					{
						try
						{
							MySqlCommand val = new MySqlCommand(item.SqlText, connection2);
							try
							{
								ApplyParameters(val, item.ParameterJson);
								((DbCommand)(object)val).ExecuteNonQuery();
								DeleteQueueItem(connection, item.QueueId);
								num++;
							}
							finally
							{
								((IDisposable)val)?.Dispose();
							}
						}
						catch (Exception ex)
						{
							MarkQueueFailure(connection, item.QueueId, ex);
						}
					}
					return num;
				}
				finally
				{
					((IDisposable)connection2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex2)
		{
			AppLogger.LogWarning("[OfflineSync] Failed to replay queued changes.", ex2);
			return 0;
		}
	}

	public static bool HasPendingChanges()
	{
		try
		{
			if (!OfflineDatabaseSupport.EnsureInitialised())
			{
				return false;
			}
			SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
			try
			{
				SqliteCommand val = connection.CreateCommand();
				try
				{
					((DbCommand)(object)val).CommandText = "SELECT COUNT(*) FROM sync_queue;";
					return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) > 0;
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch
		{
			return false;
		}
	}

	public static void QueueBackgroundSync()
	{
		Task.Run(delegate
		{
			int num = TrySyncPendingChanges();
			if (num > 0)
			{
				AppLogger.LogInfo($"[OfflineSync] Replayed {num} queued change(s) after switching online.");
			}
		});
	}

	private static bool ShouldQueue(string sql)
	{
		if (string.IsNullOrWhiteSpace(sql))
		{
			return false;
		}
		string text = sql.TrimStart();
		if (!text.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return text.IndexOf("sync_queue", StringComparison.OrdinalIgnoreCase) < 0;
	}

	private static string ResolveOperation(string sql)
	{
		string text = sql.TrimStart();
		if (text.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) || text.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase))
		{
			return "INSERT";
		}
		if (text.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
		{
			return "UPDATE";
		}
		if (text.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
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
			_ => Match.Empty, 
		};
		if (!match.Success || (match.Groups.Count < 3 && operation == "INSERT"))
		{
			return "unknown";
		}
		return ((operation == "INSERT") ? match.Groups[2].Value : match.Groups[1].Value).Trim('`');
	}

	private static string SerializeParameters(IReadOnlyList<MySqlParameter> parameters)
	{
		return JsonSerializer.Serialize(parameters.Select(ToRecord).ToList());
	}

	private static SyncParameterRecord ToRecord(MySqlParameter parameter)
	{
		object value = ((DbParameter)(object)parameter).Value;
		if (value == null || value == DBNull.Value)
		{
			return new SyncParameterRecord
			{
				Name = ((DbParameter)(object)parameter).ParameterName,
				Kind = "null",
				Value = null
			};
		}
		if (!(value is bool flag))
		{
			if (!(value is byte b))
			{
				if (!(value is short num))
				{
					if (!(value is int num2))
					{
						if (!(value is long num3))
						{
							if (!(value is decimal num4))
							{
								if (!(value is float num5))
								{
									if (!(value is double num6))
									{
										if (!(value is DateTime dateTime))
										{
											if (value is byte[] inArray)
											{
												return new SyncParameterRecord
												{
													Name = ((DbParameter)(object)parameter).ParameterName,
													Kind = "bytes",
													Value = Convert.ToBase64String(inArray)
												};
											}
											return new SyncParameterRecord
											{
												Name = ((DbParameter)(object)parameter).ParameterName,
												Kind = "string",
												Value = Convert.ToString(value, CultureInfo.InvariantCulture)
											};
										}
										return new SyncParameterRecord
										{
											Name = ((DbParameter)(object)parameter).ParameterName,
											Kind = "datetime",
											Value = dateTime.ToString("o", CultureInfo.InvariantCulture)
										};
									}
									return new SyncParameterRecord
									{
										Name = ((DbParameter)(object)parameter).ParameterName,
										Kind = "double",
										Value = num6.ToString(CultureInfo.InvariantCulture)
									};
								}
								return new SyncParameterRecord
								{
									Name = ((DbParameter)(object)parameter).ParameterName,
									Kind = "float",
									Value = num5.ToString(CultureInfo.InvariantCulture)
								};
							}
							return new SyncParameterRecord
							{
								Name = ((DbParameter)(object)parameter).ParameterName,
								Kind = "decimal",
								Value = num4.ToString(CultureInfo.InvariantCulture)
							};
						}
						return new SyncParameterRecord
						{
							Name = ((DbParameter)(object)parameter).ParameterName,
							Kind = "int64",
							Value = num3.ToString(CultureInfo.InvariantCulture)
						};
					}
					return new SyncParameterRecord
					{
						Name = ((DbParameter)(object)parameter).ParameterName,
						Kind = "int32",
						Value = num2.ToString(CultureInfo.InvariantCulture)
					};
				}
				return new SyncParameterRecord
				{
					Name = ((DbParameter)(object)parameter).ParameterName,
					Kind = "int16",
					Value = num.ToString(CultureInfo.InvariantCulture)
				};
			}
			return new SyncParameterRecord
			{
				Name = ((DbParameter)(object)parameter).ParameterName,
				Kind = "byte",
				Value = b.ToString(CultureInfo.InvariantCulture)
			};
		}
		return new SyncParameterRecord
		{
			Name = ((DbParameter)(object)parameter).ParameterName,
			Kind = "bool",
			Value = (flag ? "1" : "0")
		};
	}

	private static void ApplyParameters(MySqlCommand cmd, string? parameterJson)
	{
		if (string.IsNullOrWhiteSpace(parameterJson))
		{
			return;
		}
		List<SyncParameterRecord> list = JsonSerializer.Deserialize<List<SyncParameterRecord>>(parameterJson);
		if (list == null)
		{
			return;
		}
		foreach (SyncParameterRecord item in list)
		{
			cmd.Parameters.AddWithValue(item.Name, FromRecord(item));
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
			return string.Equals(record.Value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(record.Value, "true", StringComparison.OrdinalIgnoreCase);
		}
		if (string.Equals(record.Kind, "byte", StringComparison.OrdinalIgnoreCase))
		{
			if (!byte.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				return DBNull.Value;
			}
			return result;
		}
		if (string.Equals(record.Kind, "int16", StringComparison.OrdinalIgnoreCase))
		{
			if (!short.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
			{
				return DBNull.Value;
			}
			return result2;
		}
		if (string.Equals(record.Kind, "int32", StringComparison.OrdinalIgnoreCase))
		{
			if (!int.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result3))
			{
				return DBNull.Value;
			}
			return result3;
		}
		if (string.Equals(record.Kind, "int64", StringComparison.OrdinalIgnoreCase))
		{
			if (!long.TryParse(record.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result4))
			{
				return DBNull.Value;
			}
			return result4;
		}
		if (string.Equals(record.Kind, "decimal", StringComparison.OrdinalIgnoreCase))
		{
			if (!decimal.TryParse(record.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result5))
			{
				return DBNull.Value;
			}
			return result5;
		}
		if (string.Equals(record.Kind, "float", StringComparison.OrdinalIgnoreCase))
		{
			if (!float.TryParse(record.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result6))
			{
				return DBNull.Value;
			}
			return result6;
		}
		if (string.Equals(record.Kind, "double", StringComparison.OrdinalIgnoreCase))
		{
			if (!double.TryParse(record.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result7))
			{
				return DBNull.Value;
			}
			return result7;
		}
		if (string.Equals(record.Kind, "datetime", StringComparison.OrdinalIgnoreCase))
		{
			if (!DateTime.TryParse(record.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result8))
			{
				return DBNull.Value;
			}
			return result8;
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
		string s = sql + "\n" + parameterJson + "\n" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + Guid.NewGuid().ToString("N");
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
	}

	private static List<QueueItem> LoadQueueItems(SqliteConnection conn)
	{
		List<QueueItem> list = new List<QueueItem>();
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT queue_id, sql_text, parameter_json\r\nFROM sync_queue\r\nORDER BY queue_id ASC";
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					list.Add(new QueueItem
					{
						QueueId = ((DbDataReader)(object)val2).GetInt64(0),
						SqlText = (((DbDataReader)(object)val2).IsDBNull(1) ? string.Empty : ((DbDataReader)(object)val2).GetString(1)),
						ParameterJson = (((DbDataReader)(object)val2).IsDBNull(2) ? null : ((DbDataReader)(object)val2).GetString(2))
					});
				}
				return list;
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void DeleteQueueItem(SqliteConnection conn, long queueId)
	{
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "DELETE FROM sync_queue WHERE queue_id = $id";
			val.Parameters.AddWithValue("$id", (object)queueId);
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void MarkQueueFailure(SqliteConnection conn, long queueId, Exception ex)
	{
		string text = ex.Message;
		if (text.Length > 900)
		{
			text = text.Substring(0, 900);
		}
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "UPDATE sync_queue\r\nSET retry_count = IFNULL(retry_count, 0) + 1,\r\n    last_error = $error\r\nWHERE queue_id = $id";
			val.Parameters.AddWithValue("$error", (object)text);
			val.Parameters.AddWithValue("$id", (object)queueId);
			((DbCommand)(object)val).ExecuteNonQuery();
			AppLogger.LogWarning($"[OfflineSync] Queue item {queueId} failed and will retry later. {text}");
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
