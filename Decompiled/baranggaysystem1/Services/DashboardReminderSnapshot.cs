using System.Collections.Generic;

namespace baranggaysystem1.Services;

internal sealed class DashboardReminderSnapshot
{
	public IReadOnlyList<DashboardReminderItem> Notifications { get; }

	public IReadOnlyList<DashboardReminderItem> Plans { get; }

	public int UrgentCount { get; }

	public int NotificationCount { get; }

	public int PlanCount { get; }

	public DashboardReminderSnapshot(IReadOnlyList<DashboardReminderItem> notifications, IReadOnlyList<DashboardReminderItem> plans, int urgentCount, int notificationCount, int planCount)
	{
		Notifications = notifications;
		Plans = plans;
		UrgentCount = urgentCount;
		NotificationCount = notificationCount;
		PlanCount = planCount;
	}
}
