using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class AnnouncementService
{
	private static readonly string[] AllowedPriorities = new string[3] { "Low", "Normal", "High" };

	private static readonly string[] AllowedStatuses = new string[3] { "Draft", "Published", "Archived" };

	public async Task<IReadOnlyList<AnnouncementRecord>> GetRecentAnnouncementsAsync(int limit = 10)
	{
		return (await DatabaseManagerAsync.LoadTableAsync(BuildAnnouncementQuery(limit)).ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable().Select(MapAnnouncement).ToList();
	}

	public async Task<DataTable> GetAnnouncementRegistryAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync(BuildAnnouncementQuery()).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<AnnouncementRecord?> GetAnnouncementAsync(int announcementId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("SELECT announcement_id,\n                         title,\n                         COALESCE(body, '') AS body,\n                         COALESCE(priority, 'Normal') AS priority,\n                         COALESCE(status, 'Published') AS status,\n                         COALESCE(is_pinned, 0) AS is_pinned,\n                         created_at\n                  FROM announcements\n                  WHERE announcement_id = @announcementId\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@announcementId", (object)announcementId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		return (dataTable.Rows.Count == 0) ? null : MapAnnouncement(dataTable.Rows[0]);
	}

	public async Task CreateAnnouncementAsync(AnnouncementRecord record)
	{
		AnnouncementRecord sanitized = Sanitize(record);
		await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO announcements\n                    (title, body, priority, status, is_pinned)\n                  VALUES\n                    (@title, @body, @priority, @status, @isPinned)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@title", (object)sanitized.Title);
			cmd.Parameters.AddWithValue("@body", DbNullIfEmpty(sanitized.Body));
			cmd.Parameters.AddWithValue("@priority", (object)sanitized.Priority);
			cmd.Parameters.AddWithValue("@status", (object)sanitized.Status);
			cmd.Parameters.AddWithValue("@isPinned", (object)(sanitized.IsPinned ? 1 : 0));
		}).ConfigureAwait(continueOnCapturedContext: false);
		AuditTrailService.Log("Announcements", "announcement", sanitized.Title, "CREATE", null, sanitized, "Announcement created from the announcements workflow.");
	}

	public async Task UpdateAnnouncementAsync(AnnouncementRecord record)
	{
		if (record.AnnouncementId <= 0)
		{
			throw new InvalidOperationException("Announcement ID is required for updates.");
		}
		AnnouncementRecord before = await GetAnnouncementAsync(record.AnnouncementId).ConfigureAwait(continueOnCapturedContext: false);
		if (before == null)
		{
			throw new InvalidOperationException("The selected announcement could not be found.");
		}
		AnnouncementRecord sanitized = Sanitize(record);
		sanitized.AnnouncementId = record.AnnouncementId;
		sanitized.CreatedAt = before.CreatedAt;
		await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE announcements\n                  SET title = @title,\n                      body = @body,\n                      priority = @priority,\n                      status = @status,\n                      is_pinned = @isPinned\n                  WHERE announcement_id = @announcementId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@announcementId", (object)sanitized.AnnouncementId);
			cmd.Parameters.AddWithValue("@title", (object)sanitized.Title);
			cmd.Parameters.AddWithValue("@body", DbNullIfEmpty(sanitized.Body));
			cmd.Parameters.AddWithValue("@priority", (object)sanitized.Priority);
			cmd.Parameters.AddWithValue("@status", (object)sanitized.Status);
			cmd.Parameters.AddWithValue("@isPinned", (object)(sanitized.IsPinned ? 1 : 0));
		}).ConfigureAwait(continueOnCapturedContext: false);
		AuditTrailService.Log("Announcements", "announcement", sanitized.AnnouncementId, "UPDATE", before, sanitized, "Announcement updated from the announcements workflow.");
	}

	public async Task DeleteAnnouncementAsync(int announcementId)
	{
		AnnouncementRecord before = await GetAnnouncementAsync(announcementId).ConfigureAwait(continueOnCapturedContext: false);
		if (before == null)
		{
			throw new InvalidOperationException("The selected announcement could not be found.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM announcements WHERE announcement_id = @announcementId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@announcementId", (object)announcementId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		AuditTrailService.Log("Announcements", "announcement", announcementId, "DELETE", before, null, "Announcement deleted from the announcements workflow.");
	}

	private static string BuildAnnouncementQuery(int? limit = null)
	{
		string text = "SELECT announcement_id,\n                         title,\n                         COALESCE(body, '') AS body,\n                         COALESCE(priority, 'Normal') AS priority,\n                         COALESCE(status, 'Published') AS status,\n                         COALESCE(is_pinned, 0) AS is_pinned,\n                         created_at\n                  FROM announcements\n                  ORDER BY COALESCE(is_pinned, 0) DESC,\n                           created_at DESC,\n                           announcement_id DESC";
		if (limit.HasValue)
		{
			text += $"\nLIMIT {Math.Clamp(limit.Value, 1, 50)}";
		}
		return text;
	}

	private static AnnouncementRecord Sanitize(AnnouncementRecord record)
	{
		string text = (record.Title ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("Announcement title is required.");
		}
		if (text.Length > 150)
		{
			throw new InvalidOperationException("Announcement title must be 150 characters or fewer.");
		}
		return new AnnouncementRecord
		{
			Title = text,
			Body = TrimToLength(record.Body, 4000),
			Priority = NormalizeOption(record.Priority, AllowedPriorities, "Normal"),
			Status = NormalizeOption(record.Status, AllowedStatuses, "Published"),
			IsPinned = record.IsPinned
		};
	}

	private static AnnouncementRecord MapAnnouncement(DataRow row)
	{
		return new AnnouncementRecord
		{
			AnnouncementId = ReadInt(row, "announcement_id"),
			Title = ReadString(row, "title"),
			Body = ReadString(row, "body"),
			Priority = NormalizeOption(ReadString(row, "priority"), AllowedPriorities, "Normal"),
			Status = NormalizeOption(ReadString(row, "status"), AllowedStatuses, "Published"),
			IsPinned = (ReadInt(row, "is_pinned") != 0),
			CreatedAt = ReadDateTime(row, "created_at")
		};
	}

	private static string NormalizeOption(string? value, IReadOnlyList<string> allowedValues, string fallback)
	{
		string normalized = (value ?? string.Empty).Trim();
		return allowedValues.FirstOrDefault((string option) => string.Equals(option, normalized, StringComparison.OrdinalIgnoreCase)) ?? fallback;
	}

	private static string TrimToLength(string? value, int maxLength)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length > maxLength)
		{
			return text.Substring(0, maxLength);
		}
		return text;
	}

	private static object DbNullIfEmpty(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static int ReadInt(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0;
		}
		return Convert.ToInt32(row[columnName], CultureInfo.InvariantCulture);
	}

	private static string ReadString(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(row[columnName], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
	}

	private static DateTime? ReadDateTime(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return null;
		}
		object obj = row[columnName];
		if (obj is DateTime)
		{
			return (DateTime)obj;
		}
		if (!DateTime.TryParse(Convert.ToString(row[columnName], CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
		{
			return null;
		}
		return result;
	}
}
