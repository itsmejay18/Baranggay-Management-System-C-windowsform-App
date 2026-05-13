using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

public class StaffService
{
	private static readonly object LookupCacheSync = new object();

	private static readonly TimeSpan LookupCacheLifetime = TimeSpan.FromMinutes(3.0);

	private static string[]? _cachedRoles;

	private static DateTime _cachedRolesAtUtc;

	private static string[]? _cachedDepartments;

	private static DateTime _cachedDepartmentsAtUtc;

	public async Task<DataTable> GetStaffsAsync(string? search = null, string? role = null, string? status = null, string? department = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("SELECT ua.user_id, ua.username, ua.full_name, ua.first_name, ua.middle_name, ua.last_name, ");
		stringBuilder.Append("COALESCE(r.name, 'Staff') AS role, ");
		stringBuilder.Append("ua.is_active, ua.email, ua.contact_no, ua.position, ua.department, ua.photo_url, ");
		stringBuilder.Append("ua.last_login_at AS last_login, ua.created_at ");
		stringBuilder.Append("FROM user_account ua ");
		stringBuilder.Append("LEFT JOIN user_role ur ON ur.user_id = ua.user_id ");
		stringBuilder.Append("LEFT JOIN role r ON r.role_id = ur.role_id ");
		stringBuilder.Append("WHERE 1 = 1 ");
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		if (!string.IsNullOrWhiteSpace(search))
		{
			stringBuilder.Append("AND (ua.username LIKE @q OR ua.full_name LIKE @q OR ua.first_name LIKE @q ");
			stringBuilder.Append("OR ua.middle_name LIKE @q OR ua.last_name LIKE @q OR ua.email LIKE @q ");
			stringBuilder.Append("OR ua.contact_no LIKE @q OR ua.position LIKE @q OR ua.department LIKE @q) ");
			dictionary["@q"] = "%" + search.Trim() + "%";
		}
		if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "All Roles", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.Append("AND COALESCE(r.name, 'Staff') = @role ");
			dictionary["@role"] = role.Trim();
		}
		if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All Statuses", StringComparison.OrdinalIgnoreCase))
		{
			if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
			{
				stringBuilder.Append("AND ua.is_active = 1 ");
			}
			else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
			{
				stringBuilder.Append("AND ua.is_active = 0 ");
			}
		}
		if (!string.IsNullOrWhiteSpace(department) && !string.Equals(department, "All Departments", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.Append("AND ua.department = @department ");
			dictionary["@department"] = department.Trim();
		}
		stringBuilder.Append("ORDER BY ua.is_active DESC, ua.last_name ASC, ua.first_name ASC, ua.username ASC ");
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync(stringBuilder.ToString(), BuildParameters(dictionary));
		NormalizeStaffTable(obj);
		return obj;
	}

	public async Task<IReadOnlyList<string>> GetRoleOptionsAsync(bool forceRefresh = false)
	{
		if (!forceRefresh && TryGetCachedLookup(_cachedRoles, _cachedRolesAtUtc, out IReadOnlyList<string> lookup))
		{
			return lookup;
		}
		List<string> list = (from row in (await DatabaseManagerAsync.LoadTableAsync("SELECT name FROM role WHERE name IS NOT NULL AND TRIM(name) <> '' ORDER BY name ASC")).AsEnumerable()
			select Convert.ToString(row["name"])?.Trim() into name
			where !string.IsNullOrWhiteSpace(name)
			select name).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList();
		list.Sort(CompareRoles);
		CacheRoleOptions(list);
		return list;
	}

