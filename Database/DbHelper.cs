using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database
{
    internal static class DbHelper
    {
        public static DataTable LoadTable(string sql, Action<MySqlCommand>? configure = null)
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                return LoadTableOffline(sql, configure);
            }

            try
            {
                using var onlineConn = DBConnection.GetConnection();
                onlineConn.Open();
                using var onlineCmd = new MySqlCommand(sql, onlineConn);
                configure?.Invoke(onlineCmd);
                using var adapter = new MySqlDataAdapter(onlineCmd);
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
            catch (Exception ex) when (TryActivateOfflineFallback(ex, "LoadTable"))
            {
                return LoadTableOffline(sql, configure);
            }
        }

        public static int ExecuteNonQuery(string sql, Action<MySqlCommand>? configure = null)
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                return ExecuteNonQueryOffline(sql, configure);
            }

            try
            {
                using var onlineConn = DBConnection.GetConnection();
                onlineConn.Open();
                using var onlineCmd = new MySqlCommand(sql, onlineConn);
                configure?.Invoke(onlineCmd);
                return onlineCmd.ExecuteNonQuery();
            }
            catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteNonQuery"))
            {
                return ExecuteNonQueryOffline(sql, configure);
            }
        }

        public static T? ExecuteScalar<T>(string sql, Action<MySqlCommand>? configure = null)
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                return ExecuteScalarOffline<T>(sql, configure);
            }

            try
            {
                using var onlineConn = DBConnection.GetConnection();
                onlineConn.Open();
                using var onlineCmd = new MySqlCommand(sql, onlineConn);
                configure?.Invoke(onlineCmd);
                var result = onlineCmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return default;
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteScalar"))
            {
                return ExecuteScalarOffline<T>(sql, configure);
            }
        }

        private static List<MySqlParameter> SnapshotParameters(Action<MySqlCommand>? configure)
        {
            var parameters = new List<MySqlParameter>();
            if (configure == null)
            {
                return parameters;
            }

            using var tempMySql = new MySqlCommand();
            configure(tempMySql);
            foreach (MySqlParameter parameter in tempMySql.Parameters)
            {
                var clone = new MySqlParameter(parameter.ParameterName, parameter.Value ?? DBNull.Value)
                {
                    MySqlDbType = parameter.MySqlDbType
                };
                parameters.Add(clone);
            }

            return parameters;
        }

        private static SqliteCommand CreateSqliteCommand(SqliteConnection conn, string sql, IReadOnlyList<MySqlParameter> parameters)
        {
            var sqliteCommand = conn.CreateCommand();
            sqliteCommand.CommandText = OfflineSqlCompat.NormalizeSql(sql);

            foreach (MySqlParameter parameter in parameters)
            {
                sqliteCommand.Parameters.AddWithValue(
                    parameter.ParameterName,
                    parameter.Value ?? DBNull.Value);
            }

            return sqliteCommand;
        }

        private static DataTable LoadTableOffline(string sql, Action<MySqlCommand>? configure)
        {
            var parameters = SnapshotParameters(configure);
            using var offlineConn = OfflineDatabaseSupport.GetConnection();
            using var offlineCmd = CreateSqliteCommand(offlineConn, sql, parameters);
            using var offlineReader = offlineCmd.ExecuteReader();
            var table = new DataTable();
            table.Load(offlineReader);
            return table;
        }

        private static int ExecuteNonQueryOffline(string sql, Action<MySqlCommand>? configure)
        {
            var parameters = SnapshotParameters(configure);
            using var offlineConn = OfflineDatabaseSupport.GetConnection();
            using var offlineCmd = CreateSqliteCommand(offlineConn, sql, parameters);
            int affected = offlineCmd.ExecuteNonQuery();
            OfflineSyncService.QueueChange(sql, parameters);
            return affected;
        }

        private static T? ExecuteScalarOffline<T>(string sql, Action<MySqlCommand>? configure)
        {
            var parameters = SnapshotParameters(configure);
            using var offlineConn = OfflineDatabaseSupport.GetConnection();
            using var offlineCmd = CreateSqliteCommand(offlineConn, sql, parameters);
            object? result = offlineCmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return default;
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        private static bool TryActivateOfflineFallback(Exception exception, string operationName)
        {
            if (!IsConnectivityFailure(exception))
            {
                return false;
            }

            bool ready = OfflineDatabaseSupport.IsAvailable || OfflineDatabaseSupport.EnsureInitialised();
            if (!ready)
            {
                return false;
            }

            if (!OfflineDatabaseSupport.IsOffline)
            {
                OfflineDatabaseSupport.ActivateOfflineMode();
                AppLogger.LogWarning($"[DbHelper] Switched to offline mode during {operationName}.", exception);
            }

            return true;
        }

        private static bool IsConnectivityFailure(Exception exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is OperationCanceledException)
                {
                    return false;
                }

                if (current is TimeoutException or IOException)
                {
                    return true;
                }

                if (current is MySqlException mySqlEx)
                {
                    if (mySqlEx.Number is -1 or 0 or 1042 or 2002 or 2003 or 2005 or 2013 or 2055)
                    {
                        return true;
                    }

                    string mySqlMessage = mySqlEx.Message ?? string.Empty;
                    if (ContainsConnectivityText(mySqlMessage))
                    {
                        return true;
                    }
                }

                if (ContainsConnectivityText(current.Message ?? string.Empty))
                {
                    return true;
                }
            }

            return ContainsConnectivityText(exception.Message ?? string.Empty);
        }

        private static bool ContainsConnectivityText(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.IndexOf("Unable to connect", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("stream has failed", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("reading from the stream", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("fatal error encountered", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("server is not responding", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("connection from the pool", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
