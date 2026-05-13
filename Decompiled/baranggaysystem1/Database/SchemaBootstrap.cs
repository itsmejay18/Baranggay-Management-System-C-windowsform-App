using System;
using System.Collections.Generic;
using System.Data.Common;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class SchemaBootstrap
{
	private const string BootstrapAdminPasswordEnv = "BARANGAY_BOOTSTRAP_ADMIN_PASSWORD";

	private const string BootstrapAdminUsernameEnv = "BARANGAY_BOOTSTRAP_ADMIN_USERNAME";

	public static void EnsureCoreDefaults(MySqlConnection conn)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("INSERT INTO barangay (barangay_id, name)\n              SELECT 1, 'Default Barangay'\n              WHERE NOT EXISTS (\n                  SELECT 1 FROM barangay WHERE barangay_id = 1\n              );", conn);
		try
		{
			((DbCommand)(object)val).ExecuteNonQuery();
			MySqlCommand val2 = new MySqlCommand("INSERT INTO purok_sitio (purok_id, barangay_id, name, type)\n              SELECT 1, 1, 'Default Purok', 'PUROK'\n              WHERE NOT EXISTS (\n                  SELECT 1 FROM purok_sitio WHERE purok_id = 1\n              );", conn);
			try
			{
				((DbCommand)(object)val2).ExecuteNonQuery();
				(string, string)[] array = new(string, string)[3]
				{
					("Super Admin", "Primary system owner"),
					("Admin", "System administrator"),
					("Staff", "Staff account")
				};
				for (int i = 0; i < array.Length; i++)
				{
					(string, string) tuple = array[i];
					string item = tuple.Item1;
					string item2 = tuple.Item2;
					MySqlCommand val3 = new MySqlCommand("INSERT INTO role (name, description) SELECT @name, @desc FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM role WHERE name = @name);", conn);
					try
					{
						val3.Parameters.AddWithValue("@name", (object)item);
						val3.Parameters.AddWithValue("@desc", (object)item2);
						((DbCommand)(object)val3).ExecuteNonQuery();
					}
					finally
					{
						((IDisposable)val3)?.Dispose();
					}
				}
				EnsureDefaultDocumentTypes(conn);
				try
				{
					MySqlCommand val4 = new MySqlCommand("UPDATE document_type\n                  SET validity_days = COALESCE(validity_days, 365),\n                      renewal_reminder_days = COALESCE(renewal_reminder_days, 30)\n                  WHERE UPPER(code) = 'BC' OR UPPER(name) = 'BARANGAY CLEARANCE'", conn);
					try
					{
						((DbCommand)(object)val4).ExecuteNonQuery();
					}
					finally
					{
						((IDisposable)val4)?.Dispose();
					}
				}
				catch
				{
				}
				MySqlCommand val5 = new MySqlCommand("INSERT INTO case_type (name) VALUES ('General') ON DUPLICATE KEY UPDATE name = VALUES(name);", conn);
				try
				{
					((DbCommand)(object)val5).ExecuteNonQuery();
					EnsureRolePermissionDefaults(conn);
					EnsureTemporaryAdminAccount(conn);
				}
				finally
				{
					((IDisposable)val5)?.Dispose();
				}
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

	private static void EnsureDefaultDocumentTypes(MySqlConnection conn)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		(string, string)[] array = new(string, string)[4]
		{
			("Barangay Clearance", "BC"),
			("Certificate of Residency", "CR"),
			("Indigency", "IND"),
			("Business Clearance", "BUS")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			MySqlCommand val = new MySqlCommand("INSERT INTO document_type (name, code, requires_approval)\n                  SELECT @name, @code, 1\n                  FROM DUAL\n                  WHERE NOT EXISTS (\n                      SELECT 1\n                      FROM document_type\n                      WHERE UPPER(COALESCE(code, '')) = UPPER(@code)\n                         OR UPPER(TRIM(name)) = UPPER(TRIM(@name))\n                  );", conn);
			try
			{
				val.Parameters.AddWithValue("@name", (object)item);
				val.Parameters.AddWithValue("@code", (object)item2);
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	private static void EnsureRolePermissionDefaults(MySqlConnection conn)
	{
		if (TableExists(conn, "role_permission"))
		{
			int roleId = GetRoleId(conn, "Super Admin");
			int roleId2 = GetRoleId(conn, "Admin");
			int roleId3 = GetRoleId(conn, "Staff");
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"residents.create", "residents.update", "certificates.request", "certificates.edit_request", "blotter.create", "attachments.manage", "reports.view_hotspot", "household.view", "household.create", "household.edit",
				"household.transfer"
			};
			string[] all = PermissionKeys.All;
			foreach (string text in all)
			{
				UpsertRolePermission(conn, roleId, text, allowed: true);
				UpsertRolePermission(conn, roleId2, text, allowed: true);
				UpsertRolePermission(conn, roleId3, text, hashSet.Contains(text));
			}
		}
	}

	private static int GetRoleId(MySqlConnection conn, string roleName)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT role_id FROM role WHERE name = @name LIMIT 1", conn);
		try
		{
			val.Parameters.AddWithValue("@name", (object)roleName);
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			if (obj == null || obj == DBNull.Value)
			{
				throw new InvalidOperationException("Missing role '" + roleName + "'.");
			}
			return Convert.ToInt32(obj);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void UpsertRolePermission(MySqlConnection conn, int roleId, string permissionKey, bool allowed)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("INSERT INTO role_permission (role_id, permission_key, is_allowed)\n              VALUES (@roleId, @permissionKey, @allowed)\n              ON DUPLICATE KEY UPDATE is_allowed = VALUES(is_allowed)", conn);
		try
		{
			val.Parameters.AddWithValue("@roleId", (object)roleId);
			val.Parameters.AddWithValue("@permissionKey", (object)permissionKey);
			val.Parameters.AddWithValue("@allowed", (object)(allowed ? 1 : 0));
			((DbCommand)(object)val).ExecuteNonQuery();
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

	private static void EnsureTemporaryAdminAccount(MySqlConnection conn)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected O, but got Unknown
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Expected O, but got Unknown
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Expected O, but got Unknown
		if (!TableExists(conn, "user_account") || !TableExists(conn, "user_role"))
		{
			return;
		}
		try
		{
			string environmentVariable = Environment.GetEnvironmentVariable("BARANGAY_BOOTSTRAP_ADMIN_PASSWORD");
			if (string.IsNullOrWhiteSpace(environmentVariable))
			{
				return;
			}
			string text = Environment.GetEnvironmentVariable("BARANGAY_BOOTSTRAP_ADMIN_USERNAME");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "admin";
			}
			int roleId = GetRoleId(conn, "Super Admin");
			string text2 = PasswordHelper.HashPassword(environmentVariable);
			MySqlCommand val = new MySqlCommand("SELECT user_id FROM user_account WHERE LOWER(username) = LOWER(@username) LIMIT 1", conn);
			int num;
			try
			{
				val.Parameters.AddWithValue("@username", (object)text);
				object obj = ((DbCommand)(object)val).ExecuteScalar();
				if (obj == null || obj == DBNull.Value)
				{
					MySqlCommand val2 = new MySqlCommand("INSERT INTO user_account\n                            (barangay_id, username, password_hash, full_name, is_active, created_at, updated_at)\n                          VALUES\n                            (@barangayId, @username, @passwordHash, 'Bootstrap Admin', 1, NOW(), NOW())", conn);
					try
					{
						val2.Parameters.AddWithValue("@barangayId", (object)1);
						val2.Parameters.AddWithValue("@username", (object)text);
						val2.Parameters.AddWithValue("@passwordHash", (object)text2);
						((DbCommand)(object)val2).ExecuteNonQuery();
						num = Convert.ToInt32(val2.LastInsertedId);
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				else
				{
					num = Convert.ToInt32(obj);
					MySqlCommand val3 = new MySqlCommand("UPDATE user_account\n                          SET is_active = 1,\n                              updated_at = NOW()\n                          WHERE user_id = @userId", conn);
					try
					{
						val3.Parameters.AddWithValue("@userId", (object)num);
						((DbCommand)(object)val3).ExecuteNonQuery();
					}
					finally
					{
						((IDisposable)val3)?.Dispose();
					}
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			MySqlCommand val4 = new MySqlCommand("SELECT COUNT(*) FROM user_role WHERE user_id = @userId AND role_id = @roleId", conn);
			try
			{
				val4.Parameters.AddWithValue("@userId", (object)num);
				val4.Parameters.AddWithValue("@roleId", (object)roleId);
				if (Convert.ToInt32(((DbCommand)(object)val4).ExecuteScalar() ?? ((object)0)) == 0)
				{
					MySqlCommand val5 = new MySqlCommand("INSERT INTO user_role (user_id, role_id) VALUES (@userId, @roleId)", conn);
					try
					{
						val5.Parameters.AddWithValue("@userId", (object)num);
						val5.Parameters.AddWithValue("@roleId", (object)roleId);
						((DbCommand)(object)val5).ExecuteNonQuery();
						return;
					}
					finally
					{
						((IDisposable)val5)?.Dispose();
					}
				}
			}
			finally
			{
				((IDisposable)val4)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("EnsureTemporaryAdminAccount failed.", ex);
		}
	}
}
