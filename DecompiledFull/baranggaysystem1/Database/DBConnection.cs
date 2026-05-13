using System;
using System.Data.Common;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal class DBConnection
{
	private const string DefaultDatabase = "barangay_system";

	private const string DefaultUser = "root";

	private const string DefaultPassword = "";

	private const string BootstrapConnectionString = "server=srv1237.hstgr.io;port=3306;database=u621755393_CBaranggayMana;user id=u621755393_cbaranggay;password=Dssc@2026;SslMode=Disabled;AllowPublicKeyRetrieval=true;AllowUserVariables=true;ConnectionTimeout=5";

	private const string EnvDbConnection = "BARANGAY_DB_CONNECTION";

	private const string EnvAllowLocalFallback = "BARANGAY_ALLOW_LOCAL_FALLBACK";

	private const uint DefaultPort = 3306u;

	private const uint AlternatePort = 3307u;

	private const uint ConnectionTimeoutSeconds = 15u;

	private const uint MinimumHealthyMaxPoolSize = 25u;

	private const uint ConnectionLifeTimeSeconds = 180u;

	private static readonly TimeSpan ConnectivityFailureCooldown = TimeSpan.FromSeconds(45.0);

	private static readonly object SyncRoot = new object();

	private static string? resolvedConnectionString;

	private static string? runtimeConnectionString;

	private static DateTime? lastConnectivityFailureUtc;

	private static bool runtimeSqliteSelection;

	private static string ResolveConnectionString()
	{
		lock (SyncRoot)
		{
			if (!string.IsNullOrWhiteSpace(resolvedConnectionString))
			{
				return resolvedConnectionString;
			}
		}
		string text = null;
		lock (SyncRoot)
		{
			text = runtimeConnectionString;
		}
		string errorMessage;
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = NormalizeConnectionString(text);
			if (TryResolveWorkingConnectionString(text, out string workingConnectionString, out errorMessage))
			{
				return Cache(workingConnectionString);
			}
		}
		string text2 = Environment.GetEnvironmentVariable("BARANGAY_DB_CONNECTION");
		if (!string.IsNullOrWhiteSpace(text2))
		{
			text2 = NormalizeConnectionString(text2);
			if (TryResolveWorkingConnectionString(text2, out string workingConnectionString2, out errorMessage))
			{
				return Cache(workingConnectionString2);
			}
		}
		if (DbConnectionSettingsStore.TryLoad(out DatabaseConnectionProfile profile))
		{
			string text3 = NormalizeConnectionString(DbConnectionSettingsStore.BuildConnectionString(profile));
			if (TryResolveWorkingConnectionString(text3, out string workingConnectionString3, out errorMessage))
			{
				return Cache(workingConnectionString3);
			}
			return Cache(text3);
		}
		string text4 = NormalizeConnectionString("server=srv1237.hstgr.io;port=3306;database=u621755393_CBaranggayMana;user id=u621755393_cbaranggay;password=Dssc@2026;SslMode=Disabled;AllowPublicKeyRetrieval=true;AllowUserVariables=true;ConnectionTimeout=5");
		if (TryResolveWorkingConnectionString(text4, out string workingConnectionString4, out errorMessage))
		{
			return Cache(workingConnectionString4);
		}
		if (AllowLocalFallback())
		{
			foreach (string item in new string[4]
			{
				BuildCandidate("localhost", 3306u, "root", ""),
				BuildCandidate("127.0.0.1", 3306u, "root", ""),
				BuildCandidate("localhost", 3307u, "root", ""),
				BuildCandidate("127.0.0.1", 3307u, "root", "")
			}.Distinct<string>(StringComparer.OrdinalIgnoreCase))
			{
				if (TryResolveWorkingConnectionString(item, out string workingConnectionString5, out errorMessage))
				{
					return Cache(workingConnectionString5);
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return Cache(text);
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return Cache(text2);
		}
		if (AllowLocalFallback())
		{
			return Cache(BuildCandidate("localhost", 3306u, "root", ""));
		}
		return Cache(text4);
	}

	public static DatabaseConnectionProfile GetBootstrapProfile()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		try
		{
			MySqlConnectionStringBuilder val = new MySqlConnectionStringBuilder("server=srv1237.hstgr.io;port=3306;database=u621755393_CBaranggayMana;user id=u621755393_cbaranggay;password=Dssc@2026;SslMode=Disabled;AllowPublicKeyRetrieval=true;AllowUserVariables=true;ConnectionTimeout=5", false);
			return new DatabaseConnectionProfile
			{
				Server = ((MySqlBaseConnectionStringBuilder)val).Server,
				Port = ((MySqlBaseConnectionStringBuilder)val).Port,
				Database = ((MySqlBaseConnectionStringBuilder)val).Database,
				Username = ((MySqlBaseConnectionStringBuilder)val).UserID,
				Password = ((MySqlBaseConnectionStringBuilder)val).Password,
				UseSsl = ((int)((MySqlBaseConnectionStringBuilder)val).SslMode > 0)
			};
		}
		catch
		{
			return new DatabaseConnectionProfile
			{
				Server = "srv1237.hstgr.io",
				Port = 3306u,
				Database = "u621755393_CBaranggayMana",
				Username = "u621755393_cbaranggay",
				Password = "Dssc@2026",
				UseSsl = false
			};
		}
	}

	private static string NormalizeConnectionString(string value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		string text = value.Replace("SslMode=None", "SslMode=Disabled", StringComparison.OrdinalIgnoreCase);
		try
		{
			MySqlConnectionStringBuilder val = new MySqlConnectionStringBuilder(text, false);
			val.AllowUserVariables = true;
			val.AllowPublicKeyRetrieval = true;
			if (val.ConnectionTimeout == 0 || val.ConnectionTimeout < 15)
			{
				val.ConnectionTimeout = 15u;
			}
			val.Pooling = true;
			if (val.MinimumPoolSize > 1)
			{
				val.MinimumPoolSize = 1u;
			}
			if (val.MaximumPoolSize == 0 || val.MaximumPoolSize < 25)
			{
				val.MaximumPoolSize = 25u;
			}
			if (val.ConnectionLifeTime == 0 || val.ConnectionLifeTime > 180)
			{
				val.ConnectionLifeTime = 180u;
			}
			return ((DbConnectionStringBuilder)(object)val).ConnectionString;
		}
		catch
		{
			return text;
		}
	}

	private static string Cache(string value)
	{
		lock (SyncRoot)
		{
			resolvedConnectionString = value;
			return value;
		}
	}

	private static string BuildCandidate(string server, uint port, string user, string password)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		return ((DbConnectionStringBuilder)new MySqlConnectionStringBuilder
		{
			Server = server,
			Port = port,
			Database = "barangay_system",
			UserID = user,
			Password = password,
			SslMode = (MySqlSslMode)0,
			AllowPublicKeyRetrieval = true,
			AllowUserVariables = true,
			ConnectionTimeout = 15u
		}).ConnectionString;
	}

	private static bool TryResolveWorkingConnectionString(string candidate, out string workingConnectionString, out string errorMessage)
	{
		string text = NormalizeConnectionString(candidate);
		if (TryOpenDirect(text, out errorMessage, out Exception openException))
		{
			workingConnectionString = text;
			return true;
		}
		if (TryBuildSslDisabledFallback(text, openException, out string fallbackConnectionString))
		{
			if (TryOpenDirect(fallbackConnectionString, out string errorMessage2, out Exception _))
			{
				workingConnectionString = fallbackConnectionString;
				errorMessage = string.Empty;
				return true;
			}
			errorMessage = (string.IsNullOrWhiteSpace(errorMessage) ? errorMessage2 : (errorMessage + " Retry with SSL disabled failed: " + errorMessage2));
		}
		workingConnectionString = text;
		return false;
	}

	private static bool TryOpenDirect(string connectionString, out string errorMessage, out Exception? openException)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		errorMessage = string.Empty;
		openException = null;
		try
		{
			MySqlConnection val = new MySqlConnection(connectionString);
			try
			{
				((DbConnection)(object)val).Open();
				RegisterConnectivitySuccess();
				return true;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			RegisterConnectivityFailure(ex);
			if (ShouldRetryOpen(ex))
			{
				TryClearPools();
				Thread.Sleep(150);
				try
				{
					MySqlConnection val2 = new MySqlConnection(connectionString);
					try
					{
						((DbConnection)(object)val2).Open();
						RegisterConnectivitySuccess();
						return true;
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				catch (Exception ex2)
				{
					RegisterConnectivityFailure(ex2);
					openException = ex2;
					errorMessage = ex2.Message;
					return false;
				}
			}
			openException = ex;
			errorMessage = ex.Message;
			return false;
		}
	}

	private static bool TryBuildSslDisabledFallback(string connectionString, Exception? openException, out string fallbackConnectionString)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		fallbackConnectionString = connectionString;
		if (!IsSslHandshakeFailure(openException))
		{
			return false;
		}
		try
		{
			MySqlConnectionStringBuilder val = new MySqlConnectionStringBuilder(connectionString, false);
			if ((int)((MySqlBaseConnectionStringBuilder)val).SslMode != 1)
			{
				return false;
			}
			((MySqlBaseConnectionStringBuilder)val).SslMode = (MySqlSslMode)0;
			fallbackConnectionString = NormalizeConnectionString(((DbConnectionStringBuilder)(object)val).ConnectionString);
			return !string.Equals(fallbackConnectionString, connectionString, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSslHandshakeFailure(Exception? exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			if (ex is AuthenticationException)
			{
				return true;
			}
			string text = ex.Message ?? string.Empty;
			if (text.IndexOf("SSL", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("TLS", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("security package", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("certificate", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	public static string GetCurrentConnectionString()
	{
		return ResolveConnectionString();
	}

	public static bool IsSqliteSelected()
	{
		lock (SyncRoot)
		{
			if (runtimeSqliteSelection)
			{
				return true;
			}
		}
		return DbConnectionSettingsStore.IsSqliteSelected();
	}

	public static void SetRuntimeConnectionString(string connectionString)
	{
		string text = NormalizeConnectionString(connectionString);
		TryClearPools();
		lock (SyncRoot)
		{
			runtimeSqliteSelection = false;
			runtimeConnectionString = text;
			resolvedConnectionString = null;
			lastConnectivityFailureUtc = null;
		}
	}

	public static void SetRuntimeSqliteSelection(bool isSelected)
	{
		TryClearPools();
		lock (SyncRoot)
		{
			runtimeSqliteSelection = isSelected;
			if (isSelected)
			{
				runtimeConnectionString = null;
				lastConnectivityFailureUtc = DateTime.UtcNow;
			}
			else
			{
				lastConnectivityFailureUtc = null;
			}
			resolvedConnectionString = null;
		}
	}

	public static bool ShouldThrottleOnlineAccess(bool includeOfflineMode = true)
	{
		if (IsSqliteSelected())
		{
			return true;
		}
		if (includeOfflineMode && OfflineDatabaseSupport.IsOffline)
		{
			return true;
		}
		lock (SyncRoot)
		{
			if (!lastConnectivityFailureUtc.HasValue)
			{
				return false;
			}
			return DateTime.UtcNow - lastConnectivityFailureUtc.Value < ConnectivityFailureCooldown;
		}
	}

	public static void RegisterConnectivityFailure(Exception? exception = null)
	{
		if (exception != null && !IsConnectivityFailure(exception) && !IsSslHandshakeFailure(exception))
		{
			return;
		}
		lock (SyncRoot)
		{
			lastConnectivityFailureUtc = DateTime.UtcNow;
		}
	}

	public static void RegisterConnectivitySuccess()
	{
		lock (SyncRoot)
		{
			lastConnectivityFailureUtc = null;
		}
	}

	public static bool TryOpen(string connectionString, out string errorMessage)
	{
		string workingConnectionString;
		return TryResolveWorkingConnectionString(connectionString, out workingConnectionString, out errorMessage);
	}

	public static bool TryGetWorkingConnectionString(string connectionString, out string workingConnectionString, out string errorMessage)
	{
		return TryResolveWorkingConnectionString(connectionString, out workingConnectionString, out errorMessage);
	}

	public static bool TryOpenCurrent(out string errorMessage)
	{
		if (IsSqliteSelected())
		{
			errorMessage = "The active database profile is SQLite.";
			return false;
		}
		try
		{
			return TryOpen(GetCurrentConnectionString(), out errorMessage);
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	public static string BuildFromParts(string server, uint port, string database, string user, string password, bool useSsl)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		return NormalizeConnectionString(((DbConnectionStringBuilder)new MySqlConnectionStringBuilder
		{
			Server = server,
			Port = ((port == 0) ? 3306u : port),
			Database = database,
			UserID = user,
			Password = password,
			SslMode = (MySqlSslMode)(useSsl ? 1 : 0),
			AllowPublicKeyRetrieval = true,
			AllowUserVariables = true,
			ConnectionTimeout = 15u
		}).ConnectionString);
	}

	public static string BuildServerConnectionString(string server, uint port, string user, string password, bool useSsl)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		return NormalizeConnectionString(((DbConnectionStringBuilder)new MySqlConnectionStringBuilder
		{
			Server = server,
			Port = ((port == 0) ? 3306u : port),
			UserID = user,
			Password = password,
			SslMode = (MySqlSslMode)(useSsl ? 1 : 0),
			AllowPublicKeyRetrieval = true,
			AllowUserVariables = true,
			ConnectionTimeout = 15u
		}).ConnectionString);
	}

	public static MySqlConnection GetConnection()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		if (IsSqliteSelected())
		{
			throw new InvalidOperationException("The active database profile is SQLite. Use DbHelper, DatabaseManagerAsync, or OfflineDatabaseSupport for SQLite-backed operations.");
		}
		return new MySqlConnection(ResolveConnectionString());
	}

	private static bool AllowLocalFallback()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("BARANGAY_ALLOW_LOCAL_FALLBACK");
		if (!string.IsNullOrWhiteSpace(environmentVariable))
		{
			environmentVariable = environmentVariable.Trim();
			if (environmentVariable.Equals("1", StringComparison.OrdinalIgnoreCase) || environmentVariable.Equals("true", StringComparison.OrdinalIgnoreCase) || environmentVariable.Equals("yes", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ShouldRetryOpen(Exception exception)
	{
		if (!IsConnectivityFailure(exception))
		{
			return IsPoolExhaustion(exception);
		}
		return true;
	}

	private static bool IsConnectivityFailure(Exception? exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			if (ex is TimeoutException)
			{
				return true;
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
			}
			string text = ex.Message ?? string.Empty;
			if (text.IndexOf("Unable to connect", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("server is not responding", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsPoolExhaustion(Exception? exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			string text = ex.Message ?? string.Empty;
			if (text.IndexOf("max pool size", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("obtaining a connection from the pool", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("connection from the pool", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void TryClearPools()
	{
		try
		{
			MySqlConnection.ClearAllPools();
		}
		catch
		{
		}
	}
}
