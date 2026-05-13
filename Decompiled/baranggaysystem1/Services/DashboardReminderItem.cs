using System;

namespace baranggaysystem1.Services;

internal sealed class DashboardReminderItem
{
	public string Category { get; }

	public string Title { get; }

	public string Description { get; }

	public string Footnote { get; }

	public string Route { get; }

	public string ActionLabel { get; }

	public DashboardReminderSeverity Severity { get; }

	public int SortRank { get; }

	public DateTime? SortDate { get; }

	public DashboardReminderItem(string Category, string Title, string Description, string Footnote, string Route, string ActionLabel, DashboardReminderSeverity Severity, int SortRank, DateTime? SortDate)
	{
		this.Category = Category;
		this.Title = Title;
		this.Description = Description;
		this.Footnote = Footnote;
		this.Route = Route;
		this.ActionLabel = ActionLabel;
		this.Severity = Severity;
		this.SortRank = SortRank;
		this.SortDate = SortDate;
	}
}
