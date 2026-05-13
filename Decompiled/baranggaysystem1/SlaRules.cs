using System;

namespace baranggaysystem1;

internal static class SlaRules
{
	internal const int CertificateApprovalSlaDays = 2;

	internal const int CertificateReleaseSlaDays = 1;

	internal const int BlotterResolutionSlaDays = 15;

	internal const int CertificateDueSoonDays = 1;

	internal const int BlotterDueSoonDays = 3;

	internal static SlaEvaluation EvaluateCertificate(string? rawStatus, DateTime? requestedAt, DateTime? approvedAt, DateTime now)
	{
		string text = NormalizeStatus(rawStatus);
		if ((text == "SUBMITTED" || text == "REQUESTED") ? true : false)
		{
			return EvaluateFromStart("Approval", requestedAt, 2, 1, now);
		}
		if (text == "APPROVED")
		{
			return EvaluateFromStart("Release", approvedAt, 1, 1, now);
		}
		return new SlaEvaluation(SlaState.NotApplicable, string.Empty, null, null, null);
	}

	internal static SlaEvaluation EvaluateBlotter(string? rawStatus, DateTime? createdAt, DateTime now)
	{
		string text = NormalizeStatus(rawStatus);
		if ((text == "OPEN" || text == "ONGOING") ? true : false)
		{
			return EvaluateFromStart("Resolution", createdAt, 15, 3, now);
		}
		return new SlaEvaluation(SlaState.NotApplicable, string.Empty, null, null, null);
	}

	internal static string FormatShortLabel(SlaEvaluation evaluation)
	{
		if (!evaluation.Applies)
		{
			return string.Empty;
		}
		if (evaluation.State == SlaState.Overdue)
		{
			int num = evaluation.DaysOverdue ?? 0;
			if (num > 0)
			{
				return $"Overdue {num}d";
			}
			return "Overdue";
		}
		int num2 = evaluation.DaysRemaining ?? 0;
		if (num2 <= 0)
		{
			return "Due today";
		}
		return $"Due {num2}d";
	}

	internal static string FormatDetailText(SlaEvaluation evaluation)
	{
		if (!evaluation.Applies)
		{
			return "-";
		}
		string text = (evaluation.DueDate.HasValue ? evaluation.DueDate.Value.ToString("MMM dd, yyyy") : "-");
		if (evaluation.State == SlaState.Overdue)
		{
			int value = evaluation.DaysOverdue ?? 0;
			return $"{evaluation.Stage} overdue since {text} ({value}d)";
		}
		int num = evaluation.DaysRemaining ?? 0;
		if (num <= 0)
		{
			return evaluation.Stage + " due today (" + text + ")";
		}
		return $"{evaluation.Stage} due {text} ({num}d left)";
	}

	private static SlaEvaluation EvaluateFromStart(string stage, DateTime? startAt, int slaDays, int dueSoonDays, DateTime now)
	{
		if (!startAt.HasValue || startAt.Value == DateTime.MinValue)
		{
			return new SlaEvaluation(SlaState.NotApplicable, string.Empty, null, null, null);
		}
		DateTime value = startAt.Value.Date.AddDays(slaDays);
		int days = (value.Date - now.Date).Days;
		if (days < 0)
		{
			return new SlaEvaluation(SlaState.Overdue, stage, value, null, -days);
		}
		if (days <= dueSoonDays)
		{
			return new SlaEvaluation(SlaState.DueSoon, stage, value, days, null);
		}
		return new SlaEvaluation(SlaState.OnTrack, stage, value, days, null);
	}

	private static string NormalizeStatus(string? value)
	{
		return (value ?? string.Empty).Trim().ToUpperInvariant();
	}
}
