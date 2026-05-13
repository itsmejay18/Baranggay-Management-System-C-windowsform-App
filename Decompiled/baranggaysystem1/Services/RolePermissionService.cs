using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class RolePermissionService
{
	public async Task<IReadOnlyList<RolePermissionSummary>> GetRoleSummariesAsync(string? search = null)
	{
		string trimmed = search?.Trim() ?? string.Empty;
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("\n            SELECT r.role_id,\n                   r.name,\n                   COALESCE(r.description, '') AS description,\n                   COUNT(DISTINCT ur.user_id) AS user_count,\n                   SUM(CASE WHEN IFNULL(ua.is_active, 1) = 1 THEN 1 ELSE 0 END) AS active_user_count\n            FROM role r\n            LEFT JOIN user_role ur ON ur.role_id = r.role_id\n            LEFT JOIN user_account ua ON ua.user_id = ur.user_id\n            WHERE (@q = '' OR r.name LIKE @search OR COALESCE(r.description, '') LIKE @search)\n            GROUP BY r.role_id, r.name, r.description", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@q", (object)trimmed);
			cmd.Parameters.AddWithValue("@search", (object)("%" + trimmed + "%"));
		}).ConfigureAwait(continueOnCapturedContext: false);
		List<RolePermissionSummary> list = new List<RolePermissionSummary>();
		foreach (DataRow row in obj.Rows)
		{
			string text = Convert.ToString(row["name"])?.Trim() ?? string.Empty;
			list.Add(new RolePermissionSummary
			{
				RoleId = Convert.ToInt32(row["role_id"]),
				Name = text,
				Description = (Convert.ToString(row["description"])?.Trim() ?? string.Empty),
				UserCount = ((row["user_count"] != DBNull.Value) ? Convert.ToInt32(row["user_count"]) : 0),
				ActiveUserCount = ((row["active_user_count"] != DBNull.Value) ? Convert.ToInt32(row["active_user_count"]) : 0),
				IsCoreRole = PermissionCatalog.IsCoreRole(text),
				IsSuperAdmin = string.Equals(text, "Super Admin", StringComparison.OrdinalIgnoreCase)
			});
		}
		list.Sort((RolePermissionSummary left, RolePermissionSummary right) => PermissionCatalog.CompareRoles(left.Name, right.Name));
		return list;
	}

	public async Task<RolePermissionEditorModel?> GetRoleEditorAsync(int roleId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n            SELECT r.role_id,\n                   r.name,\n                   COALESCE(r.description, '') AS description,\n                   COUNT(DISTINCT ur.user_id) AS user_count,\n                   SUM(CASE WHEN IFNULL(ua.is_active, 1) = 1 THEN 1 ELSE 0 END) AS active_user_count\n            FROM role r\n            LEFT JOIN user_role ur ON ur.role_id = r.role_id\n            LEFT JOIN user_account ua ON ua.user_id = ur.user_id\n            WHERE r.role_id = @roleId\n            GROUP BY r.role_id, r.name, r.description\n            LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@roleId", (object)roleId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		DataRow dataRow = dataTable.Rows[0];
		string text = Convert.ToString(dataRow["name"])?.Trim() ?? string.Empty;
		RolePermissionEditorModel editor = new RolePermissionEditorModel
		{
			RoleId = Convert.ToInt32(dataRow["role_id"]),
			Name = text,
			Description = (Convert.ToString(dataRow["description"])?.Trim() ?? string.Empty),
			UserCount = ((dataRow["user_count"] != DBNull.Value) ? Convert.ToInt32(dataRow["user_count"]) : 0),
			ActiveUserCount = ((dataRow["active_user_count"] != DBNull.Value) ? Convert.ToInt32(dataRow["active_user_count"]) : 0),
			IsCoreRole = PermissionCatalog.IsCoreRole(text),
			IsSuperAdmin = string.Equals(text, "Super Admin", StringComparison.OrdinalIgnoreCase)
		};
		Dictionary<string, bool> dictionary = (from rowValue in (await DatabaseManagerAsync.LoadTableAsync("SELECT permission_key, is_allowed\n              FROM role_permission\n              WHERE role_id = @roleId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@roleId", (object)roleId);
			}).ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable()
			where rowValue.Table.Columns.Contains("permission_key")
			select rowValue).ToDictionary<DataRow, string, bool>((DataRow rowValue) => Convert.ToString(rowValue["permission_key"]) ?? string.Empty, (DataRow rowValue) => rowValue["is_allowed"] != DBNull.Value && Convert.ToInt32(rowValue["is_allowed"]) == 1, StringComparer.OrdinalIgnoreCase);
		foreach (PermissionCatalogItem item in from item in PermissionCatalog.All
			orderby item.GroupOrder, item.ItemOrder
			select item)
		{
			editor.Permissions.Add(new RolePermissionGrantItem
			{
				PermissionKey = item.Key,
				GroupName = item.GroupName,
				Label = item.Label,
				Description = item.Description,
				GroupOrder = item.GroupOrder,
				ItemOrder = item.ItemOrder,
				IsAllowed = (dictionary.TryGetValue(item.Key, out var value) && value)
			});
		}
		return editor;
	}

	public RolePermissionEditorModel CreateNewRoleDraft()
	{
		RolePermissionEditorModel rolePermissionEditorModel = new RolePermissionEditorModel();
		foreach (PermissionCatalogItem item in from item in PermissionCatalog.All
			orderby item.GroupOrder, item.ItemOrder
			select item)
		{
			rolePermissionEditorModel.Permissions.Add(new RolePermissionGrantItem
			{
				PermissionKey = item.Key,
				GroupName = item.GroupName,
				Label = item.Label,
				Description = item.Description,
				GroupOrder = item.GroupOrder,
				ItemOrder = item.ItemOrder,
				IsAllowed = false
			});
		}
		return rolePermissionEditorModel;
	}

	public async Task<IReadOnlyList<string>> GetRoleNameOptionsAsync()
	{
		List<string> list = (from row in (await DatabaseManagerAsync.LoadTableAsync("SELECT name FROM role WHERE name IS NOT NULL AND TRIM(name) <> '' ORDER BY name ASC").ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable()
			select Convert.ToString(row["name"])?.Trim() into name
			where !string.IsNullOrWhiteSpace(name)
			select name).Distinct<string>(StringComparer.OrdinalIgnoreCase).Cast<string>().ToList();
		list.Sort(PermissionCatalog.CompareRoles);
		return list;
	}

	public async Task<int> SaveRoleAsync(RolePermissionEditorModel editor)
	{
		if (editor == null)
		{
			throw new ArgumentNullException("editor");
		}
		string trimmedName = (editor.Name ?? string.Empty).Trim();
		string trimmedDescription = (editor.Description ?? string.Empty).Trim();
		bool isNew = editor.RoleId <= 0;
		RolePermissionSummary rolePermissionSummary = null;
		if (!isNew)
		{
			rolePermissionSummary = (await GetRoleSummariesAsync().ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((RolePermissionSummary role) => role.RoleId == editor.RoleId);
			if (rolePermissionSummary == null)
			{
				throw new InvalidOperationException("The selected role could not be found anymore.");
			}
		}
		if (isNew && string.IsNullOrWhiteSpace(trimmedName))
		{
			throw new InvalidOperationException("Role name is required.");
		}
		string nameToPersist = (isNew ? trimmedName : rolePermissionSummary.Name);
		if (string.IsNullOrWhiteSpace(nameToPersist))
		{
			throw new InvalidOperationException("Role name is required.");
		}
		if (await RoleNameExistsAsync(nameToPersist, editor.RoleId).ConfigureAwait(continueOnCapturedContext: false))
		{
			throw new InvalidOperationException("A role with the same name already exists.");
		}
		if (!isNew)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE role SET description = @description WHERE role_id = @roleId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@description", (object)(string.IsNullOrWhiteSpace(trimmedDescription) ? ((IConvertible)DBNull.Value) : ((IConvertible)trimmedDescription)));
				cmd.Parameters.AddWithValue("@roleId", (object)editor.RoleId);
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO role (name, description) VALUES (@name, @description)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@name", (object)nameToPersist);
				cmd.Parameters.AddWithValue("@description", (object)(string.IsNullOrWhiteSpace(trimmedDescription) ? ((IConvertible)DBNull.Value) : ((IConvertible)trimmedDescription)));
			}).ConfigureAwait(continueOnCapturedContext: false);
			RolePermissionEditorModel rolePermissionEditorModel = editor;
			rolePermissionEditorModel.RoleId = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT role_id FROM role WHERE LOWER(name) = LOWER(@name) ORDER BY role_id DESC LIMIT 1", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@name", (object)nameToPersist);
			}).ConfigureAwait(continueOnCapturedContext: false);
			if (editor.RoleId <= 0)
			{
				throw new InvalidOperationException("The new role was saved, but its ID could not be resolved.");
			}
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM role_permission WHERE role_id = @roleId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@roleId", (object)editor.RoleId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		Dictionary<string, bool> grants = editor.Permissions.Where((RolePermissionGrantItem permission) => !string.IsNullOrWhiteSpace(permission.PermissionKey)).GroupBy<RolePermissionGrantItem, string>((RolePermissionGrantItem permission) => permission.PermissionKey, StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, RolePermissionGrantItem>, string, bool>((IGrouping<string, RolePermissionGrantItem> group) => group.Key, (IGrouping<string, RolePermissionGrantItem> group) => group.Last().IsAllowed, StringComparer.OrdinalIgnoreCase);
		foreach (PermissionCatalogItem catalogItem in from item in PermissionCatalog.All
			orderby item.GroupOrder, item.ItemOrder
			select item)
		{
			bool value;
			bool allowed = grants.TryGetValue(catalogItem.Key, out value) && value;
			await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO role_permission (role_id, permission_key, is_allowed)\n                  VALUES (@roleId, @permissionKey, @allowed)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@roleId", (object)editor.RoleId);
				cmd.Parameters.AddWithValue("@permissionKey", (object)catalogItem.Key);
				cmd.Parameters.AddWithValue("@allowed", (object)(allowed ? 1 : 0));
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		Permissions.Refresh();
		return editor.RoleId;
	}

	public async Task DeleteRoleAsync(int roleId)
	{
		RolePermissionSummary? obj = (await GetRoleSummariesAsync().ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((RolePermissionSummary role) => role.RoleId == roleId) ?? throw new InvalidOperationException("The selected role could not be found anymore.");
		if (obj.IsCoreRole)
		{
			throw new InvalidOperationException("Core roles cannot be deleted.");
		}
		if (obj.UserCount > 0)
		{
			throw new InvalidOperationException("This role is still assigned to one or more user accounts.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM role_permission WHERE role_id = @roleId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@roleId", (object)roleId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM role WHERE role_id = @roleId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@roleId", (object)roleId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		Permissions.Refresh();
	}

	private static async Task<bool> RoleNameExistsAsync(string roleName, int excludeRoleId)
	{
		return await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\n              FROM role\n              WHERE LOWER(name) = LOWER(@name)\n                AND (@excludeRoleId = 0 OR role_id <> @excludeRoleId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@name", (object)roleName);
			cmd.Parameters.AddWithValue("@excludeRoleId", (object)excludeRoleId);
		}).ConfigureAwait(continueOnCapturedContext: false) > 0;
	}
}
