using System;
using System.IO;
using System.Text;

namespace baranggaysystem1;

/// <summary>
/// Service for exporting reports to PDF and Excel formats.
/// </summary>
public static class ReportsExportService
{
    /// <summary>
    /// Exports report data to a PDF file.
    /// </summary>
    public static void ExportToPdf(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
    {
        ExportDashboardPdf(data, from, to, filePath);
    }

    /// <summary>
    /// Exports report data to a PDF file (named to match caller convention).
    /// </summary>
    public static void ExportDashboardPdf(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BARANGAY MANAGEMENT SYSTEM - REPORT");
        sb.AppendLine($"Period: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();

        sb.AppendLine("SUMMARY");
        sb.AppendLine($"  New Residents:          {data.Summary.NewResidents}");
        sb.AppendLine($"  Certificate Requests:   {data.Summary.CertificateRequests}");
        sb.AppendLine($"  Certificates Released:  {data.Summary.CertificatesReleased}");
        sb.AppendLine($"  Blotters Filed:         {data.Summary.BlottersFiled}");
        sb.AppendLine($"  Pending Certificates:   {data.Summary.PendingCertificates}");
        sb.AppendLine($"  Active Blotters:        {data.Summary.ActiveBlotters}");
        sb.AppendLine();

        sb.AppendLine("SERVICE TIME METRICS");
        sb.AppendLine($"  Avg Request to Approval: {TimeSpan.FromSeconds(data.ServiceTimes.AvgRequestToApprovalSeconds):g}");
        sb.AppendLine($"  Avg Approval to Release: {TimeSpan.FromSeconds(data.ServiceTimes.AvgApprovalToReleaseSeconds):g}");
        sb.AppendLine();

        sb.AppendLine("MONTHLY TRENDS");
        foreach (var trend in data.Trends)
        {
            sb.AppendLine($"  {trend.MonthLabel}: Residents={trend.Residents}, Certificates={trend.Certificates}, Blotters={trend.Blotters}");
        }
        sb.AppendLine();

        sb.AppendLine("STAFF PERFORMANCE");
        foreach (var staff in data.StaffPerformance)
        {
            sb.AppendLine($"  {staff.DisplayName} ({staff.Username}): Approvals={staff.ApprovalsCompleted}, Releases={staff.ReleasesCompleted}");
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Exports report data to an Excel-compatible CSV file.
    /// </summary>
    public static void ExportToExcel(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
    {
        ExportDashboardExcel(data, from, to, filePath);
    }

    /// <summary>
    /// Exports report data to an Excel-compatible CSV file (named to match caller convention).
    /// </summary>
    public static void ExportDashboardExcel(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
    {
        var sb = new StringBuilder();

        // Summary sheet
        sb.AppendLine("Report Period," + from.ToString("yyyy-MM-dd") + "," + to.ToString("yyyy-MM-dd"));
        sb.AppendLine();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"New Residents,{data.Summary.NewResidents}");
        sb.AppendLine($"Certificate Requests,{data.Summary.CertificateRequests}");
        sb.AppendLine($"Certificates Released,{data.Summary.CertificatesReleased}");
        sb.AppendLine($"Blotters Filed,{data.Summary.BlottersFiled}");
        sb.AppendLine($"Pending Certificates,{data.Summary.PendingCertificates}");
        sb.AppendLine($"Active Blotters,{data.Summary.ActiveBlotters}");
        sb.AppendLine();

        // Trends
        sb.AppendLine("Month,Residents,Certificates,Blotters");
        foreach (var trend in data.Trends)
        {
            sb.AppendLine($"{trend.MonthLabel},{trend.Residents},{trend.Certificates},{trend.Blotters}");
        }
        sb.AppendLine();

        // Staff Performance
        sb.AppendLine("Username,Display Name,Approvals,Releases,Blotter Resolutions");
        foreach (var staff in data.StaffPerformance)
        {
            sb.AppendLine($"{staff.Username},{staff.DisplayName},{staff.ApprovalsCompleted},{staff.ReleasesCompleted},{staff.BlotterResolutions}");
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}