	public async Task<IReadOnlyList<string>> GetDepartmentOptionsAsync(bool forceRefresh = false)
	{
		if (!forceRefresh && TryGetCachedLookup(_cachedDepartments, _cachedDepartmentsAtUtc, out IReadOnlyList<string> lookup))
		{
			return lookup;
		}
		string[] array = (from row in (await DatabaseManagerAsync.LoadTableAsync("SELECT DISTINCT department FROM user_account WHERE department IS NOT NULL AND TRIM(department) <> '' ORDER BY department ASC")).AsEnumerable()
			select Convert.ToString(row["department"])?.Trim() into name
			where !string.IsNullOrWhiteSpace(name)
			select name).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string name) => name, StringComparer.OrdinalIgnoreCase).Cast<string>()
			.ToArray();
		CacheDepartmentOptions(array);
		return array;
	}

	public async Task<bool> UsernameExistsAsync(string username, int excludeUserId = 0)
	{
		if (string.IsNullOrWhiteSpace(username))
		{
			return false;
		}
		return await DatabaseManagerAsync.ExecuteScalarAsync<int>("\n                SELECT COUNT(*)\n                FROM user_account\n                WHERE username = @username\n                  AND (@excludeUserId = 0 OR user_id <> @excludeUserId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@username", (object)username.Trim());
			cmd.Parameters.AddWithValue("@excludeUserId", (object)excludeUserId);
		}) > 0;
	}

	public async Task<StaffProfileDetails?> GetStaffDetailsAsync(int staffId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n                SELECT ua.user_id,\n                       ua.username,\n                       ua.full_name,\n                       ua.first_name,\n                       ua.middle_name,\n                       ua.last_name,\n                       ua.email,\n                       ua.contact_no,\n                       ua.position,\n                       ua.department,\n                       ua.photo_url,\n                       ua.is_active,\n                       ua.last_login_at,\n                       ua.created_at,\n                       COALESCE(r.name, 'Staff') AS role_name,\n                       COALESCE(r.description, '') AS role_description\n                FROM user_account ua\n                LEFT JOIN user_role ur ON ur.user_id = ua.user_id\n                LEFT JOIN role r ON r.role_id = ur.role_id\n                WHERE ua.user_id = @userId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@userId", (object)staffId);
		});
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		DataRow dataRow = dataTable.Rows[0];
		StaffProfileDetails details = new StaffProfileDetails
		{
			UserId = staffId,
			Username = (Convert.ToString(dataRow["username"]) ?? string.Empty),
			FullName = BuildDisplayName(dataRow),
			FirstName = (Convert.ToString(dataRow["first_name"]) ?? string.Empty),
			MiddleName = (Convert.ToString(dataRow["middle_name"]) ?? string.Empty),
			LastName = (Convert.ToString(dataRow["last_name"]) ?? string.Empty),
			Email = (Convert.ToString(dataRow["email"]) ?? string.Empty),
			ContactNumber = (Convert.ToString(dataRow["contact_no"]) ?? string.Empty),
			Position = (Convert.ToString(dataRow["position"]) ?? string.Empty),
			Department = (Convert.ToString(dataRow["department"]) ?? string.Empty),
			PhotoUrl = (Convert.ToString(dataRow["photo_url"]) ?? string.Empty),
			IsActive = (dataRow["is_active"] != DBNull.Value && Convert.ToInt32(dataRow["is_active"], CultureInfo.InvariantCulture) == 1),
			LastLoginAt = ((dataRow["last_login_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(dataRow["last_login_at"], CultureInfo.InvariantCulture))),
			CreatedAt = ((dataRow["created_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(dataRow["created_at"], CultureInfo.InvariantCulture))),
			RoleName = (Convert.ToString(dataRow["role_name"]) ?? "Staff"),
			RoleDescription = (Convert.ToString(dataRow["role_description"]) ?? string.Empty)
		};
		foreach (DataRow row in (await DatabaseManagerAsync.LoadTableAsync("\n                SELECT rp.permission_key, rp.is_allowed\n                FROM user_account ua\n                LEFT JOIN user_role ur ON ur.user_id = ua.user_id\n                LEFT JOIN role_permission rp ON rp.role_id = ur.role_id\n                WHERE ua.user_id = @userId\n                ORDER BY rp.permission_key ASC", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@userId", (object)staffId);
		})).Rows)
		{
			string text = Convert.ToString(row["permission_key"]) ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(text))
			{
				details.Permissions.Add(new StaffPermissionGrant
				{
					GroupName = GetPermissionGroupName(text),
					PermissionKey = text,
					IsAllowed = (row["is_allowed"] != DBNull.Value && Convert.ToInt32(row["is_allowed"], CultureInfo.InvariantCulture) == 1)
				});
			}
		}
		return details;
	}

	public async Task AddStaffAsync(Staff staff, string roleName = "Staff")
	{
		Dictionary<string, object> parameters = new Dictionary<string, object>
		{
			["@barangayId"] = UserSession.BarangayId,
			["@username"] = staff.Username.Trim(),
			["@passwordHash"] = staff.PasswordHash,
			["@firstName"] = staff.FirstName.Trim(),
			["@middleName"] = staff.MiddleName?.Trim() ?? string.Empty,
			["@lastName"] = staff.LastName.Trim(),
			["@fullName"] = ComposeFullName(staff.FirstName, staff.MiddleName, staff.LastName, staff.Username),
			["@email"] = staff.Email?.Trim() ?? string.Empty,
			["@contactNo"] = staff.ContactNumber?.Trim() ?? string.Empty,
			["@position"] = staff.Position?.Trim() ?? string.Empty,
			["@department"] = staff.Department?.Trim() ?? string.Empty,
			["@isActive"] = staff.IsActive
		};
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO user_account\n                    (barangay_id, username, password_hash, first_name, middle_name, last_name, full_name,\n                     email, contact_no, position, department, is_active, created_at, updated_at)\n                VALUES\n                    (@barangayId, @username, @passwordHash, @firstName, @middleName, @lastName, @fullName,\n                     @email, @contactNo, @position, @department, @isActive, NOW(), NOW())", BuildParameters(parameters));
		int userId = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT user_id\n                  FROM user_account\n                  WHERE barangay_id = @barangayId AND username = @username\n                  ORDER BY user_id DESC\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)UserSession.BarangayId);
			cmd.Parameters.AddWithValue("@username", (object)staff.Username.Trim());
		});
		if (userId <= 0)
		{
			throw new InvalidOperationException("Unable to resolve the newly created staff account.");
		}
		await EnsureUserRoleAsync(userId, await ResolveRoleIdAsync(roleName));
		InvalidateLookupCaches();
	}

	public async Task UpdateStaffAsync(Staff staff)
	{
		Dictionary<string, object> parameters = new Dictionary<string, object>
		{
			["@userId"] = staff.StaffId,
			["@firstName"] = staff.FirstName.Trim(),
			["@middleName"] = staff.MiddleName?.Trim() ?? string.Empty,
			["@lastName"] = staff.LastName.Trim(),
			["@fullName"] = ComposeFullName(staff.FirstName, staff.MiddleName, staff.LastName, staff.Username),
			["@email"] = staff.Email?.Trim() ?? string.Empty,
			["@contactNo"] = staff.ContactNumber?.Trim() ?? string.Empty,
			["@position"] = staff.Position?.Trim() ?? string.Empty,
			["@department"] = staff.Department?.Trim() ?? string.Empty,
			["@isActive"] = staff.IsActive
		};
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                UPDATE user_account\n                SET first_name = @firstName,\n                    middle_name = @middleName,\n                    last_name = @lastName,\n                    full_name = @fullName,\n                    email = @email,\n                    contact_no = @contactNo,\n                    position = @position,\n                    department = @department,\n                    is_active = @isActive,\n                    updated_at = NOW()\n                WHERE user_id = @userId", BuildParameters(parameters));
		if (!string.IsNullOrWhiteSpace(staff.Role))
		{
			int roleId = await ResolveRoleIdAsync(staff.Role);
			await EnsureUserRoleAsync(staff.StaffId, roleId);
		}
		InvalidateLookupCaches();
	}

	public async Task SetStaffActiveStatusAsync(int staffId, bool isActive)
	{
		await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE user_account SET is_active = @isActive, updated_at = NOW() WHERE user_id = @userId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@isActive", (object)isActive);
			cmd.Parameters.AddWithValue("@userId", (object)staffId);
		});
	}

	public async Task DeleteStaffAsync(int staffId)
	{
		await SetStaffActiveStatusAsync(staffId, isActive: false);
	}

	public void InvalidateLookupCaches()
	{
		lock (LookupCacheSync)
		{
			_cachedRoles = null;
			_cachedRolesAtUtc = DateTime.MinValue;
			_cachedDepartments = null;
			_cachedDepartmentsAtUtc = DateTime.MinValue;
		}
	}

	private async Task<int> ResolveRoleIdAsync(string roleName)
	{
		int num = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT role_id FROM role WHERE name = @roleName LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@roleName", (object)roleName.Trim());
		});
		if (num <= 0)
		{
			throw new InvalidOperationException("Role '" + roleName + "' was not found.");
		}
		return num;
	}

	private static async Task EnsureUserRoleAsync(int userId, int roleId)
	{
		if (await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE user_role SET role_id = @roleId WHERE user_id = @userId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@roleId", (object)roleId);
			cmd.Parameters.AddWithValue("@userId", (object)userId);
		}) <= 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO user_role (user_id, role_id) VALUES (@userId, @roleId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@userId", (object)userId);
				cmd.Parameters.AddWithValue("@roleId", (object)roleId);
			});
		}
	}

	private static void NormalizeStaffTable(DataTable table)
	{
		if (!table.Columns.Contains("full_name"))
		{
			table.Columns.Add("full_name", typeof(string));
		}
		foreach (DataRow row in table.Rows)
		{
			row["full_name"] = BuildDisplayName(row);
		}
	}

	private static string BuildDisplayName(DataRow row)
	{
		string text = Convert.ToString(row["full_name"])?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = Convert.ToString(row["first_name"])?.Trim() ?? string.Empty;
		string text3 = Convert.ToString(row["middle_name"])?.Trim() ?? string.Empty;
		string text4 = Convert.ToString(row["last_name"])?.Trim() ?? string.Empty;
		string text5 = string.Join(" ", new string[3] { text2, text3, text4 }.Where((string part) => !string.IsNullOrWhiteSpace(part))).Trim();
		if (!string.IsNullOrWhiteSpace(text5))
		{
			return text5;
		}
		return Convert.ToString(row["username"])?.Trim() ?? "Unnamed Staff";
	}

	private static string ComposeFullName(string? firstName, string? middleName, string? lastName, string? fallbackUsername)
	{
		string text = string.Join(" ", from part in new string[3] { firstName, middleName, lastName }
			select part?.Trim() into part
			where !string.IsNullOrWhiteSpace(part)
			select part).Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return fallbackUsername?.Trim() ?? string.Empty;
	}

	private static string GetPermissionGroupName(string permissionKey)
	{
		int num = permissionKey.IndexOf('.');
		string text = ((num >= 0) ? permissionKey.Substring(0, num) : permissionKey);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "General";
		}
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.Replace('_', ' '));
	}

	private static int CompareRoles(string? left, string? right)
	{
		int roleRank = GetRoleRank(left);
		int roleRank2 = GetRoleRank(right);
		int num = roleRank.CompareTo(roleRank2);
		if (num != 0)
		{
			return num;
		}
		return StringComparer.OrdinalIgnoreCase.Compare(left, right);
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

	private static Action<MySqlCommand> BuildParameters(Dictionary<string, object?> parameters)
	{
		return delegate(MySqlCommand cmd)
		{
			foreach (var (text2, obj2) in parameters)
			{
				cmd.Parameters.AddWithValue(text2, obj2 ?? DBNull.Value);
			}
		};
	}

	private static bool TryGetCachedLookup(string[]? values, DateTime cachedAtUtc, out IReadOnlyList<string>? lookup)
	{
		lock (LookupCacheSync)
		{
			if (values != null && DateTime.UtcNow - cachedAtUtc <= LookupCacheLifetime)
			{
				lookup = values.ToArray();
				return true;
			}
		}
		lookup = null;
		return false;
	}

	private static void CacheRoleOptions(IEnumerable<string> roles)
	{
		lock (LookupCacheSync)
		{
			_cachedRoles = roles.ToArray();
			_cachedRolesAtUtc = DateTime.UtcNow;
		}
	}

	private static void CacheDepartmentOptions(IEnumerable<string> departments)
	{
		lock (LookupCacheSync)
		{
			_cachedDepartments = departments.ToArray();
			_cachedDepartmentsAtUtc = DateTime.UtcNow;
		}
	}
}
