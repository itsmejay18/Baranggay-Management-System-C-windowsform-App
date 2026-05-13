using System;
using System.Collections.Generic;

namespace baranggaysystem1;

internal sealed class ReportsDashboardData
{
	public IReadOnlyList<MonthlyTrendRow> Trends { get; set; } = Array.Empty<MonthlyTrendRow>();

	public ReportsSummary Summary { get; set; } = new ReportsSummary();

	public ServiceTimeMetrics ServiceTimes { get; set; } = new ServiceTimeMetrics();

	public IReadOnlyList<StaffPerformanceRow> StaffPerformance { get; set; } = Array.Empty<StaffPerformanceRow>();

	public IReadOnlyList<HotspotPoint> Hotspots { get; set; } = Array.Empty<HotspotPoint>();
}
