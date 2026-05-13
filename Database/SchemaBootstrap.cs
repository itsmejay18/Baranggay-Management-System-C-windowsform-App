using System;
using System.Collections.Generic;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal static class SchemaBootstrap
{
    private const string BootstrapAdminPasswordEnv = "BARANGAY_BOOTSTRAP_ADMIN_PASSWORD";
    private const string BootstrapAdminUsernameEnv = "BARANGAY_BOOTSTRAP_ADMIN_USERNAME";

    public static void EnsureCoreDefaults(MySqlConnection conn)
    {
        using var barangayCmd = new MySqlCommand(
            "INSERT INTO barangay (barangay_id, name) VALUES (1, 'Default Barangay') " +
            "ON DUPLICATE KEY UPDATE name = VALUES(name);",
            conn);
        barangayCmd.ExecuteNonQuery();

        using var purokCmd = new MySqlCommand(
            "INSERT INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (1, 1, 'Default Purok', 'PUROK') " +
            "ON DUPLICATE KEY UPDATE name = VALUES(name);",
            conn);
        purokCmd.ExecuteNonQuery();

        // Use WHERE NOT EXISTS so this is safe even when the unique index on name
        // does not yet exist (SchemaGuard adds it on first run after startup).
        foreach (var (roleName, roleDesc) in new[]
        {
            ("Super Admin", "Primary system owner"),
            ("Admin",       "System administrator"),
            ("Staff",       "Staff account"),
        })
        {
            using var roleCmd = new MySqlCommand(
                "INSERT INTO role (name, description) " +
                "SELECT @name, @desc FROM DUAL " +
                "WHERE NOT EXISTS (SELECT 1 FROM role WHERE name = @name);",
                conn);
            roleCmd.Parameters.AddWithValue("@name", roleName);
            roleCmd.Parameters.AddWithValue("@desc", roleDesc);
            roleCmd.ExecuteNonQuery();
        }

        using var docCmd = new MySqlCommand(
            "INSERT INTO document_type (name, code, requires_approval) VALUES " +
            "('Barangay Clearance', 'BC', 1)," +
            "('Certificate of Residency', 'CR', 1)," +
            "('Indigency', 'IND', 1)," +
            "('Business Clearance', 'BUS', 1) " +
            "ON DUPLICATE KEY UPDATE code = VALUES(code);",
            conn);
        docCmd.ExecuteNonQuery();

        try
        {
            using var docDefaultsCmd = new MySqlCommand(
                @"UPDATE document_type
                  SET validity_days = COALESCE(validity_days, 365),
                      renewal_reminder_days = COALESCE(renewal_reminder_days, 30)
                  WHERE UPPER(code) = 'BC' OR UPPER(name) = 'BARANGAY CLEARANCE'",
                conn);
            docDefaultsCmd.ExecuteNonQuery();
        }
        catch
        {
            // Column may not exist in older pre-migration databases.
        }

        using var caseCmd = new MySqlCommand(
            "INSERT INTO case_type (name) VALUES ('General') " +
            "ON DUPLICATE KEY UPDATE name = VALUES(name);",
            conn);
        caseCmd.ExecuteNonQuery();

        EnsureRolePermissionDefaults(conn);
        EnsureTemporaryAdminAccount(conn);
    }

    private static void EnsureRolePermissionDefaults(MySqlConnection conn)
    {
        if (!TableExists(conn, "role_permission"))
        {
            return;
        }

        int superAdminRoleId = GetRoleId(conn, "Super Admin");
        int adminRoleId = GetRoleId(conn, "Admin");
        int staffRoleId = GetRoleId(conn, "Staff");

        var staffAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionKeys.CreateResidents,
            PermissionKeys.UpdateResidents,
            PermissionKeys.RequestCertificates,
            PermissionKeys.EditCertificateRequests,
            PermissionKeys.CreateBlotter,
            PermissionKeys.ManageAttachments,
            PermissionKeys.ViewHotspotReports,
            PermissionKeys.ViewHouseholds,
            PermissionKeys.CreateHouseholds,
            PermissionKeys.EditHouseholds,
            PermissionKeys.TransferHouseholds
        };

        foreach (string key in PermissionKeys.All)
        {
            UpsertRolePermission(conn, superAdminRoleId, key, true);
            UpsertRolePermission(conn, adminRoleId, key, true);
            UpsertRolePermission(conn, staffRoleId, key, staffAllowed.Contains(key));
        }
    }

    private static int GetRoleId(MySqlConnection conn, string roleName)
    {
        using var cmd = new MySqlCommand(
            "SELECT role_id FROM role WHERE name = @name LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@name", roleName);
        object? value = cmd.ExecuteScalar();
        if (value == null || value == DBNull.Value)
        {
            throw new InvalidOperationException($"Missing role '{roleName}'.");
        }

        return Convert.ToInt32(value);
    }

    private static void UpsertRolePermission(MySqlConnection conn, int roleId, string permissionKey, bool allowed)
    {
        using var cmd = new MySqlCommand(
            @"INSERT INTO role_permission (role_id, permission_key, is_allowed)
              VALUES (@roleId, @permissionKey, @allowed)
              ON DUPLICATE KEY UPDATE is_allowed = VALUES(is_allowed)",
            conn);
        cmd.Parameters.AddWithValue("@roleId", roleId);
        cmd.Parameters.AddWithValue("@permissionKey", permissionKey);
        cmd.Parameters.AddWithValue("@allowed", allowed ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    private static bool TableExists(MySqlConnection conn, string table)
    {
        using var cmd = new MySqlCommand(
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table", conn);
        cmd.Parameters.AddWithValue("@table", table);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static void EnsureTemporaryAdminAccount(MySqlConnection conn)
    {
        if (!TableExists(conn, "user_account") || !TableExists(conn, "user_role"))
        {
            return;
        }

        try
        {
            string? password = Environment.GetEnvironmentVariable(BootstrapAdminPasswordEnv);
            if (string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            string username = Environment.GetEnvironmentVariable(BootstrapAdminUsernameEnv);
            if (string.IsNullOrWhiteSpace(username))
            {
                username = "admin";
            }

            int superAdminRoleId = GetRoleId(conn, "Super Admin");
            string hash = PasswordHelper.HashPassword(password);

            int userId;
            using (var findUser = new MySqlCommand("SELECT user_id FROM user_account WHERE LOWER(username) = LOWER(@username) LIMIT 1", conn))
            {
                findUser.Parameters.AddWithValue("@username", username);
                object? existing = findUser.ExecuteScalar();
                if (existing == null || existing == DBNull.Value)
                {
                    using var insertUser = new MySqlCommand(
                        @"INSERT INTO user_account
                            (barangay_id, username, password_hash, full_name, is_active, created_at, updated_at)
                          VALUES
                            (@barangayId, @username, @passwordHash, 'Bootstrap Admin', 1, NOW(), NOW())",
                        conn);
                    insertUser.Parameters.AddWithValue("@barangayId", SchemaDefaults.DefaultBarangayId);
                    insertUser.Parameters.AddWithValue("@username", username);
                    insertUser.Parameters.AddWithValue("@passwordHash", hash);
                    insertUser.ExecuteNonQuery();
                    userId = Convert.ToInt32(insertUser.LastInsertedId);
                }
                else
                {
                    userId = Convert.ToInt32(existing);
                    using var updateUser = new MySqlCommand(
                        @"UPDATE user_account
                          SET is_active = 1,
                              updated_at = NOW()
                          WHERE user_id = @userId",
                        conn);
                    updateUser.Parameters.AddWithValue("@userId", userId);
                    updateUser.ExecuteNonQuery();
                }
            }

            using (var roleCheck = new MySqlCommand(
                       "SELECT COUNT(*) FROM user_role WHERE user_id = @userId AND role_id = @roleId",
                       conn))
            {
                roleCheck.Parameters.AddWithValue("@userId", userId);
                roleCheck.Parameters.AddWithValue("@roleId", superAdminRoleId);
                int existingRole = Convert.ToInt32(roleCheck.ExecuteScalar() ?? 0);
                if (existingRole == 0)
                {
                    using var setRole = new MySqlCommand(
                        "INSERT INTO user_role (user_id, role_id) VALUES (@userId, @roleId)",
                        conn);
                    setRole.Parameters.AddWithValue("@userId", userId);
                    setRole.Parameters.AddWithValue("@roleId", superAdminRoleId);
                    setRole.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("EnsureTemporaryAdminAccount failed.", ex);
        }
    }
}
