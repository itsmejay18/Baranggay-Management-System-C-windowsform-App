using System;

namespace baranggaysystem1.ViewModels;

public sealed class BlotterTimelineEventItem
{
	public DateTime Date { get; init; }

	public string Event { get; init; } = string.Empty;

	public string Details { get; init; } = string.Empty;

	public string User { get; init; } = "System";
}
