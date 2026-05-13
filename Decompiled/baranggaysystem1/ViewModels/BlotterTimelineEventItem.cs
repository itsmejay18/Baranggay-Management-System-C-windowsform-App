using System;

namespace baranggaysystem1.ViewModels;

public sealed class BlotterTimelineEventItem
{
	public DateTime Date { get; set; }

	public string Event { get; set; } = string.Empty;

	public string Details { get; set; } = string.Empty;

	public string User { get; set; } = "System";
}
