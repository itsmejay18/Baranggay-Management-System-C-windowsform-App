using System;

namespace baranggaysystem1;

/// <summary>
/// Service Level Agreement rules for certificates and blotter cases.
/// </summary>
public static class SlaRules
{
    /// <summary>Number of days allowed for certificate approval.</summary>
    public const int CertificateApprovalSlaDays = 3;

    /// <summary>Number of days allowed for certificate release after approval.</summary>
    public const int CertificateReleaseSlaDays = 3;

    /// <summary>Number of days before SLA breach to show "due soon" warning.</summary>
    public const int CertificateDueSoonDays = 1;

    /// <summary>Number of days allowed for blotter case resolution.</summary>
    public const int BlotterResolutionSlaDays = 15;

    /// <summary>Number of days before blotter SLA breach to show "due soon" warning.</summary>
    public const int BlotterDueSoonDays = 3;

    /// <summary>
    /// Evaluates the SLA status for a certificate request.
    /// </summary>
    public static SlaEvaluation EvaluateCertificate(string rawStatus, DateTime? requestedAt, DateTime? approvedAt, DateTime now)
    {
        string status = (rawStatus ?? string.Empty).Trim().ToUpperInvariant();

        if (status == "SUBMITTED" && requestedAt.HasValue)
        {
            double daysElapsed = (now - requestedAt.Value).TotalDays;
            if (daysElapsed > CertificateApprovalSlaDays)
            {
                return new SlaEvaluation(true, SlaState.Overdue, (int)Math.Ceiling(daysElapsed - CertificateApprovalSlaDays));
            }
            if (daysElapsed >= CertificateApprovalSlaDays - CertificateDueSoonDays)
            {
                return new SlaEvaluation(true, SlaState.DueSoon, (int)Math.Ceiling(CertificateApprovalSlaDays - daysElapsed));
            }
            return new SlaEvaluation(true, SlaState.OnTrack, (int)Math.Ceiling(CertificateApprovalSlaDays - daysElapsed));
        }

        if (status == "APPROVED" && approvedAt.HasValue)
        {
            double daysElapsed = (now - approvedAt.Value).TotalDays;
            if (daysElapsed > CertificateReleaseSlaDays)
            {
                return new SlaEvaluation(true, SlaState.Overdue, (int)Math.Ceiling(daysElapsed - CertificateReleaseSlaDays));
            }
            if (daysElapsed >= CertificateReleaseSlaDays - CertificateDueSoonDays)
            {
                return new SlaEvaluation(true, SlaState.DueSoon, (int)Math.Ceiling(CertificateReleaseSlaDays - daysElapsed));
            }
            return new SlaEvaluation(true, SlaState.OnTrack, (int)Math.Ceiling(CertificateReleaseSlaDays - daysElapsed));
        }

        return new SlaEvaluation(false, SlaState.NotApplicable, 0);
    }

    /// <summary>
    /// Evaluates the SLA status for a blotter case.
    /// </summary>
    public static SlaEvaluation EvaluateBlotter(string rawStatus, DateTime? createdAt, DateTime now)
    {
        string status = (rawStatus ?? string.Empty).Trim().ToUpperInvariant();

        if ((status == "OPEN" || status == "ONGOING") && createdAt.HasValue)
        {
            double daysElapsed = (now - createdAt.Value).TotalDays;
            if (daysElapsed > BlotterResolutionSlaDays)
            {
                return new SlaEvaluation(true, SlaState.Overdue, (int)Math.Ceiling(daysElapsed - BlotterResolutionSlaDays));
            }
            if (daysElapsed >= BlotterResolutionSlaDays - BlotterDueSoonDays)
            {
                return new SlaEvaluation(true, SlaState.DueSoon, (int)Math.Ceiling(BlotterResolutionSlaDays - daysElapsed));
            }
            return new SlaEvaluation(true, SlaState.OnTrack, (int)Math.Ceiling(BlotterResolutionSlaDays - daysElapsed));
        }

        return new SlaEvaluation(false, SlaState.NotApplicable, 0);
    }

    /// <summary>
    /// Formats a short label for display in SLA badges.
    /// </summary>
    public static string FormatShortLabel(SlaEvaluation evaluation)
    {
        return evaluation.State switch
        {
            SlaState.Overdue => $"Overdue ({evaluation.DaysCount}d)",
            SlaState.DueSoon => $"Due soon ({evaluation.DaysCount}d left)",
            SlaState.OnTrack => $"{evaluation.DaysCount}d left",
            _ => string.Empty
        };
    }
}

public enum SlaState
{
    NotApplicable,
    OnTrack,
    DueSoon,
    Overdue
}

public sealed class SlaEvaluation
{
    public bool Applies { get; }
    public SlaState State { get; }
    public int DaysCount { get; }

    public SlaEvaluation(bool applies, SlaState state, int daysCount)
    {
        Applies = applies;
        State = state;
        DaysCount = daysCount;
    }
}
