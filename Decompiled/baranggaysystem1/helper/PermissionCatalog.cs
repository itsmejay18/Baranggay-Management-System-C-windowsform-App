using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace baranggaysystem1.helper;

internal static class PermissionCatalog
{
	private static readonly PermissionCatalogItem[] _items = new PermissionCatalogItem[23]
	{
		new PermissionCatalogItem("residents.create", "Residents", "Create Residents", "Register new resident profiles and add them to the workspace.", 0, 0),
		new PermissionCatalogItem("residents.update", "Residents", "Edit Residents", "Update resident records, status, and profile details.", 0, 1),
		new PermissionCatalogItem("residents.delete", "Residents", "Deactivate Residents", "Archive, deactivate, or remove resident records from active use.", 0, 2),
		new PermissionCatalogItem("household.view", "Households", "View Households", "Open household records, members, and address details.", 1, 0),
		new PermissionCatalogItem("household.create", "Households", "Create Households", "Add new households and assign addresses or purok areas.", 1, 1),
		new PermissionCatalogItem("household.edit", "Households", "Edit Households", "Update household addresses, coordinates, and metadata.", 1, 2),
		new PermissionCatalogItem("household.delete", "Households", "Delete Households", "Delete household entries that are no longer needed.", 1, 3),
		new PermissionCatalogItem("household.transfer", "Households", "Transfer Residents", "Move residents between purok and household records.", 1, 4),
		new PermissionCatalogItem("certificates.request", "Services", "Create Requests", "File new certificate, clearance, and permit requests.", 2, 0),
		new PermissionCatalogItem("certificates.edit_request", "Services", "Edit Requests", "Update request details before release or approval.", 2, 1),
		new PermissionCatalogItem("certificates.approve", "Services", "Approve Requests", "Approve certificate, permit, and clearance requests.", 2, 2),
		new PermissionCatalogItem("certificates.issue", "Services", "Issue Documents", "Release approved certificates, permits, and clearances.", 2, 3),
		new PermissionCatalogItem("certificates.cancel", "Services", "Cancel Requests", "Cancel or void certificate requests and releases.", 2, 4),
		new PermissionCatalogItem("certificates.export", "Services", "Export Service Data", "Export request and issuance records for reporting.", 2, 5),
		new PermissionCatalogItem("blotter.create", "Cases", "Create Blotter Records", "File new blotter and case records for residents.", 3, 0),
		new PermissionCatalogItem("blotter.update_status", "Cases", "Update Case Status", "Change blotter progression, settlement, or referral status.", 3, 1),
		new PermissionCatalogItem("users.manage", "Administration", "Manage Users", "Manage staff accounts, roles, and access settings.", 4, 0),
		new PermissionCatalogItem("settings.open", "Administration", "Open Settings", "Access database and system configuration screens.", 4, 1),
		new PermissionCatalogItem("announcements.manage", "Administration", "Manage Announcements", "Create and publish dashboard announcements.", 4, 2),
		new PermissionCatalogItem("projects.manage", "Administration", "Manage Projects", "Create and update barangay project records.", 4, 3),
		new PermissionCatalogItem("attachments.manage", "Administration", "Manage Attachments", "Upload, open, and remove stored file attachments.", 4, 4),
		new PermissionCatalogItem("notifications.dispatch", "Administration", "Dispatch Notifications", "Send reminders and outbound notifications.", 4, 5),
		new PermissionCatalogItem("reports.view_hotspot", "Reports", "View Hotspot Reports", "Open hotspot analytics and risk reports.", 5, 0)
	};

	private static readonly Dictionary<string, PermissionCatalogItem> _byKey = _items.ToDictionary<PermissionCatalogItem, string, PermissionCatalogItem>((PermissionCatalogItem item) => item.Key, (PermissionCatalogItem item) => item, StringComparer.OrdinalIgnoreCase);

	public static IReadOnlyList<PermissionCatalogItem> All => _items;

	public static PermissionCatalogItem Resolve(string permissionKey)
	{
		if (_byKey.TryGetValue(permissionKey ?? string.Empty, out PermissionCatalogItem value))
		{
			return value;
		}
		string fallbackGroupName = GetFallbackGroupName(permissionKey);
		return new PermissionCatalogItem(permissionKey ?? string.Empty, fallbackGroupName, ToPermissionLabel(permissionKey), "No description available yet for this permission.", 99, 99);
	}

	public static bool IsCoreRole(string? roleName)
	{
		if (!string.Equals(roleName, "Super Admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static int CompareRoles(string? left, string? right)
	{
		int roleRank = GetRoleRank(left);
		int roleRank2 = GetRoleRank(right);
		int num = roleRank.CompareTo(roleRank2);
		if (num == 0)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(left, right);
		}
		return num;
	}

	private static int GetRoleRank(string? roleName)
	{
		if (string.Equals(roleName, "Super Admin", StringComparison.OrdinalIgnoreCase))
		{
			return 0;
		}
		if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}
		if (string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase))
		{
			return 2;
		}
		return 10;
	}

	private static string GetFallbackGroupName(string permissionKey)
	{
		int num = permissionKey?.IndexOf('.') ?? (-1);
		string text = ((num >= 0) ? permissionKey.Substring(0, num) : (permissionKey ?? "General"));
		text = text.Replace('_', ' ').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "General";
		}
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
	}

	private static string ToPermissionLabel(string? permissionKey)
	{
		string text = permissionKey ?? string.Empty;
		int num = text.IndexOf('.');
		string text2;
		if (num < 0)
		{
			text2 = text;
		}
		else
		{
			string text3 = text;
			int num2 = num + 1;
			text2 = text3.Substring(num2, text3.Length - num2);
		}
		string text4 = text2.Replace('_', ' ').Replace('.', ' ');
		return string.Join(" ", from word in text4.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
			select char.ToUpperInvariant(word[0]) + word.Substring(1, word.Length - 1));
	}
}
