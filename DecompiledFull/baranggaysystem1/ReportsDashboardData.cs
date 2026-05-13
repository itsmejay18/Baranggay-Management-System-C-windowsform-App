using System;
using System.Collections.Generic;

namespace baranggaysystem1;

internal sealed class ReportsDashboardData
{
	public IReadOnlyList<MonthlyTrendRow> Trends { get; init; } = Array.Empty<MonthlyTrendRow>();

	public ReportsSummary Summary { get; init; } = new ReportsSummary();

	public ServiceTimeMetrics ServiceTimes { get; init; } = new ServiceTimeMetrics();

	public IReadOnlyList<StaffPerformanceRow> StaffPerformance { get; init; } = Array.Empty<StaffPerformanceRow>();

	public IReadOnlyList<HotspotPoint> Hotspots { get; init; } = Array.Empty<HotspotPoint>();
}
