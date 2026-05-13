using System;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal static class PackageInstallerService
{
	public static bool NeedsInstaller()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		try
		{
			if (!DBConnection.TryOpenCurrent(out string _))
			{
				return true;
			}
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				if (!TableExists(connection, "user_account"))
				{
					return true;
				}
				MySqlCommand val = new MySqlCommand("SELECT COUNT(*) FROM user_account", connection);
				try
				{
					return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) == 0;
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
			return true;
		}
	}

	public static ConnectionTestResult TestConnection(DatabaseConnectionProfile profile)
	{
		if (DBConnection.TryOpen(DbConnectionSettingsStore.BuildConnectionString(profile), out string errorMessage))
		{
			return ConnectionTestResult.Pass("Connection successful. Database is reachable.");
		}
		if (IsUnknownDatabaseError(errorMessage))
		{
			if (DBConnection.TryOpen(DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: false), out string errorMessage2))
			{
				return ConnectionTestResult.Pass("Server connection successful. Database does not exist yet and will be created during install.", databaseMissing: true);
			}
			return ConnectionTestResult.Fail("Server connection failed: " + errorMessage2);
		}
		return ConnectionTestResult.Fail("Connection failed: " + errorMessage);
	}

	public static void Install(PackageInstallRequest request)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected O, but got Unknown
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		string text = request.SuperAdminUsername?.Trim() ?? string.Empty;
		string text2 = request.SuperAdminPassword ?? string.Empty;
		string text3 = request.UserUsername?.Trim() ?? string.Empty;
		string text4 = request.UserPassword ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("Super Admin username is required.");
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			throw new InvalidOperationException("Super Admin password is required.");
		}
		if (string.IsNullOrWhiteSpace(text3))
		{
			throw new InvalidOperationException("User username is required.");
		}
		if (string.IsNullOrWhiteSpace(text4))
		{
			throw new InvalidOperationException("User password is required.");
		}
		if (string.Equals(text, text3, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Super Admin and User usernames must be different.");
		}
		DatabaseConnectionProfile databaseConnectionProfile = request.ConnectionProfile ?? DatabaseConnectionProfile.CreateDefault();
		string workingConnectionString = DbConnectionSettingsStore.BuildConnectionString(databaseConnectionProfile, includeDatabase: false);
		if (!DBConnection.TryGetWorkingConnectionString(workingConnectionString, out workingConnectionString, out string errorMessage))
		{
			throw new InvalidOperationException("Server connection failed: " + errorMessage);
		}
		string text5 = DbConnectionSettingsStore.BuildConnectionString(databaseConnectionProfile);
		MySqlConnection val = new MySqlConnection(workingConnectionString);
		try
		{
			((DbConnection)(object)val).Open();
			EnsureDatabaseExists(val, databaseConnectionProfile.Database);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		if (DBConnection.TryGetWorkingConnectionString(text5, out string workingConnectionString2, out string _))
		{
			text5 = workingConnectionString2;
		}
		DbConnectionSettingsStore.Save(databaseConnectionProfile);
		DBConnection.SetRuntimeConnectionString(text5);
		SchemaGuard.EnsureDatabaseReady();
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			SchemaBootstrap.EnsureCoreDefaults(connection);
			MySqlTransaction val2 = connection.BeginTransaction();
			try
			{
				int roleId = EnsureRole(connection, val2, "Super Admin", "Primary system owner.");
				int roleId2 = EnsureRole(connection, val2, "Staff", "Staff account.");
				UpsertUserWithRole(connection, val2, text, text2, "Super Admin", roleId);
				UpsertUserWithRole(connection, val2, text3, text4, "Default User", roleId2);
				((DbTransaction)(object)val2).Commit();
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static void EnsureDatabaseExists(MySqlConnection serverConn, string databaseName)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (string.IsNullOrWhiteSpace(databaseName))
		{
			throw new InvalidOperationException("Database name is required.");
		}
		string text = databaseName.Trim().Replace("`", "``", StringComparison.Ordinal);
		MySqlCommand val = new MySqlCommand("CREATE DATABASE IF NOT EXISTS `" + text + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", serverConn);
		try
		{
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static int EnsureRole(MySqlConnection conn, MySqlTransaction tx, string roleName, string description)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT role_id FROM role WHERE name = @name LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@name", (object)roleName);
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			if (obj != null && obj != DBNull.Value)
			{
				return Convert.ToInt32(obj);
			}
			MySqlCommand val2 = new MySqlCommand("INSERT INTO role (name, description) VALUES (@name, @description)", conn, tx);
			try
			{
				val2.Parameters.AddWithValue("@name", (object)roleName);
				val2.Parameters.AddWithValue("@description", (object)description);
				((DbCommand)(object)val2).ExecuteNonQuery();
				return (int)val2.LastInsertedId;
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

	private static void UpsertUserWithRole(MySqlConnection conn, MySqlTransaction tx, string username, string password, string fullName, int roleId)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		int? num = FindUserId(conn, tx, username);
		string text = PasswordHelper.HashPassword(password);
		if (!num.HasValue)
		{
			MySqlCommand val = new MySqlCommand("INSERT INTO user_account\n                    (barangay_id, username, password_hash, full_name, is_active, created_at, updated_at)\n                  VALUES\n                    (@barangayId, @username, @passwordHash, @fullName, 1, NOW(), NOW())", conn, tx);
			try
			{
				val.Parameters.AddWithValue("@barangayId", (object)1);
				val.Parameters.AddWithValue("@username", (object)username);
				val.Parameters.AddWithValue("@passwordHash", (object)text);
				val.Parameters.AddWithValue("@fullName", (object)fullName);
				((DbCommand)(object)val).ExecuteNonQuery();
				num = (int)val.LastInsertedId;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		else
		{
			MySqlCommand val2 = new MySqlCommand("UPDATE user_account\n                  SET password_hash = @passwordHash,\n                      full_name = CASE WHEN IFNULL(full_name, '') = '' THEN @fullName ELSE full_name END,\n                      is_active = 1,\n                      updated_at = NOW()\n                  WHERE user_id = @userId", conn, tx);
			try
			{
				val2.Parameters.AddWithValue("@passwordHash", (object)text);
				val2.Parameters.AddWithValue("@fullName", (object)fullName);
				val2.Parameters.AddWithValue("@userId", (object)num.Value);
				((DbCommand)(object)val2).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		MySqlCommand val3 = new MySqlCommand("DELETE FROM user_role WHERE user_id = @userId", conn, tx);
		try
		{
			val3.Parameters.AddWithValue("@userId", (object)num.Value);
			((DbCommand)(object)val3).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
		MySqlCommand val4 = new MySqlCommand("INSERT INTO user_role (user_id, role_id) VALUES (@userId, @roleId)", conn, tx);
		try
		{
			val4.Parameters.AddWithValue("@userId", (object)num.Value);
			val4.Parameters.AddWithValue("@roleId", (object)roleId);
			((DbCommand)(object)val4).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val4)?.Dispose();
		}
	}

	private static int? FindUserId(MySqlConnection conn, MySqlTransaction tx, string username)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT user_id FROM user_account WHERE username = @username LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@username", (object)username);
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			if (obj == null || obj == DBNull.Value)
			{
				return null;
			}
			return Convert.ToInt32(obj);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool TableExists(MySqlConnection conn, string table)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n              FROM INFORMATION_SCHEMA.TABLES\n              WHERE TABLE_SCHEMA = DATABASE()\n                AND TABLE_NAME = @table", conn);
		try
		{
			val.Parameters.AddWithValue("@table", (object)table);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar()) > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool IsUnknownDatabaseError(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return false;
		}
		if (message.IndexOf("Unknown database", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("doesn't exist", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return message.IndexOf("1049", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}
}
