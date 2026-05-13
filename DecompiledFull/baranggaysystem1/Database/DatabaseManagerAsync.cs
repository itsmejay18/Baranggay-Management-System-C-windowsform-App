using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class DatabaseManagerAsync
{
	private const int CommandTimeoutSeconds = 30;

	public static async Task<DataTable> LoadTableAsync(string sql, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (ShouldUseOfflineFastPath())
		{
			return await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		try
		{
			MySqlConnection conn = DBConnection.GetConnection();
			try
			{
				await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand cmd = new MySqlCommand(sql, conn);
				try
				{
					((DbCommand)(object)cmd).CommandTimeout = 30;
					configure?.Invoke(cmd);
					MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
					try
					{
						DataTable table = new DataTable();
						await Task.Run(() => ((DbDataAdapter)(object)adapter).Fill(table), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						return table;
					}
					finally
					{
						if (adapter != null)
						{
							((IDisposable)adapter).Dispose();
						}
					}
				}
				finally
				{
					((IDisposable)cmd)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)conn)?.Dispose();
			}
		}
		catch (Exception ex) when (TryActivateOfflineFallback(ex, "LoadTableAsync"))
		{
			return await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public static async Task<int> ExecuteNonQueryAsync(string sql, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (ShouldUseOfflineFastPath())
		{
			return await Task.Run(() => DbHelper.ExecuteNonQuery(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		try
		{
			MySqlConnection conn = DBConnection.GetConnection();
			try
			{
				await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand cmd = new MySqlCommand(sql, conn);
				try
				{
					((DbCommand)(object)cmd).CommandTimeout = 30;
					configure?.Invoke(cmd);
					return await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					((IDisposable)cmd)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)conn)?.Dispose();
			}
		}
		catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteNonQueryAsync"))
		{
			return await Task.Run(() => DbHelper.ExecuteNonQuery(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public static async Task<T?> ExecuteScalarAsync<T>(string sql, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (ShouldUseOfflineFastPath())
		{
			return await Task.Run(() => DbHelper.ExecuteScalar<T>(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		try
		{
			MySqlConnection conn = DBConnection.GetConnection();
			try
			{
				await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand cmd = new MySqlCommand(sql, conn);
				try
				{
					((DbCommand)(object)cmd).CommandTimeout = 30;
					configure?.Invoke(cmd);
					object obj = await ((DbCommand)(object)cmd).ExecuteScalarAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					if (obj == null || obj == DBNull.Value)
					{
						return default(T);
					}
					return (T)Convert.ChangeType(obj, typeof(T));
				}
				finally
				{
					((IDisposable)cmd)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)conn)?.Dispose();
			}
		}
		catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteScalarAsync"))
		{
			return await Task.Run(() => DbHelper.ExecuteScalar<T>(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public static async Task ExecuteReaderAsync(string sql, Func<DbDataReader, Task> processRow, Action<MySqlCommand>? configure = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (ShouldUseOfflineFastPath())
		{
			using (DataTableReader offlineReader = (await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).CreateDataReader())
			{
				while (offlineReader.Read())
				{
					cancellationToken.ThrowIfCancellationRequested();
					await processRow(offlineReader).ConfigureAwait(continueOnCapturedContext: false);
				}
				return;
			}
		}
		try
		{
			MySqlConnection conn = DBConnection.GetConnection();
			try
			{
				await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand cmd = new MySqlCommand(sql, conn);
				try
				{
					((DbCommand)(object)cmd).CommandTimeout = 30;
					configure?.Invoke(cmd);
					using DbDataReader reader = await ((DbCommand)(object)cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					while (await reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
					{
						await processRow(reader).ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				finally
				{
					((IDisposable)cmd)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)conn)?.Dispose();
			}
		}
		catch (Exception ex) when (TryActivateOfflineFallback(ex, "ExecuteReaderAsync"))
		{
			using DataTableReader offlineReader = (await Task.Run(() => DbHelper.LoadTable(sql, configure), cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).CreateDataReader();
			while (offlineReader.Read())
			{
				cancellationToken.ThrowIfCancellationRequested();
				await processRow(offlineReader).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	public static async Task<int> SafeScalarAsync(string sql, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			return await ExecuteScalarAsync<int>(sql, null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			return 0;
		}
	}

	public static async Task<DataTable> SafeLoadTableAsync(string sql, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			return await LoadTableAsync(sql, null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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
		DBConnection.RegisterConnectivityFailure(exception);
		if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
		{
			return false;
		}
		if (!OfflineDatabaseSupport.IsOffline)
		{
			OfflineDatabaseSupport.ActivateOfflineMode();
			AppLogger.LogWarning("[DatabaseManagerAsync] Switched to offline mode during " + operationName + " after connectivity failure.", exception);
		}
		return true;
	}

	private static bool ShouldUseOfflineFastPath()
	{
		if (OfflineDatabaseSupport.IsOffline)
		{
			return true;
		}
		if (!DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false))
		{
			return false;
		}
		if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
		{
			return false;
		}
		if (!OfflineDatabaseSupport.IsOffline)
		{
			OfflineDatabaseSupport.ActivateOfflineMode();
		}
		return true;
	}

	private static bool IsConnectivityFailure(Exception exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			if (ex is OperationCanceledException)
			{
				return false;
			}
			if (ex is TimeoutException)
			{
				return true;
			}
			if (IsPoolExhaustion(ex))
			{
				return false;
			}
			MySqlException ex2 = (MySqlException)(object)((ex is MySqlException) ? ex : null);
			if (ex2 != null)
			{
				bool flag;
				switch (ex2.Number)
				{
				case -1:
				case 0:
				case 1042:
				case 2002:
				case 2003:
				case 2005:
				case 2013:
				case 2055:
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (flag)
				{
					return true;
				}
				if (ContainsConnectivityText(((Exception)(object)ex2).Message ?? string.Empty))
				{
					return true;
				}
			}
			if (ContainsConnectivityText(ex.Message ?? string.Empty))
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
		if (message.IndexOf("Unable to connect", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("stream has failed", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("reading from the stream", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("fatal error encountered", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("server is not responding", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return message.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static bool IsPoolExhaustion(Exception exception)
	{
		string text = exception.Message ?? string.Empty;
		if (text.IndexOf("max pool size", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("obtaining a connection from the pool", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return text.IndexOf("connection from the pool", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}
}
