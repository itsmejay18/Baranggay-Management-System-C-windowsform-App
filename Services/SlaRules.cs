using System;

namespace baranggaysystem1;

internal enum SlaState
{
    NotApplicable,
    OnTrack,
    DueSoon,
    Overdue
}

internal readonly record struct SlaEvaluation(
    SlaState State,
    string Stage,
    DateTime? DueDate,
    int? DaysRemaining,
    int? DaysOverdue)
{
    internal bool Applies => State != SlaState.NotApplicable;
}

internal static class SlaRules
{
    // Keep these defaults in one place so it is easy to tune later.
    // All calculations are date-based (not time-of-day) to keep the UI predictable.
    internal const int CertificateApprovalSlaDays = 2; // SUBMITTED -> APPROVED
    internal const int CertificateReleaseSlaDays = 1; // APPROVED -> RELEASED
    internal const int BlotterResolutionSlaDays = 15; // OPEN/ONGOING -> SETTLED/REFERRED/CLOSED

    internal const int CertificateDueSoonDays = 1;
    internal const int BlotterDueSoonDays = 3;

    internal static SlaEvaluation EvaluateCertificate(string? rawStatus, DateTime? requestedAt, DateTime? approvedAt, DateTime now)
    {
        string status = NormalizeStatus(rawStatus);

        if (status is "SUBMITTED" or "REQUESTED")
        {
            return EvaluateFromStart("Approval", requestedAt, CertificateApprovalSlaDays, CertificateDueSoonDays, now);
        }

        if (status is "APPROVED")
        {
            return EvaluateFromStart("Release", approvedAt, CertificateReleaseSlaDays, CertificateDueSoonDays, now);
        }

        return new SlaEvaluation(SlaState.NotApplicable, string.Empty, null, null, null);
    }

    internal static SlaEvaluation EvaluateBlotter(string? rawStatus, DateTime? createdAt, DateTime now)
    {
        string status = NormalizeStatus(rawStatus);

        if (status is "OPEN" or "ONGOING")
        {
            return EvaluateFromStart("Resolution", createdAt, BlotterResolutionSlaDays, BlotterDueSoonDays, now);
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
            int days = evaluation.DaysOverdue.GetValueOrDefault(0);
            return days <= 0 ? "Overdue" : $"Overdue {days}d";
        }

        int remaining = evaluation.DaysRemaining.GetValueOrDefault(0);
        if (remaining <= 0)
        {
            return "Due today";
        }

        return $"Due {remaining}d";
    }

    internal static string FormatDetailText(SlaEvaluation evaluation)
    {
        if (!evaluation.Applies)
        {
            return "-";
        }

        string due = evaluation.DueDate.HasValue ? evaluation.DueDate.Value.ToString("MMM dd, yyyy") : "-";
        if (evaluation.State == SlaState.Overdue)
        {
            int days = evaluation.DaysOverdue.GetValueOrDefault(0);
            return $"{evaluation.Stage} overdue since {due} ({days}d)";
        }

        int remaining = evaluation.DaysRemaining.GetValueOrDefault(0);
        if (remaining <= 0)
        {
            return $"{evaluation.Stage} due today ({due})";
        }

        return $"{evaluation.Stage} due {due} ({remaining}d left)";
    }

    private static SlaEvaluation EvaluateFromStart(string stage, DateTime? startAt, int slaDays, int dueSoonDays, DateTime now)
    {
        if (!startAt.HasValue || startAt.Value == DateTime.MinValue)
        {
            return new SlaEvaluation(SlaState.NotApplicable, string.Empty, null, null, null);
        }

        DateTime dueDate = startAt.Value.Date.AddDays(slaDays);
        int daysRemaining = (dueDate.Date - now.Date).Days;

        if (daysRemaining < 0)
        {
            return new SlaEvaluation(SlaState.Overdue, stage, dueDate, null, -daysRemaining);
        }

        if (daysRemaining <= dueSoonDays)
        {
            return new SlaEvaluation(SlaState.DueSoon, stage, dueDate, daysRemaining, null);
        }

        return new SlaEvaluation(SlaState.OnTrack, stage, dueDate, daysRemaining, null);
    }

    private static string NormalizeStatus(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}

