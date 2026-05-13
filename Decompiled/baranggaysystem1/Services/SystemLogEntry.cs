using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace baranggaysystem1.Services;

internal sealed class SystemLogEntry
{
	public long? RecordId { get; set; }

	public DateTime Timestamp { get; set; }

	public SystemLogSource Source { get; set; }

	public string Level { get; set; } = string.Empty;

	public string Module { get; set; } = string.Empty;

	public string Action { get; set; } = string.Empty;

	public string Actor { get; set; } = string.Empty;

	public string Summary { get; set; } = string.Empty;

	public string Details { get; set; } = string.Empty;

	public string Notes { get; set; } = string.Empty;

	public string EntityType { get; set; } = string.Empty;

	public string EntityId { get; set; } = string.Empty;

	public string BeforeJson { get; set; } = string.Empty;

	public string AfterJson { get; set; } = string.Empty;

	public string FileName { get; set; } = string.Empty;

	public string TimestampDisplay
	{
		get
		{
			if (!(Timestamp == DateTime.MinValue))
			{
				return Timestamp.ToString("MMM dd, yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
			}
			return "Unknown";
		}
	}

	public string DateBucket
	{
		get
		{
			if (!(Timestamp == DateTime.MinValue))
			{
				return Timestamp.ToString("dddd, MMM dd, yyyy", CultureInfo.InvariantCulture);
			}
			return "Unknown date";
		}
	}

	public string SourceDisplay
	{
		get
		{
			if (Source != SystemLogSource.AuditTrail)
			{
				return "Application Log";
			}
			return "Audit Trail";
		}
	}

	public string CategoryDisplay
	{
		get
		{
			if (Source != SystemLogSource.AuditTrail)
			{
				if (!string.IsNullOrWhiteSpace(Level))
				{
					return Level.ToUpperInvariant();
				}
				return "INFO";
			}
			if (!string.IsNullOrWhiteSpace(Action))
			{
				return Action.ToUpperInvariant();
			}
			return "AUDIT";
		}
	}

	public string ModuleDisplay
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Module))
			{
				return Module;
			}
			return "General";
		}
	}

	public string ActorDisplay
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Actor))
			{
				return Actor;
			}
			return "System";
		}
	}

	public string EntityDisplay
	{
		get
		{
			if (Source != SystemLogSource.AuditTrail)
			{
				if (!string.IsNullOrWhiteSpace(FileName))
				{
					return FileName;
				}
				return "Application runtime";
			}
			if (string.IsNullOrWhiteSpace(EntityType) && string.IsNullOrWhiteSpace(EntityId))
			{
				return "Audit record";
			}
			if (!string.IsNullOrWhiteSpace(EntityId))
			{
				return EntityType + " #" + EntityId;
			}
			return EntityType;
		}
	}

	public string SearchIndex => string.Join(" ", new string[11]
	{
		SourceDisplay, CategoryDisplay, ModuleDisplay, Action, ActorDisplay, Summary, Details, Notes, EntityType, EntityId,
		FileName
	}.Where((string value) => !string.IsNullOrWhiteSpace(value)));

	public string FullDetailText
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(Notes))
			{
				stringBuilder.AppendLine("Notes");
				stringBuilder.AppendLine(Notes.Trim());
			}
			if (!string.IsNullOrWhiteSpace(Details))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine((Source == SystemLogSource.AuditTrail) ? "Change Details" : "Log Details");
				stringBuilder.AppendLine(Details.Trim());
			}
			if (stringBuilder.Length == 0)
			{
				stringBuilder.Append((Source == SystemLogSource.AuditTrail) ? "No extra audit details were stored for this entry." : "No stack trace or extended details were captured for this log line.");
			}
			return stringBuilder.ToString().Trim();
		}
	}
}
