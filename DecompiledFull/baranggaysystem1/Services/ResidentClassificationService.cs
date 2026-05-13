using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class ResidentClassificationService
{
	private static readonly string[] AllowedTypes = new string[2] { "TAG", "CATEGORY" };

	private static readonly string[] AllowedStatuses = new string[2] { "ACTIVE", "ARCHIVED" };

	private readonly ResidentsModuleDataService _residentService = new ResidentsModuleDataService();

	public async Task<IReadOnlyList<ResidentClassificationRecord>> GetClassificationsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		await EnsureSchemaReadyAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		List<ResidentClassificationRecord> records = (await DatabaseManagerAsync.LoadTableAsync("SELECT classification_id,\n                     barangay_id,\n                     COALESCE(classification_type, 'TAG') AS classification_type,\n                     COALESCE(classification_key, '') AS classification_key,\n                     COALESCE(name, '') AS name,\n                     COALESCE(description, '') AS description,\n                     COALESCE(color_hex, '#3B82F6') AS color_hex,\n                     COALESCE(status, 'ACTIVE') AS status,\n                     COALESCE(is_system, 0) AS is_system,\n                     COALESCE(sort_order, 0) AS sort_order,\n                     DATE_FORMAT(created_at, '%Y-%m-%d %h:%i %p') AS created_at_display\n              FROM resident_classification\n              WHERE barangay_id = @barangayId\n              ORDER BY CASE UPPER(COALESCE(status, 'ACTIVE'))\n                    WHEN 'ACTIVE' THEN 0\n                    ELSE 1\n                  END,\n                  CASE UPPER(COALESCE(classification_type, 'TAG'))\n                    WHEN 'CATEGORY' THEN 0\n                    ELSE 1\n                  END,\n                  COALESCE(sort_order, 0),\n                  COALESCE(name, '')", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable().Select(MapRecord).ToList();
		await AttachUsageCountsAsync(records, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return records;
	}

	public async Task<ResidentClassificationRecord?> GetClassificationAsync(int classificationId, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (classificationId <= 0)
		{
			return null;
		}
		return (await GetClassificationsAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((ResidentClassificationRecord record) => record.ClassificationId == classificationId);
	}

	public async Task<int> SaveClassificationAsync(ResidentClassificationRecord record, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!CanManageClassifications())
		{
			throw new UnauthorizedAccessException("You do not have permission to manage tags and categories.");
		}
		await EnsureSchemaReadyAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		ResidentClassificationRecord sanitized = await SanitizeForSaveAsync(record, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (sanitized.ClassificationId > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                UPDATE resident_classification\n                SET classification_type = @type,\n                    name = @name,\n                    description = @description,\n                    color_hex = @colorHex,\n                    status = @status,\n                    updated_by_user_id = @userId,\n                    updated_at = NOW()\n                WHERE classification_id = @classificationId\n                  AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
			{
				AddSaveParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@classificationId", (object)sanitized.ClassificationId);
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return sanitized.ClassificationId;
		}
		int sortOrder = await ResolveNextSortOrderAsync(sanitized.ClassificationType, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n            INSERT INTO resident_classification\n                (barangay_id, classification_type, classification_key, name, description, color_hex, status, is_system, sort_order, created_by_user_id, updated_by_user_id, created_at, updated_at)\n            VALUES\n                (@barangayId, @type, NULL, @name, @description, @colorHex, @status, 0, @sortOrder, @userId, @userId, NOW(), NOW())", delegate(MySqlCommand cmd)
		{
			AddSaveParameters(cmd, sanitized);
			cmd.Parameters.AddWithValue("@sortOrder", (object)sortOrder);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT classification_id\n              FROM resident_classification\n              WHERE barangay_id = @barangayId\n                AND classification_type = @type\n                AND LOWER(name) = LOWER(@name)\n              ORDER BY classification_id DESC\n              LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
			cmd.Parameters.AddWithValue("@type", (object)sanitized.ClassificationType);
			cmd.Parameters.AddWithValue("@name", (object)sanitized.Name);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task SetStatusAsync(int classificationId, string status, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!CanManageClassifications())
		{
			throw new UnauthorizedAccessException("You do not have permission to manage tags and categories.");
		}
		ResidentClassificationRecord residentClassificationRecord = await GetClassificationAsync(classificationId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (residentClassificationRecord == null)
		{
			throw new InvalidOperationException("The selected tag or category could not be found.");
		}
		residentClassificationRecord.Status = NormalizeStatus(status);
		await SaveClassificationAsync(residentClassificationRecord, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task DeleteClassificationAsync(int classificationId, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!CanManageClassifications())
		{
			throw new UnauthorizedAccessException("You do not have permission to manage tags and categories.");
		}
		ResidentClassificationRecord obj = (await GetClassificationAsync(classificationId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("The selected tag or category could not be found.");
		if (obj.IsSystem)
		{
			throw new InvalidOperationException("System classifications cannot be deleted.");
		}
		if (obj.UsageCount > 0)
		{
			throw new InvalidOperationException("This classification is still used by one or more resident records.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM resident_classification\n              WHERE classification_id = @classificationId\n                AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@classificationId", (object)classificationId);
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static bool CanManageClassifications()
	{
		return Permissions.IsAdmin;
	}

	private static async Task EnsureSchemaReadyAsync(CancellationToken cancellationToken)
	{
		if (OfflineDatabaseSupport.IsOffline || DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false))
		{
			if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
			{
				throw new InvalidOperationException("Offline tags and categories storage is not available right now.");
			}
			if (!OfflineDatabaseSupport.IsOffline)
			{
				OfflineDatabaseSupport.ActivateOfflineMode();
			}
		}
		else
		{
			await Task.Run(delegate
			{
				SchemaGuard.EnsureDatabaseReady();
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task<ResidentClassificationRecord> SanitizeForSaveAsync(ResidentClassificationRecord record, CancellationToken cancellationToken)
	{
		if (record == null)
		{
			throw new InvalidOperationException("Classification record is required.");
		}
		ResidentClassificationRecord residentClassificationRecord = ((record.ClassificationId <= 0) ? null : (await GetClassificationAsync(record.ClassificationId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
		ResidentClassificationRecord existing = residentClassificationRecord;
		if (record.ClassificationId > 0 && existing == null)
		{
			throw new InvalidOperationException("The selected tag or category could not be found.");
		}
		string name = NormalizeRequired(record.Name, "Name is required.", 100);
		string type = ((existing?.IsSystem ?? false) ? existing.ClassificationType : NormalizeOption(record.ClassificationType, AllowedTypes, "TAG"));
		string status = NormalizeStatus(record.Status);
		string colorHex = NormalizeColor(record.ColorHex);
		string description = NormalizeOptional(record.Description, 255);
		bool flag = string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
		if (flag)
		{
			flag = await HasActiveDuplicateNameAsync(record.ClassificationId, type, name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (flag)
		{
			throw new InvalidOperationException("An active tag or category with the same name already exists.");
		}
		return new ResidentClassificationRecord
		{
			ClassificationId = record.ClassificationId,
			BarangayId = GetBarangayId(),
			ClassificationType = type,
			ClassificationKey = (existing?.ClassificationKey ?? string.Empty),
			Name = name,
			Description = description,
			ColorHex = colorHex,
			Status = status,
			IsSystem = (existing?.IsSystem ?? false),
			SortOrder = (existing?.SortOrder ?? record.SortOrder)
		};
	}

	private static async Task<bool> HasActiveDuplicateNameAsync(int classificationId, string type, string name, CancellationToken cancellationToken)
	{
		return await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\n              FROM resident_classification\n              WHERE barangay_id = @barangayId\n                AND classification_id <> @classificationId\n                AND classification_type = @type\n                AND LOWER(name) = LOWER(@name)\n                AND UPPER(COALESCE(status, 'ACTIVE')) = 'ACTIVE'", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
			cmd.Parameters.AddWithValue("@classificationId", (object)classificationId);
			cmd.Parameters.AddWithValue("@type", (object)type);
			cmd.Parameters.AddWithValue("@name", (object)name);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false) > 0;
	}

	private static async Task<int> ResolveNextSortOrderAsync(string type, CancellationToken cancellationToken)
	{
		return await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(sort_order), 0)\n              FROM resident_classification\n              WHERE barangay_id = @barangayId\n                AND classification_type = @type", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
			cmd.Parameters.AddWithValue("@type", (object)type);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false) + 10;
	}

	private async Task AttachUsageCountsAsync(List<ResidentClassificationRecord> records, CancellationToken cancellationToken)
	{
		if (records.Count == 0)
		{
			return;
		}
		Dictionary<string, int> dictionary = await _residentService.LoadResidentCategoryCountsAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		foreach (ResidentClassificationRecord record in records)
		{
			if (!string.IsNullOrWhiteSpace(record.ClassificationKey) && dictionary.TryGetValue(record.ClassificationKey, out var value))
			{
				record.UsageCount = value;
			}
		}
	}

	private static ResidentClassificationRecord MapRecord(DataRow row)
	{
		return new ResidentClassificationRecord
		{
			ClassificationId = ReadInt(row, "classification_id"),
			BarangayId = ReadInt(row, "barangay_id"),
			ClassificationType = NormalizeOption(ReadString(row, "classification_type"), AllowedTypes, "TAG"),
			ClassificationKey = ReadString(row, "classification_key"),
			Name = ReadString(row, "name"),
			Description = ReadString(row, "description"),
			ColorHex = NormalizeColor(ReadString(row, "color_hex")),
			Status = NormalizeStatus(ReadString(row, "status")),
			IsSystem = ReadBool(row, "is_system"),
			SortOrder = ReadInt(row, "sort_order"),
			CreatedAtDisplay = ReadString(row, "created_at_display")
		};
	}

	private static void AddSaveParameters(MySqlCommand cmd, ResidentClassificationRecord record)
	{
		cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		cmd.Parameters.AddWithValue("@type", (object)record.ClassificationType);
		cmd.Parameters.AddWithValue("@name", (object)record.Name);
		cmd.Parameters.AddWithValue("@description", ToDbNullable(record.Description));
		cmd.Parameters.AddWithValue("@colorHex", (object)record.ColorHex);
		cmd.Parameters.AddWithValue("@status", (object)record.Status);
		cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
	}

	private static string NormalizeStatus(string? status)
	{
		return NormalizeOption(status, AllowedStatuses, "ACTIVE");
	}

	private static string NormalizeOption(string? value, IReadOnlyCollection<string> allowedValues, string fallback)
	{
		string normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return fallback;
		}
		if (!allowedValues.Any((string allowed) => string.Equals(allowed, normalized, StringComparison.OrdinalIgnoreCase)))
		{
			return fallback;
		}
		return normalized;
	}

	private static string NormalizeRequired(string? value, string message, int maxLength)
	{
		string text = NormalizeOptional(value, maxLength);
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException(message);
		}
		return text;
	}

	private static string NormalizeOptional(string? value, int maxLength)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length > maxLength)
		{
			return text.Substring(0, maxLength);
		}
		return text;
	}

	private static string NormalizeColor(string? value)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length == 7 && text[0] == '#' && text.Skip(1).All(Uri.IsHexDigit))
		{
			return text.ToUpperInvariant();
		}
		return "#3B82F6";
	}

	private static object ToDbNullable(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static object GetUserIdOrNull()
	{
		if (UserSession.UserId <= 0)
		{
			return DBNull.Value;
		}
		return UserSession.UserId;
	}

	private static int GetBarangayId()
	{
		if (UserSession.BarangayId <= 0)
		{
			return 1;
		}
		return UserSession.BarangayId;
	}

	private static int ReadInt(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0;
		}
		return Convert.ToInt32(row[columnName], CultureInfo.InvariantCulture);
	}

	private static bool ReadBool(DataRow row, string columnName)
	{
		return ReadInt(row, columnName) != 0;
	}

	private static string ReadString(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(row[columnName], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
	}
}
