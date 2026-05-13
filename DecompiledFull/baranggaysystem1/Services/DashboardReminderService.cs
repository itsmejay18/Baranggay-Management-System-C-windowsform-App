using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class DashboardReminderService
{
	private const int MaxNotificationItems = 8;

	private const int MaxPlanItems = 6;

	public async Task<DashboardReminderSnapshot> LoadSnapshotAsync()
	{
		DateTime now = DateTime.Now;
		List<DashboardReminderItem> notifications = new List<DashboardReminderItem>();
		List<DashboardReminderItem> plans = new List<DashboardReminderItem>();
		List<DashboardReminderItem> list = notifications;
		list.AddRange(await LoadAnnouncementsSafeAsync().ConfigureAwait(continueOnCapturedContext: false));
		list = notifications;
		list.AddRange(await LoadCertificateAlertsSafeAsync(now).ConfigureAwait(continueOnCapturedContext: false));
		list = notifications;
		list.AddRange(await LoadBlotterAlertsSafeAsync(now).ConfigureAwait(continueOnCapturedContext: false));
		list = notifications;
		list.AddRange(await LoadOutboundQueueAlertsSafeAsync().ConfigureAwait(continueOnCapturedContext: false));
		var (collection, collection2) = await LoadProjectItemsSafeAsync(now).ConfigureAwait(continueOnCapturedContext: false);
		notifications.AddRange(collection);
		plans.AddRange(collection2);
		list = plans;
		list.AddRange(await LoadHearingPlansSafeAsync().ConfigureAwait(continueOnCapturedContext: false));
		List<DashboardReminderItem> list2 = (from item in notifications
			orderby item.SortRank, item.SortDate ?? DateTime.MaxValue
			select item).ThenBy<DashboardReminderItem, string>((DashboardReminderItem item) => item.Title, StringComparer.OrdinalIgnoreCase).Take(8).ToList();
		List<DashboardReminderItem> list3 = (from item in plans
			orderby item.SortRank, item.SortDate ?? DateTime.MaxValue
			select item).ThenBy<DashboardReminderItem, string>((DashboardReminderItem item) => item.Title, StringComparer.OrdinalIgnoreCase).Take(6).ToList();
		int urgentCount = list2.Count((DashboardReminderItem item) => item.Severity == DashboardReminderSeverity.Urgent);
		return new DashboardReminderSnapshot(list2, list3, urgentCount, list2.Count, list3.Count);
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadAnnouncementsSafeAsync()
	{
		try
		{
			return await LoadAnnouncementAlertsAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("DashboardReminderService: announcement reminders failed.", ex);
			return Array.Empty<DashboardReminderItem>();
		}
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadCertificateAlertsSafeAsync(DateTime now)
	{
		try
		{
			return await LoadCertificateAlertsAsync(now).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("DashboardReminderService: certificate reminders failed.", ex);
			return Array.Empty<DashboardReminderItem>();
		}
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadBlotterAlertsSafeAsync(DateTime now)
	{
		try
		{
			return await LoadBlotterAlertsAsync(now).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("DashboardReminderService: blotter reminders failed.", ex);
			return Array.Empty<DashboardReminderItem>();
		}
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadOutboundQueueAlertsSafeAsync()
	{
		try
		{
			return await LoadOutboundQueueAlertsAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("DashboardReminderService: outbound notification reminders failed.", ex);
			return Array.Empty<DashboardReminderItem>();
		}
	}

	private static async Task<(IReadOnlyList<DashboardReminderItem> Notifications, IReadOnlyList<DashboardReminderItem> Plans)> LoadProjectItemsSafeAsync(DateTime now)
	{
		try
		{
			return await LoadProjectItemsAsync(now).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("DashboardReminderService: project reminders failed.", ex);
			return (Notifications: Array.Empty<DashboardReminderItem>(), Plans: Array.Empty<DashboardReminderItem>());
		}
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadHearingPlansSafeAsync()
	{
		try
		{
			return await LoadHearingPlansAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("DashboardReminderService: hearing plans failed.", ex);
			return Array.Empty<DashboardReminderItem>();
		}
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadAnnouncementAlertsAsync()
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT announcement_id,\n                         COALESCE(title, '') AS title,\n                         COALESCE(body, '') AS body,\n                         COALESCE(priority, 'Normal') AS priority,\n                         COALESCE(is_pinned, 0) AS is_pinned,\n                         created_at\n                  FROM announcements\n                  WHERE UPPER(COALESCE(status, 'Published')) = 'PUBLISHED'\n                    AND (COALESCE(is_pinned, 0) = 1 OR UPPER(COALESCE(priority, 'Normal')) = 'HIGH')\n                  ORDER BY COALESCE(is_pinned, 0) DESC,\n                           created_at DESC,\n                           announcement_id DESC\n                  LIMIT 6").ConfigureAwait(continueOnCapturedContext: false);
		List<DashboardReminderItem> list = new List<DashboardReminderItem>();
		foreach (DataRow row in obj.Rows)
		{
			string text = ReadString(row, "title");
			if (!string.IsNullOrWhiteSpace(text))
			{
				bool flag = ReadInt(row, "is_pinned") != 0;
				DateTime? sortDate = ReadDateTime(row, "created_at");
				list.Add(new DashboardReminderItem(flag ? "Pinned announcement" : "High priority", text, BuildAnnouncementDescription(ReadString(row, "body"), flag), sortDate.HasValue ? $"Published {sortDate.Value:MMM dd, yyyy}" : "Published announcement", "GovernanceRegistry", "Open registry", DashboardReminderSeverity.Urgent, 0, sortDate));
			}
		}
		return list;
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadCertificateAlertsAsync(DateTime now)
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT dr.doc_request_id,\n                         COALESCE(dt.name, 'Document request') AS document_name,\n                         COALESCE(\n                             NULLIF(TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name)), ''),\n                             CONCAT('Resident #', dr.resident_id)\n                         ) AS resident_name,\n                         UPPER(COALESCE(dr.status, 'SUBMITTED')) AS status,\n                         dr.requested_at,\n                         dr.approved_at\n                  FROM document_request dr\n                  LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n                  LEFT JOIN resident r ON r.resident_id = dr.resident_id\n                  WHERE UPPER(COALESCE(dr.status, '')) IN ('SUBMITTED', 'APPROVED')\n                  ORDER BY COALESCE(dr.approved_at, dr.requested_at) ASC,\n                           dr.doc_request_id ASC\n                  LIMIT 24").ConfigureAwait(continueOnCapturedContext: false);
		List<DashboardReminderItem> list = new List<DashboardReminderItem>();
		foreach (DataRow row in obj.Rows)
		{
			string text = ReadString(row, "status");
			DateTime? dateTime = ReadDateTime(row, "requested_at");
			DateTime? dateTime2 = ReadDateTime(row, "approved_at");
			SlaEvaluation evaluation = SlaRules.EvaluateCertificate(text, dateTime, dateTime2, now);
			SlaState state = evaluation.State;
			if ((uint)(state - 2) <= 1u)
			{
				string text2 = ReadString(row, "resident_name");
				string text3 = ReadString(row, "document_name");
				DateTime? dateTime3 = dateTime2 ?? dateTime;
				list.Add(new DashboardReminderItem((evaluation.State == SlaState.Overdue) ? "Overdue request" : "Due soon", text3 + " for " + text2, SlaRules.FormatDetailText(evaluation), "Status " + text, "Clearances", "Open requests", (evaluation.State != SlaState.Overdue) ? DashboardReminderSeverity.Attention : DashboardReminderSeverity.Urgent, (evaluation.State != SlaState.Overdue) ? 1 : 0, evaluation.DueDate ?? dateTime3));
			}
		}
		return list;
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadBlotterAlertsAsync(DateTime now)
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT cr.case_id,\n                         COALESCE(\n                             NULLIF(TRIM(cr.case_no), ''),\n                             CONCAT('BLT-', DATE_FORMAT(COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()), '%Y'), '-', LPAD(cr.case_id, 5, '0'))\n                         ) AS case_no,\n                         COALESCE(NULLIF(TRIM(cr.incident_type), ''), 'General') AS incident_type,\n                         UPPER(COALESCE(cr.status, 'ONGOING')) AS status,\n                         COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()) AS opened_on\n                  FROM case_record cr\n                  WHERE UPPER(COALESCE(cr.status, '')) IN ('OPEN', 'ONGOING')\n                  ORDER BY COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()) ASC,\n                           cr.case_id ASC\n                  LIMIT 24").ConfigureAwait(continueOnCapturedContext: false);
		List<DashboardReminderItem> list = new List<DashboardReminderItem>();
		foreach (DataRow row in obj.Rows)
		{
			string text = ReadString(row, "status");
			DateTime? dateTime = ReadDateTime(row, "opened_on");
			SlaEvaluation evaluation = SlaRules.EvaluateBlotter(text, dateTime, now);
			SlaState state = evaluation.State;
			if ((uint)(state - 2) <= 1u)
			{
				list.Add(new DashboardReminderItem((evaluation.State == SlaState.Overdue) ? "Case overdue" : "Case due soon", "Case " + ReadString(row, "case_no"), SlaRules.FormatDetailText(evaluation), ReadString(row, "incident_type") + " | " + text, "ResidentCases", "Open cases", (evaluation.State != SlaState.Overdue) ? DashboardReminderSeverity.Attention : DashboardReminderSeverity.Urgent, (evaluation.State != SlaState.Overdue) ? 1 : 0, evaluation.DueDate ?? dateTime));
			}
		}
		return list;
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadOutboundQueueAlertsAsync()
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT UPPER(COALESCE(status, '')) AS status,\n                         COUNT(*) AS total\n                  FROM outbound_notification\n                  WHERE UPPER(COALESCE(status, '')) IN ('FAILED', 'PENDING', 'SKIPPED')\n                  GROUP BY UPPER(COALESCE(status, ''))").ConfigureAwait(continueOnCapturedContext: false);
		List<DashboardReminderItem> list = new List<DashboardReminderItem>();
		foreach (DataRow row in obj.Rows)
		{
			string a = ReadString(row, "status");
			int num = ReadInt(row, "total");
			if (num > 0)
			{
				if (string.Equals(a, "FAILED", StringComparison.OrdinalIgnoreCase))
				{
					list.Add(new DashboardReminderItem("Dispatch issue", $"{num:N0} failed notification(s)", "The outbound queue has failed items that may need retry or correction.", "Review the outbox before the next dispatch cycle.", "NotificationOutbox", "Open outbox", DashboardReminderSeverity.Urgent, 0, null));
				}
				else if (string.Equals(a, "SKIPPED", StringComparison.OrdinalIgnoreCase))
				{
					list.Add(new DashboardReminderItem("Skipped notifications", $"{num:N0} skipped notification(s)", "Some queued messages were skipped because a required notification channel was unavailable.", "Confirm SMS or email configuration.", "NotificationOutbox", "Open outbox", DashboardReminderSeverity.Attention, 2, null));
				}
				else if (string.Equals(a, "PENDING", StringComparison.OrdinalIgnoreCase) && num >= 5)
				{
					list.Add(new DashboardReminderItem("Queued notifications", $"{num:N0} pending notification(s)", "The outbound queue is building up and should be checked before it turns into missed reminders.", "Pending items are waiting for the next dispatch cycle.", "NotificationOutbox", "Open outbox", DashboardReminderSeverity.Attention, 3, null));
				}
			}
		}
		return list;
	}

	private static async Task<(IReadOnlyList<DashboardReminderItem> Notifications, IReadOnlyList<DashboardReminderItem> Plans)> LoadProjectItemsAsync(DateTime now)
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT project_id,\n                         COALESCE(record_type, 'Project') AS record_type,\n                         COALESCE(name, 'Untitled initiative') AS name,\n                         COALESCE(status, 'Planned') AS status,\n                         start_date,\n                         end_date,\n                         COALESCE(outcome_status, 'Pending') AS outcome_status,\n                         created_at\n                  FROM projects\n                  WHERE UPPER(COALESCE(status, '')) IN ('PLANNED', 'ONGOING', 'ON HOLD')\n                     OR UPPER(COALESCE(outcome_status, '')) = 'NEEDS FOLLOW-UP'\n                  ORDER BY COALESCE(start_date, end_date, created_at) ASC,\n                           project_id ASC\n                  LIMIT 18").ConfigureAwait(continueOnCapturedContext: false);
		List<DashboardReminderItem> list = new List<DashboardReminderItem>();
		List<DashboardReminderItem> list2 = new List<DashboardReminderItem>();
		foreach (DataRow row in obj.Rows)
		{
			string text = ReadString(row, "record_type");
			string text2 = ReadString(row, "name");
			string text3 = ReadString(row, "status");
			string a = ReadString(row, "outcome_status");
			DateTime? dateTime = ReadDateTime(row, "start_date");
			DateTime? dateTime2 = ReadDateTime(row, "end_date");
			DateTime? dateTime3 = ReadDateTime(row, "created_at");
			if (string.Equals(a, "Needs follow-up", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(new DashboardReminderItem("Needs follow-up", text + ": " + text2, "The recorded outcome still needs follow-up action from the barangay team.", string.IsNullOrWhiteSpace(text3) ? text : (text + " | " + text3), "GovernanceRegistry", "Open plans", DashboardReminderSeverity.Attention, 2, dateTime2 ?? dateTime ?? dateTime3));
			}
			DashboardReminderItem dashboardReminderItem = BuildProjectPlanItem(text, text2, text3, dateTime, dateTime2, dateTime3, now);
			if (dashboardReminderItem != null)
			{
				list2.Add(dashboardReminderItem);
			}
		}
		return (Notifications: list, Plans: list2);
	}

	private static async Task<IReadOnlyList<DashboardReminderItem>> LoadHearingPlansAsync()
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT ch.case_id,\n                         ch.schedule_at,\n                         COALESCE(NULLIF(TRIM(ch.venue), ''), 'Barangay hall') AS venue,\n                         COALESCE(\n                             NULLIF(TRIM(cr.case_no), ''),\n                             CONCAT('BLT-', DATE_FORMAT(COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()), '%Y'), '-', LPAD(cr.case_id, 5, '0'))\n                         ) AS case_no\n                  FROM case_hearing ch\n                  INNER JOIN case_record cr ON cr.case_id = ch.case_id\n                  WHERE ch.schedule_at >= NOW()\n                    AND UPPER(COALESCE(ch.status, 'SCHEDULED')) = 'SCHEDULED'\n                  ORDER BY ch.schedule_at ASC,\n                           ch.hearing_id ASC\n                  LIMIT 6").ConfigureAwait(continueOnCapturedContext: false);
		List<DashboardReminderItem> list = new List<DashboardReminderItem>();
		foreach (DataRow row in obj.Rows)
		{
			DateTime? sortDate = ReadDateTime(row, "schedule_at");
			if (sortDate.HasValue)
			{
				list.Add(new DashboardReminderItem("Upcoming mediation", "Case " + ReadString(row, "case_no"), $"{sortDate.Value:MMM dd, yyyy hh:mm tt}", ReadString(row, "venue"), "ResidentCases", "Open cases", DashboardReminderSeverity.Plan, 0, sortDate));
			}
		}
		return list;
	}

	private static DashboardReminderItem? BuildProjectPlanItem(string recordType, string name, string status, DateTime? startDate, DateTime? endDate, DateTime? createdAt, DateTime now)
	{
		string text = status.Trim().ToUpperInvariant();
		if (text == "PLANNED" && startDate.HasValue)
		{
			if (startDate.Value.Date < now.Date.AddDays(-3.0) || startDate.Value.Date > now.Date.AddDays(30.0))
			{
				return null;
			}
			return new DashboardReminderItem("Planned initiative", recordType + ": " + name, (startDate.Value.Date < now.Date) ? $"Planned start was {startDate.Value:MMM dd, yyyy}" : $"Starts {startDate.Value:MMM dd, yyyy}", recordType + " | " + status, "GovernanceRegistry", "Open plans", DashboardReminderSeverity.Plan, (!(startDate.Value.Date < now.Date)) ? 1 : 0, startDate);
		}
		if ((text == "ONGOING" || text == "ON HOLD") ? true : false)
		{
			if (!endDate.HasValue)
			{
				return null;
			}
			if (endDate.Value.Date > now.Date.AddDays(21.0))
			{
				return null;
			}
			return new DashboardReminderItem((endDate.Value.Date < now.Date) ? "Past target date" : "Upcoming target date", recordType + ": " + name, (endDate.Value.Date < now.Date) ? $"Target end passed on {endDate.Value:MMM dd, yyyy}" : $"Target end {endDate.Value:MMM dd, yyyy}", recordType + " | " + status, "GovernanceRegistry", "Open plans", (endDate.Value.Date < now.Date) ? DashboardReminderSeverity.Attention : DashboardReminderSeverity.Plan, (!(endDate.Value.Date < now.Date)) ? 1 : 0, endDate);
		}
		if (!startDate.HasValue && !endDate.HasValue && createdAt.HasValue && createdAt.Value.Date >= now.Date.AddDays(-7.0))
		{
			return new DashboardReminderItem("Newly logged plan", recordType + ": " + name, "Recently added to the initiatives registry.", recordType + " | " + status, "GovernanceRegistry", "Open plans", DashboardReminderSeverity.Plan, 2, createdAt);
		}
		return null;
	}

	private static string BuildAnnouncementDescription(string body, bool isPinned)
	{
		string text = TrimToLength(body, 140);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (!isPinned)
		{
			return "High-priority announcement for immediate barangay attention.";
		}
		return "Pinned announcement for immediate barangay attention.";
	}

	private static string TrimToLength(string value, int maxLength)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length <= maxLength)
		{
			return text;
		}
		return text.Substring(0, Math.Max(0, maxLength - 3)).TrimEnd() + "...";
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
		if (DateTime.TryParse(Convert.ToString(row[columnName], CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			return result;
		}
		return null;
	}
}
