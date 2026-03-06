using System;
using System.Collections.Generic;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal static class SchemaBootstrap
{
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

        using var roleCmd = new MySqlCommand(
            "INSERT INTO role (name, description) VALUES " +
            "('Admin', 'System administrator')," +
            "('Staff', 'Staff account') " +
            "ON DUPLICATE KEY UPDATE description = VALUES(description);",
            conn);
        roleCmd.ExecuteNonQuery();

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
    }

    private static void EnsureRolePermissionDefaults(MySqlConnection conn)
    {
        if (!TableExists(conn, "role_permission"))
        {
            return;
        }

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
}
