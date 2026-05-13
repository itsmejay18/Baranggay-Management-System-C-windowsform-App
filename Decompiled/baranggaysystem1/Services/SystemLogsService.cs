using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal static class SystemLogsService
{
	private sealed class ApplicationLogBuilder
	{
		public DateTime Timestamp { get; set; }

		public string Level { get; set; } = string.Empty;

		public string Message { get; set; } = string.Empty;

		public string Actor { get; set; } = string.Empty;

		public StringBuilder Details { get; } = new StringBuilder();
	}

	private const int DefaultAuditLimit = 800;

	private const int DefaultApplicationLimit = 500;

	private static readonly Regex LogEntryRegex = new Regex("^\\[(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2})\\]\\s+\\[(?<level>[A-Z]+)\\]\\s+(?<message>.*)$", RegexOptions.Compiled);

	public static SystemLogSnapshot LoadSnapshot(int auditLimit = 800, int applicationLimit = 500)
	{
		List<SystemLogEntry> list = LoadAuditEntries(auditLimit);
		List<SystemLogEntry> list2 = LoadApplicationEntries(applicationLimit);
		List<SystemLogEntry> list3 = (from entry in list.Concat(list2)
			orderby entry.Timestamp descending, entry.RecordId.GetValueOrDefault() descending
			select entry).ToList();
		return new SystemLogSnapshot(errorCount: list3.Count((SystemLogEntry entry) => string.Equals(entry.CategoryDisplay, "ERROR", StringComparison.OrdinalIgnoreCase)), activeUsers: (from entry in list3
			where !string.IsNullOrWhiteSpace(entry.Actor)
			select entry.Actor.Trim()).Distinct<string>(StringComparer.OrdinalIgnoreCase).Count(), moduleCount: (from entry in list3
			select entry.ModuleDisplay.Trim() into value
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct<string>(StringComparer.OrdinalIgnoreCase).Count(), entries: list3, auditCount: list.Count, applicationCount: list2.Count);
	}

	public static string GetApplicationLogDirectory()
	{
		return AppLogger.GetLogDirectoryPath();
	}

	private static List<SystemLogEntry> LoadAuditEntries(int limit)
	{
		try
		{
			AuditTrailService.EnsureSchema();
			DataTable dataTable = TryLoadAuditTable(limit);
			List<SystemLogEntry> list = new List<SystemLogEntry>(dataTable.Rows.Count);
			foreach (DataRow row in dataTable.Rows)
			{
				DateTime timestamp = ParseDateTime(row["action_at"]);
				string text = ReadText(row, "actor_name");
				string text2 = ReadText(row, "action_by");
				if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
				{
					text = "User #" + text2;
				}
				string action = ReadText(row, "action").Trim();
				string entityType = ReadText(row, "entity_type").Trim();
				string text3 = ReadText(row, "notes");
				string beforeJson = ReadText(row, "before_json");
				string afterJson = ReadText(row, "after_json");
				string summary = ((!string.IsNullOrWhiteSpace(text3)) ? text3.Trim() : BuildAuditSummary(action, entityType, ReadText(row, "entity_id")));
				list.Add(new SystemLogEntry
				{
					RecordId = TryReadLong(row["audit_id"]),
					Timestamp = timestamp,
					Source = SystemLogSource.AuditTrail,
					Level = "AUDIT",
					Module = ToTitleCase(ReadText(row, "module")),
					Action = action,
					Actor = text,
					Summary = summary,
					Details = BuildAuditDetails(action, entityType, ReadText(row, "entity_id"), beforeJson, afterJson),
					Notes = text3,
					EntityType = entityType,
					EntityId = ReadText(row, "entity_id"),
					BeforeJson = beforeJson,
					AfterJson = afterJson,
					FileName = string.Empty
				});
			}
			return list;
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to load audit trail feed for System Logs.", ex);
			return new List<SystemLogEntry>();
		}
	}

	private static DataTable TryLoadAuditTable(int limit)
	{
		Exception ex = null;
		string[] array = new string[2] { "SELECT a.audit_id,\n                     a.action_at,\n                     a.module,\n                     a.entity_type,\n                     a.entity_id,\n                     a.action,\n                     a.notes,\n                     a.action_by,\n                     a.before_json,\n                     a.after_json,\n                     COALESCE(NULLIF(ua.full_name, ''), NULLIF(ua.username, ''), '') AS actor_name\n              FROM audit_trail a\n              LEFT JOIN user_account ua ON ua.user_id = a.action_by\n              ORDER BY a.action_at DESC\n              LIMIT @limit", "SELECT audit_id,\n                     action_at,\n                     module,\n                     entity_type,\n                     entity_id,\n                     action,\n                     notes,\n                     action_by,\n                     before_json,\n                     after_json,\n                     '' AS actor_name\n              FROM audit_trail\n              ORDER BY action_at DESC\n              LIMIT @limit" };
		foreach (string sql in array)
		{
			try
			{
				return DbHelper.LoadTable(sql, delegate(MySqlCommand cmd)
				{
					cmd.Parameters.AddWithValue("@limit", (object)limit);
				});
			}
			catch (Exception ex2) when (IsMissingTable(ex2))
			{
				ex = ex2;
			}
		}
		if (ex != null)
		{
			throw ex;
		}
		return new DataTable();
	}

	private static List<SystemLogEntry> LoadApplicationEntries(int limit)
	{
		try
		{
			string applicationLogDirectory = GetApplicationLogDirectory();
			if (!Directory.Exists(applicationLogDirectory))
			{
				return new List<SystemLogEntry>();
			}
			List<SystemLogEntry> list = new List<SystemLogEntry>();
			foreach (string item in Directory.EnumerateFiles(applicationLogDirectory, "app-*.log", SearchOption.TopDirectoryOnly).OrderByDescending(File.GetLastWriteTime))
			{
				list.AddRange(ParseApplicationLogFile(item));
				if (list.Count >= limit * 2)
				{
					break;
				}
			}
			return list.OrderByDescending((SystemLogEntry entry) => entry.Timestamp).Take(limit).ToList();
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to load application log feed for System Logs.", ex);
			return new List<SystemLogEntry>();
		}
	}

	private static IEnumerable<SystemLogEntry> ParseApplicationLogFile(string filePath)
	{
		List<SystemLogEntry> list = new List<SystemLogEntry>();
		ApplicationLogBuilder applicationLogBuilder = null;
		foreach (string item in File.ReadLines(filePath))
		{
			Match match = LogEntryRegex.Match(item);
			if (match.Success)
			{
				FinalizeCurrent(list, applicationLogBuilder, filePath);
				applicationLogBuilder = new ApplicationLogBuilder
				{
					Timestamp = (DateTime.TryParseExact(match.Groups["timestamp"].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : DateTime.MinValue),
					Level = match.Groups["level"].Value.Trim(),
					Message = match.Groups["message"].Value.Trim()
				};
			}
			else
			{
				if (applicationLogBuilder == null)
				{
					continue;
				}
				if (item.StartsWith("  User:", StringComparison.OrdinalIgnoreCase))
				{
					ApplicationLogBuilder applicationLogBuilder2 = applicationLogBuilder;
					string text = item;
					int length = "  User:".Length;
					applicationLogBuilder2.Actor = text.Substring(length, text.Length - length).Trim();
					continue;
				}
				string value = item.TrimEnd();
				if (!string.IsNullOrWhiteSpace(value))
				{
					if (applicationLogBuilder.Details.Length > 0)
					{
						applicationLogBuilder.Details.AppendLine();
					}
					applicationLogBuilder.Details.Append(value);
				}
			}
		}
		FinalizeCurrent(list, applicationLogBuilder, filePath);
		return list;
	}

	private static void FinalizeCurrent(ICollection<SystemLogEntry> entries, ApplicationLogBuilder? current, string filePath)
	{
		if (current != null)
		{
			string text = current.Message.Trim();
			string details = current.Details.ToString().Trim();
			entries.Add(new SystemLogEntry
			{
				RecordId = null,
				Timestamp = current.Timestamp,
				Source = SystemLogSource.ApplicationLog,
				Level = current.Level,
				Module = InferApplicationModule(text),
				Action = "LOG",
				Actor = current.Actor.Trim(),
				Summary = text,
				Details = details,
				Notes = string.Empty,
				EntityType = "application_log",
				EntityId = string.Empty,
				BeforeJson = string.Empty,
				AfterJson = string.Empty,
				FileName = Path.GetFileName(filePath)
			});
		}
	}

	private static string BuildAuditSummary(string action, string entityType, string entityId)
	{
		string text = (string.IsNullOrWhiteSpace(action) ? "Updated" : ToTitleCase(action));
		string text2 = (string.IsNullOrWhiteSpace(entityType) ? "record" : entityType.Replace('_', ' '));
		if (!string.IsNullOrWhiteSpace(entityId))
		{
			return $"{text} {text2} #{entityId}.";
		}
		return text + " " + text2 + ".";
	}

	private static string BuildAuditDetails(string action, string entityType, string entityId, string beforeJson, string afterJson)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(action) || !string.IsNullOrWhiteSpace(entityType))
		{
			string text = (string.IsNullOrWhiteSpace(entityId) ? (ToTitleCase(action) + " " + entityType).Trim() : $"{ToTitleCase(action)} {entityType} #{entityId}".Trim());
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text.Trim());
			}
		}
		if (!string.IsNullOrWhiteSpace(beforeJson))
		{
			list.Add("Captured previous state.");
		}
		if (!string.IsNullOrWhiteSpace(afterJson))
		{
			list.Add("Captured updated state.");
		}
		return string.Join(Environment.NewLine, list.Where((string value) => !string.IsNullOrWhiteSpace(value)));
	}

	private static string InferApplicationModule(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return "Application";
		}
		if (message.StartsWith("[", StringComparison.Ordinal))
		{
			int num = message.IndexOf(']');
			if (num > 1 && num <= 32)
			{
				return message.Substring(1, num - 1).Trim();
			}
		}
		int num2 = message.IndexOf(':');
		if (num2 > 0 && num2 <= 28)
		{
			string text = message.Substring(0, num2).Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return "Application";
	}

	private static long? TryReadLong(object value)
	{
		if (value == DBNull.Value)
		{
			return null;
		}
		try
		{
			return Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return null;
		}
	}

	private static DateTime ParseDateTime(object value)
	{
		if (value is DateTime)
		{
			return (DateTime)value;
		}
		if (!DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
		{
			return DateTime.MinValue;
		}
		return result;
	}

	private static string ReadText(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(row[columnName], CultureInfo.InvariantCulture) ?? string.Empty;
	}

	private static bool IsMissingTable(Exception exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			string text = ex.Message ?? string.Empty;
			if (text.IndexOf("doesn't exist", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("unknown table", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("no such table", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string ToTitleCase(string? value)
	{
		string text = value?.Replace('_', ' ').Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
	}
}
