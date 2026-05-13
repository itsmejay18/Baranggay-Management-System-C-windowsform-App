using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1.helper;

internal static class Permissions
{
	private static readonly object SyncRoot = new object();

	private static string _loadedRole = string.Empty;

	private static Dictionary<string, bool> _grants = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

	public static bool IsAdmin
	{
		get
		{
			if (!string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}

	public static bool CanCreateResidents => Has("residents.create");

	public static bool CanUpdateResidents => Has("residents.update");

	public static bool CanDeleteResidents => Has("residents.delete");

	public static bool CanRequestCertificates => Has("certificates.request");

	public static bool CanEditCertificateRequests => Has("certificates.edit_request");

	public static bool CanApproveCertificates => Has("certificates.approve");

	public static bool CanIssueCertificates => Has("certificates.issue");

	public static bool CanCancelCertificates => Has("certificates.cancel");

	public static bool CanExportCertificates => Has("certificates.export");

	public static bool CanCreateBlotter => Has("blotter.create");

	public static bool CanUpdateBlotterStatus => Has("blotter.update_status");

	public static bool CanManageUsers => Has("users.manage");

	public static bool CanOpenSettings => Has("settings.open");

	public static bool CanManageAnnouncements => Has("announcements.manage");

	public static bool CanManageProjects => Has("projects.manage");

	public static bool CanManageAttachments => Has("attachments.manage");

	public static bool CanDispatchNotifications => Has("notifications.dispatch");

	public static bool CanViewHotspotReports => Has("reports.view_hotspot");

	public static bool CanViewHouseholds => Has("household.view");

	public static bool CanCreateHouseholds => Has("household.create");

	public static bool CanEditHouseholds => Has("household.edit");

	public static bool CanDeleteHouseholds => Has("household.delete");

	public static bool CanTransferHouseholds => Has("household.transfer");

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
		if (string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		EnsureLoaded();
		bool value;
		return _grants.TryGetValue(permissionKey, out value) && value;
	}

	private static void EnsureLoaded()
	{
		string text = UserSession.Role?.Trim() ?? string.Empty;
		lock (SyncRoot)
		{
			if (!string.Equals(_loadedRole, text, StringComparison.OrdinalIgnoreCase))
			{
				_grants = LoadRolePermissions(text);
				_loadedRole = text;
			}
		}
	}

	private static Dictionary<string, bool> LoadRolePermissions(string roleName)
	{
		Dictionary<string, bool> dictionary = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
		try
		{
			EnsureRolePermissionSchemaOnline();
			foreach (DataRow row in DbHelper.LoadTable("SELECT rp.permission_key, rp.is_allowed\n                      FROM role_permission rp\n                      INNER JOIN role r ON r.role_id = rp.role_id\n                      WHERE r.name = @roleName", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@roleName", (object)roleName);
			}).Rows)
			{
				string text = Convert.ToString(row["permission_key"]) ?? string.Empty;
				bool value = row["is_allowed"] != DBNull.Value && Convert.ToInt32(row["is_allowed"]) == 1;
				if (!string.IsNullOrWhiteSpace(text))
				{
					dictionary[text] = value;
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Permissions.LoadRolePermissions failed.", ex);
		}
		ApplyFallbackPermissions(roleName, dictionary);
		return dictionary;
	}

	private static void EnsureRolePermissionSchemaOnline()
	{
		if (OfflineDatabaseSupport.IsOffline || DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false))
		{
			return;
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				DBConnection.RegisterConnectivitySuccess();
				SchemaBootstrap.EnsureCoreDefaults(connection);
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			DBConnection.RegisterConnectivityFailure(ex);
			AppLogger.LogWarning("Permissions schema bootstrap skipped (continuing with existing data).", ex);
		}
	}

	private static void ApplyFallbackPermissions(string roleName, Dictionary<string, bool> grants)
	{
		if (grants.Count <= 0 && (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(roleName, "Super Admin", StringComparison.OrdinalIgnoreCase)))
		{
			string[] all = PermissionKeys.All;
			foreach (string key in all)
			{
				grants[key] = true;
			}
			AppLogger.LogWarning("Permissions fallback applied for role '" + roleName + "'. Access granted from local defaults.");
		}
	}
}
