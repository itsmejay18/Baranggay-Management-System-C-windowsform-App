using System;
using System.Collections.Generic;
using baranggaysystem1.Database;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.helper
{
    internal static class Permissions
    {
        private static readonly object SyncRoot = new();
        private static string _loadedRole = string.Empty;
        private static Dictionary<string, bool> _grants = new(StringComparer.OrdinalIgnoreCase);

        public static bool IsAdmin =>
            string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);

        public static bool CanCreateResidents => Has(PermissionKeys.CreateResidents);
        public static bool CanUpdateResidents => Has(PermissionKeys.UpdateResidents);
        public static bool CanDeleteResidents => Has(PermissionKeys.DeleteResidents);

        public static bool CanRequestCertificates => Has(PermissionKeys.RequestCertificates);
        public static bool CanEditCertificateRequests => Has(PermissionKeys.EditCertificateRequests);
        public static bool CanApproveCertificates => Has(PermissionKeys.ApproveCertificates);
        public static bool CanIssueCertificates => Has(PermissionKeys.IssueCertificates);
        public static bool CanCancelCertificates => Has(PermissionKeys.CancelCertificates);
        public static bool CanExportCertificates => Has(PermissionKeys.ExportCertificates);

        public static bool CanCreateBlotter => Has(PermissionKeys.CreateBlotter);
        public static bool CanUpdateBlotterStatus => Has(PermissionKeys.UpdateBlotterStatus);

        public static bool CanManageUsers => Has(PermissionKeys.ManageUsers);
        public static bool CanOpenSettings => Has(PermissionKeys.OpenSettings);
        public static bool CanManageAnnouncements => Has(PermissionKeys.ManageAnnouncements);
        public static bool CanManageProjects => Has(PermissionKeys.ManageProjects);
        public static bool CanManageAttachments => Has(PermissionKeys.ManageAttachments);
        public static bool CanDispatchNotifications => Has(PermissionKeys.DispatchNotifications);
        public static bool CanViewHotspotReports => Has(PermissionKeys.ViewHotspotReports);
        public static bool CanViewHouseholds => Has(PermissionKeys.ViewHouseholds);
        public static bool CanCreateHouseholds => Has(PermissionKeys.CreateHouseholds);
        public static bool CanEditHouseholds => Has(PermissionKeys.EditHouseholds);
        public static bool CanDeleteHouseholds => Has(PermissionKeys.DeleteHouseholds);
        public static bool CanTransferHouseholds => Has(PermissionKeys.TransferHouseholds);

        public static void Refresh()
        {
            lock (SyncRoot)
            {
                _loadedRole = string.Empty;
                _grants = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static bool Has(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                return false;
            }

            EnsureLoaded();
            return _grants.TryGetValue(permissionKey, out bool allowed) && allowed;
        }

        private static void EnsureLoaded()
        {
            string roleName = UserSession.Role?.Trim() ?? string.Empty;

            lock (SyncRoot)
            {
                if (string.Equals(_loadedRole, roleName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _grants = LoadRolePermissions(roleName);
                _loadedRole = roleName;
            }
        }

        private static Dictionary<string, bool> LoadRolePermissions(string roleName)
        {
            var grants = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var conn = DBConnection.GetConnection();
                conn.Open();
                SchemaBootstrap.EnsureCoreDefaults(conn);
                using var cmd = new MySqlCommand(
                    @"SELECT rp.permission_key, rp.is_allowed
                      FROM role_permission rp
                      INNER JOIN role r ON r.role_id = rp.role_id
                      WHERE r.name = @roleName", conn);
                cmd.Parameters.AddWithValue("@roleName", roleName);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string key = Convert.ToString(reader["permission_key"]) ?? string.Empty;
                    bool allowed = reader["is_allowed"] != DBNull.Value && Convert.ToInt32(reader["is_allowed"]) == 1;
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        grants[key] = allowed;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Permissions.LoadRolePermissions failed.", ex);
            }

            return grants;
        }
    }
}
