using System;

namespace baranggaysystem1;

public sealed class ProjectRecord
{
	public int ProjectId { get; set; }

	public string RecordType { get; set; } = "Project";

	public string Name { get; set; } = string.Empty;

	public string Status { get; set; } = "Planned";

	public decimal Budget { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public string Lead { get; set; } = string.Empty;

	public string Remarks { get; set; } = string.Empty;

	public int AttendanceTarget { get; set; }

	public int AttendanceCount { get; set; }

	public DateTime? LastActivityDate { get; set; }

	public string OutcomeStatus { get; set; } = "Pending";

	public string OutcomeSummary { get; set; } = string.Empty;

	public DateTime? CreatedAt { get; set; }

	public string CreatedAtDisplay
	{
		get
		{
			if (!CreatedAt.HasValue)
			{
				return "Date unavailable";
			}
			return CreatedAt.Value.ToString("MMM dd, yyyy");
		}
	}

	public string LastActivityDisplay
	{
		get
		{
			if (!LastActivityDate.HasValue)
			{
				return "No activity date";
			}
			return LastActivityDate.Value.ToString("MMM dd, yyyy");
		}
	}
}
