using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class DbHelper
{
	public static DataTable LoadTable(string sql, Action<MySqlCommand>? configure = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		if (ShouldUseOfflineFastPath())
		{
			return LoadTableOffline(sql, configure);
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand val = new MySqlCommand(sql, connection);
				try
				{
					configure?.Invoke(val);
					MySqlDataAdapter val2 = new MySqlDataAdapter(val);
					try
					{
						DataTable dataTable = new DataTable();
						((DbDataAdapter)(object)val2).Fill(dataTable);
						return dataTable;
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
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception exception) when (TryActivateOfflineFallback(exception, "LoadTable"))
		{
			return LoadTableOffline(sql, configure);
		}
	}

	public static int ExecuteNonQuery(string sql, Action<MySqlCommand>? configure = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		if (ShouldUseOfflineFastPath())
		{
			return ExecuteNonQueryOffline(sql, configure);
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand val = new MySqlCommand(sql, connection);
				try
				{
					configure?.Invoke(val);
					return ((DbCommand)(object)val).ExecuteNonQuery();
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
		catch (Exception exception) when (TryActivateOfflineFallback(exception, "ExecuteNonQuery"))
		{
			return ExecuteNonQueryOffline(sql, configure);
		}
	}

	public static T? ExecuteScalar<T>(string sql, Action<MySqlCommand>? configure = null)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		if (ShouldUseOfflineFastPath())
		{
			return ExecuteScalarOffline<T>(sql, configure);
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand val = new MySqlCommand(sql, connection);
				try
				{
					configure?.Invoke(val);
					object obj = ((DbCommand)(object)val).ExecuteScalar();
					if (obj == null || obj == DBNull.Value)
					{
						return default(T);
					}
					return (T)Convert.ChangeType(obj, typeof(T));
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
		catch (Exception exception) when (TryActivateOfflineFallback(exception, "ExecuteScalar"))
		{
			return ExecuteScalarOffline<T>(sql, configure);
		}
	}

	private static List<MySqlParameter> SnapshotParameters(Action<MySqlCommand>? configure)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		List<MySqlParameter> list = new List<MySqlParameter>();
		if (configure == null)
		{
			return list;
		}
		MySqlCommand val = new MySqlCommand();
		try
		{
			configure(val);
			foreach (MySqlParameter item2 in (DbParameterCollection)(object)val.Parameters)
			{
				MySqlParameter val2 = item2;
				MySqlParameter item = new MySqlParameter(((DbParameter)(object)val2).ParameterName, ((DbParameter)(object)val2).Value ?? DBNull.Value)
				{
					MySqlDbType = val2.MySqlDbType
				};
				list.Add(item);
			}
			return list;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static SqliteCommand CreateSqliteCommand(SqliteConnection conn, string sql, IReadOnlyList<MySqlParameter> parameters)
	{
		SqliteCommand val = conn.CreateCommand();
		((DbCommand)(object)val).CommandText = OfflineSqlCompat.NormalizeSql(sql);
		foreach (MySqlParameter parameter in parameters)
		{
			val.Parameters.AddWithValue(((DbParameter)(object)parameter).ParameterName, ((DbParameter)(object)parameter).Value ?? DBNull.Value);
		}
		return val;
	}

	private static DataTable LoadTableOffline(string sql, Action<MySqlCommand>? configure)
	{
		List<MySqlParameter> parameters = SnapshotParameters(configure);
		SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
		try
		{
			SqliteCommand val = CreateSqliteCommand(connection, sql, parameters);
			try
			{
				SqliteDataReader val2 = val.ExecuteReader();
				try
				{
					DataTable dataTable = new DataTable();
					dataTable.Load((IDataReader)val2);
					return dataTable;
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
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static int ExecuteNonQueryOffline(string sql, Action<MySqlCommand>? configure)
	{
		List<MySqlParameter> parameters = SnapshotParameters(configure);
		SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
		try
		{
			SqliteCommand val = CreateSqliteCommand(connection, sql, parameters);
			try
			{
				int result = ((DbCommand)(object)val).ExecuteNonQuery();
				OfflineSyncService.QueueChange(sql, parameters);
				return result;
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

	private static T? ExecuteScalarOffline<T>(string sql, Action<MySqlCommand>? configure)
	{
		List<MySqlParameter> parameters = SnapshotParameters(configure);
		SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
		try
		{
			SqliteCommand val = CreateSqliteCommand(connection, sql, parameters);
			try
			{
				object obj = ((DbCommand)(object)val).ExecuteScalar();
				if (obj == null || obj == DBNull.Value)
				{
					return default(T);
				}
				return (T)Convert.ChangeType(obj, typeof(T));
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
			AppLogger.LogWarning("[DbHelper] Switched to offline mode during " + operationName + ".", exception);
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
			if ((ex is TimeoutException || ex is IOException) ? true : false)
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
