using System.Collections.Generic;

namespace baranggaysystem1;

public enum CertificateStatusFilter
{
    AllNonDraft = 0,
    Pending = 1,
    Submitted = 2,
    Approved = 3,
    Released = 4,
    Cancelled = 5,
    Rejected = 6
}

public enum BlotterStatusFilter
{
    All = 0,
    Active = 1,
    Settled = 2,
    Referred = 3,
    Closed = 4
}

public sealed class ReportsFilters
{
    public int? PurokId { get; set; }
    public CertificateStatusFilter CertificateStatus { get; set; } = CertificateStatusFilter.AllNonDraft;
    public BlotterStatusFilter BlotterStatus { get; set; } = BlotterStatusFilter.All;
}

public sealed class ReportsDashboardData
{
    public ReportsSummary Summary { get; set; } = new();
    public ServiceTimeMetrics ServiceTimes { get; set; } = new();
    public IReadOnlyList<MonthlyTrendRow> Trends { get; set; } = [];
    public IReadOnlyList<StaffPerformanceRow> StaffPerformance { get; set; } = [];
    public IReadOnlyList<HotspotPoint> Hotspots { get; set; } = [];
}

public sealed class ReportsSummary
{
    public int NewResidents { get; set; }
    public int CertificateRequests { get; set; }
    public int CertificatesReleased { get; set; }
    public int BlottersFiled { get; set; }
    public int PendingCertificates { get; set; }
    public int ActiveBlotters { get; set; }
}

public sealed class ServiceTimeMetrics
{
    public double AvgRequestToApprovalSeconds { get; set; }
    public int ApprovalSamples { get; set; }
    public double AvgApprovalToReleaseSeconds { get; set; }
    public int ReleaseSamples { get; set; }
}

public sealed class MonthlyTrendRow
{
    public string MonthLabel { get; set; } = string.Empty;
    public int Residents { get; set; }
    public int Certificates { get; set; }
    public int Blotters { get; set; }
}

public sealed class StaffPerformanceRow
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ApprovalsCompleted { get; set; }
    public int ApprovalsOverdue { get; set; }
    public double AvgRequestToApprovalSeconds { get; set; }
    public int ReleasesCompleted { get; set; }
    public int ReleasesOverdue { get; set; }
    public double AvgApprovalToReleaseSeconds { get; set; }
    public int BlotterStatusChanges { get; set; }
    public int BlotterResolutions { get; set; }
    public int BlotterResolutionsOverdue { get; set; }
    public double AvgBlotterResolutionSeconds { get; set; }
}

public sealed class HotspotPoint
{
    public int PurokId { get; set; }
    public string PurokName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int IncidentCount { get; set; }
}
