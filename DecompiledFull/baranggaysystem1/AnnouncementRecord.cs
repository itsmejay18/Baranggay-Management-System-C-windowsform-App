using System;

namespace baranggaysystem1;

public sealed class AnnouncementRecord
{
	public int AnnouncementId { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Body { get; set; } = string.Empty;

	public string Priority { get; set; } = "Normal";

	public string Status { get; set; } = "Published";

	public bool IsPinned { get; set; }

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
}
