using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database
{
    /// <summary>
    /// Async database operations for online-ready performance.
    /// Provides non-blocking database access with proper cancellation support.
    /// </summary>
    internal static class DatabaseManagerAsync
    {
        private const int CommandTimeoutSeconds = 30;

        /// <summary>
        /// Asynchronously loads a DataTable from the database.
        /// </summary>
        public static async Task<DataTable> LoadTableAsync(string sql, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default)
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                return await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(false);
            }

            try
            {
                using var conn = DBConnection.GetConnection();
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var cmd = new MySqlCommand(sql, conn);
                cmd.CommandTimeout = CommandTimeoutSeconds;
                configure?.Invoke(cmd);
                using var adapter = new MySqlDataAdapter(cmd);
                var table = new DataTable();
                await Task.Run(() => adapter.Fill(table), cancellationToken).ConfigureAwait(false);
                return table;
            }
            catch (Exception ex) when (TryActivateOfflineFallback(ex, "LoadTableAsync"))
            {
                return await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously executes a non-query command (INSERT, UPDATE, DELETE).
        /// </summary>
        public static async Task<int> ExecuteNonQueryAsync(string sql, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default)
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                return await Task.Run(() => DbHelper.ExecuteNonQuery(sql, configure), cancellationToken).ConfigureAwait(false);
            }

            try
            {
                using var conn = DBConnection.GetConnection();
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var cmd = new MySqlCommand(sql, conn);
                cmd.CommandTimeout = CommandTimeoutSeconds;
                configure?.Invoke(cmd);
                return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteNonQueryAsync"))
            {
                return await Task.Run(() => DbHelper.ExecuteNonQuery(sql, configure), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously executes a scalar query and returns the result.
        /// </summary>
        public static async Task<T?> ExecuteScalarAsync<T>(string sql, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default)
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                return await Task.Run(() => DbHelper.ExecuteScalar<T>(sql, configure), cancellationToken).ConfigureAwait(false);
            }

            try
            {
                using var conn = DBConnection.GetConnection();
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var cmd = new MySqlCommand(sql, conn);
                cmd.CommandTimeout = CommandTimeoutSeconds;
                configure?.Invoke(cmd);
                var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result == null || result == DBNull.Value)
                {
                    return default;
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteScalarAsync"))
            {
                return await Task.Run(() => DbHelper.ExecuteScalar<T>(sql, configure), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously executes a reader query and processes each row.
        /// </summary>
        public static async Task ExecuteReaderAsync(string sql, Func<System.Data.Common.DbDataReader, Task> processRow, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default)
        {
            if (OfflineDatabaseSupport.IsOffline)
            {
                DataTable table = await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(false);
                using var offlineReader = table.CreateDataReader();
                while (offlineReader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await processRow(offlineReader).ConfigureAwait(false);
                }

                return;
            }

            try
            {
                using var conn = DBConnection.GetConnection();
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var cmd = new MySqlCommand(sql, conn);
                cmd.CommandTimeout = CommandTimeoutSeconds;
                configure?.Invoke(cmd);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    await processRow(reader).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteReaderAsync"))
            {
                DataTable table = await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(false);
                using var offlineReader = table.CreateDataReader();
                while (offlineReader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await processRow(offlineReader).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Safely executes a scalar query asynchronously, returning 0 on error.
        /// </summary>
        public static async Task<int> SafeScalarAsync(string sql, CancellationToken cancellationToken = default)
        {
            try
            {
                return await ExecuteScalarAsync<int>(sql, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Safely loads a DataTable asynchronously, returning empty table on error.
        /// </summary>
        public static async Task<DataTable> SafeLoadTableAsync(string sql, CancellationToken cancellationToken = default)
        {
            try
            {
                return await LoadTableAsync(sql, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return new DataTable();
            }
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
                AppLogger.LogWarning(
                    $"[DatabaseManagerAsync] Switched to offline mode during {operationName} after connectivity failure.",
                    exception);
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

                if (current is TimeoutException)
                {
                    return true;
                }

                if (current is MySqlException mySqlEx)
                {
                    if (mySqlEx.Number is -1 or 0 or 1042 or 2002 or 2003 or 2005 or 2013 or 2055)
                    {
                        return true;
                    }

                    if (ContainsConnectivityText(mySqlEx.Message ?? string.Empty))
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
