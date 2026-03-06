using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace baranggaysystem1;

internal static class ReportsExportService
{
    public static void ExportDashboardExcel(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        EnsureDirectory(filePath);

        using var wb = new XLWorkbook();

        WriteSummarySheet(wb, data, from, to);
        WriteTrendsSheet(wb, data);
        WriteStaffSheet(wb, data);

        wb.SaveAs(filePath);
    }

    public static void ExportDashboardPdf(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        EnsureDirectory(filePath);

        // Required by QuestPDF license model.
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Barangay System Reports").FontSize(18).SemiBold();
                            col.Item().Text($"Range: {from:MMM dd, yyyy} - {to:MMM dd, yyyy}").FontSize(10).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(160).AlignRight().Column(col =>
                        {
                            col.Item().Text($"Generated: {DateTime.Now:MMM dd, yyyy hh:mm tt}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });

                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Element(x => BuildSummaryCards(x, data));
                    col.Item().Element(x => BuildServiceTimes(x, data));
                    col.Item().Element(x => BuildTrendsTable(x, data.Trends));

                    // Staff table can become long; keep it after the core summary.
                    col.Item().Element(x => BuildStaffTable(x, data.StaffPerformance));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Barangay System").FontSize(9).FontColor(Colors.Grey.Darken2);
                    text.Span("  |  ").FontSize(9).FontColor(Colors.Grey.Darken2);
                    text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Darken2);
                    text.Span(" / ").FontSize(9).FontColor(Colors.Grey.Darken2);
                    text.TotalPages().FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf(filePath);
    }

    private static void EnsureDirectory(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static void WriteSummarySheet(IXLWorkbook wb, ReportsDashboardData data, DateTime from, DateTime to)
    {
        var ws = wb.AddWorksheet("Summary");

        ws.Cell(1, 1).Value = "Barangay System Reports";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        ws.Cell(2, 1).Value = "Date range:";
        ws.Cell(2, 2).Value = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}";

        ws.Cell(4, 1).Value = "Summary";
        ws.Cell(4, 1).Style.Font.Bold = true;

        int r = 5;
        WriteKeyValue(ws, r++, "New residents", data.Summary.NewResidents);
        WriteKeyValue(ws, r++, "Certificate requests", data.Summary.CertificateRequests);
        WriteKeyValue(ws, r++, "Certificates released", data.Summary.CertificatesReleased);
        WriteKeyValue(ws, r++, "Blotter cases filed", data.Summary.BlottersFiled);
        WriteKeyValue(ws, r++, "Total residents", data.Summary.TotalResidents);
        WriteKeyValue(ws, r++, "Pending certificates", data.Summary.PendingCertificates);
        WriteKeyValue(ws, r++, "Active blotter cases", data.Summary.ActiveBlotters);

        r++;
        ws.Cell(r, 1).Value = "Service times";
        ws.Cell(r, 1).Style.Font.Bold = true;
        r++;
        ws.Cell(r, 1).Value = "Avg request -> approval";
        ws.Cell(r, 2).Value = FormatDuration(data.ServiceTimes.AvgRequestToApprovalSeconds);
        ws.Cell(r, 3).Value = $"Samples: {data.ServiceTimes.ApprovalSamples}";
        r++;
        ws.Cell(r, 1).Value = "Avg approval -> release";
        ws.Cell(r, 2).Value = FormatDuration(data.ServiceTimes.AvgApprovalToReleaseSeconds);
        ws.Cell(r, 3).Value = $"Samples: {data.ServiceTimes.ReleaseSamples}";

        ws.Columns().AdjustToContents();
    }

    private static void WriteTrendsSheet(IXLWorkbook wb, ReportsDashboardData data)
    {
        var ws = wb.AddWorksheet("Monthly Trends");

        ws.Cell(1, 1).Value = "Month";
        ws.Cell(1, 2).Value = "Residents";
        ws.Cell(1, 3).Value = "Certificates";
        ws.Cell(1, 4).Value = "Blotter";
        ws.Range(1, 1, 1, 4).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

        int row = 2;
        foreach (MonthlyTrendRow tr in data.Trends ?? Array.Empty<MonthlyTrendRow>())
        {
            ws.Cell(row, 1).Value = tr.MonthLabel;
            ws.Cell(row, 2).Value = tr.Residents;
            ws.Cell(row, 3).Value = tr.Certificates;
            ws.Cell(row, 4).Value = tr.Blotters;
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static void WriteStaffSheet(IXLWorkbook wb, ReportsDashboardData data)
    {
        var ws = wb.AddWorksheet("Staff Performance");

        string[] headers =
        {
            "User",
            "Completed",
            "Overdue",
            "Cert Approvals",
            "Approval Overdue",
            "Avg Req->Approve",
            "Cert Releases",
            "Release Overdue",
            "Avg Approve->Release",
            "Blotter Updates",
            "Resolutions",
            "Resolution Overdue",
            "Avg Resolution"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

        var rows = (data.StaffPerformance ?? Array.Empty<StaffPerformanceRow>())
            .Where(r => r.IsActive || HasAnyActivity(r))
            .Select(r =>
            {
                int completed = r.ApprovalsCompleted + r.ReleasesCompleted + r.BlotterResolutions;
                int overdue = r.ApprovalsOverdue + r.ReleasesOverdue + r.BlotterResolutionsOverdue;
                return new
                {
                    User = FormatUser(r),
                    Completed = completed,
                    Overdue = overdue,
                    r.ApprovalsCompleted,
                    r.ApprovalsOverdue,
                    AvgApproval = FormatDuration(r.AvgRequestToApprovalSeconds),
                    r.ReleasesCompleted,
                    r.ReleasesOverdue,
                    AvgRelease = FormatDuration(r.AvgApprovalToReleaseSeconds),
                    r.BlotterStatusChanges,
                    r.BlotterResolutions,
                    r.BlotterResolutionsOverdue,
                    AvgResolution = FormatDuration(r.AvgBlotterResolutionSeconds)
                };
            })
            .OrderByDescending(r => r.Completed)
            .ThenBy(r => r.Overdue)
            .ThenBy(r => r.User, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.User;
            ws.Cell(row, 2).Value = r.Completed;
            ws.Cell(row, 3).Value = r.Overdue;
            ws.Cell(row, 4).Value = r.ApprovalsCompleted;
            ws.Cell(row, 5).Value = r.ApprovalsOverdue;
            ws.Cell(row, 6).Value = r.AvgApproval;
            ws.Cell(row, 7).Value = r.ReleasesCompleted;
            ws.Cell(row, 8).Value = r.ReleasesOverdue;
            ws.Cell(row, 9).Value = r.AvgRelease;
            ws.Cell(row, 10).Value = r.BlotterStatusChanges;
            ws.Cell(row, 11).Value = r.BlotterResolutions;
            ws.Cell(row, 12).Value = r.BlotterResolutionsOverdue;
            ws.Cell(row, 13).Value = r.AvgResolution;
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static void WriteKeyValue(IXLWorksheet ws, int row, string key, int value)
    {
        ws.Cell(row, 1).Value = key;
        ws.Cell(row, 2).Value = value;
    }

    private static bool HasAnyActivity(StaffPerformanceRow row)
        => row.ApprovalsCompleted > 0 ||
           row.ReleasesCompleted > 0 ||
           row.BlotterStatusChanges > 0 ||
           row.BlotterResolutions > 0;

    private static string FormatUser(StaffPerformanceRow row)
    {
        string name = string.IsNullOrWhiteSpace(row.DisplayName) ? row.Username : row.DisplayName;
        if (!string.IsNullOrWhiteSpace(row.Username) && !string.Equals(name, row.Username, StringComparison.OrdinalIgnoreCase))
        {
            name = $"{row.Username} ({name})";
        }
        if (!row.IsActive)
        {
            name += " [inactive]";
        }

        return name;
    }

    private static string FormatDuration(double seconds)
    {
        if (seconds <= 0)
        {
            return "-";
        }

        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalMinutes < 1)
        {
            return "<1m";
        }

        if (ts.TotalHours < 1)
        {
            return $"{ts.TotalMinutes:0}m";
        }

        if (ts.TotalDays < 1)
        {
            return $"{ts.TotalHours:0.#}h";
        }

        int days = (int)Math.Floor(ts.TotalDays);
        if (days < 10 && ts.Hours > 0)
        {
            return $"{days}d {ts.Hours}h";
        }

        return $"{ts.TotalDays:0.#}d";
    }

    private static void BuildSummaryCards(IContainer container, ReportsDashboardData data)
    {
        container.Row(row =>
        {
            row.Spacing(10);

            row.RelativeItem().Element(c => Card(c, "New residents", data.Summary.NewResidents.ToString("N0")));
            row.RelativeItem().Element(c => Card(c, "Cert requests", data.Summary.CertificateRequests.ToString("N0")));
            row.RelativeItem().Element(c => Card(c, "Cert released", data.Summary.CertificatesReleased.ToString("N0")));
            row.RelativeItem().Element(c => Card(c, "Blotter filed", data.Summary.BlottersFiled.ToString("N0")));
        });
    }

    private static void BuildServiceTimes(IContainer container, ReportsDashboardData data)
    {
        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Text("Service Time Metrics").SemiBold();

            col.Item().Text(
                $"Avg request -> approval: {FormatDuration(data.ServiceTimes.AvgRequestToApprovalSeconds)} (samples: {data.ServiceTimes.ApprovalSamples:N0})")
                .FontSize(10).FontColor(Colors.Grey.Darken2);

            col.Item().Text(
                $"Avg approval -> release: {FormatDuration(data.ServiceTimes.AvgApprovalToReleaseSeconds)} (samples: {data.ServiceTimes.ReleaseSamples:N0})")
                .FontSize(10).FontColor(Colors.Grey.Darken2);
        });
    }

    private static void BuildTrendsTable(IContainer container, IReadOnlyList<MonthlyTrendRow> trends)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text("Monthly Trends").SemiBold();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Month");
                    HeaderCell(header.Cell(), "Residents");
                    HeaderCell(header.Cell(), "Certificates");
                    HeaderCell(header.Cell(), "Blotter");
                });

                foreach (MonthlyTrendRow t in trends ?? Array.Empty<MonthlyTrendRow>())
                {
                    BodyCell(table.Cell(), t.MonthLabel);
                    BodyCell(table.Cell(), t.Residents.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), t.Certificates.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), t.Blotters.ToString("N0"), alignRight: true);
                }
            });
        });
    }

    private static void BuildStaffTable(IContainer container, IReadOnlyList<StaffPerformanceRow> staff)
    {
        var rows = (staff ?? Array.Empty<StaffPerformanceRow>())
            .Where(r => r.IsActive || HasAnyActivity(r))
            .Select(r =>
            {
                int completed = r.ApprovalsCompleted + r.ReleasesCompleted + r.BlotterResolutions;
                int overdue = r.ApprovalsOverdue + r.ReleasesOverdue + r.BlotterResolutionsOverdue;
                return new
                {
                    User = FormatUser(r),
                    Completed = completed,
                    Overdue = overdue,
                    Approvals = r.ApprovalsCompleted,
                    ApprovalOverdue = r.ApprovalsOverdue,
                    Releases = r.ReleasesCompleted,
                    ReleaseOverdue = r.ReleasesOverdue,
                    Resolutions = r.BlotterResolutions,
                    ResolutionOverdue = r.BlotterResolutionsOverdue
                };
            })
            .OrderByDescending(r => r.Completed)
            .ThenBy(r => r.Overdue)
            .ThenBy(r => r.User, StringComparer.OrdinalIgnoreCase)
            .ToList();

        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text("Staff Performance").SemiBold();

            if (rows.Count == 0)
            {
                col.Item().Text("No staff activity in the selected date range.").FontSize(10).FontColor(Colors.Grey.Darken2);
                return;
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "User");
                    HeaderCell(header.Cell(), "Done");
                    HeaderCell(header.Cell(), "Overdue");
                    HeaderCell(header.Cell(), "Appr");
                    HeaderCell(header.Cell(), "Appr OD");
                    HeaderCell(header.Cell(), "Rel");
                    HeaderCell(header.Cell(), "Rel OD");
                    HeaderCell(header.Cell(), "Res");
                    HeaderCell(header.Cell(), "Res OD");
                });

                foreach (var r in rows)
                {
                    BodyCell(table.Cell(), r.User);
                    BodyCell(table.Cell(), r.Completed.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), r.Overdue.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), r.Approvals.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), r.ApprovalOverdue.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), r.Releases.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), r.ReleaseOverdue.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), r.Resolutions.ToString("N0"), alignRight: true);
                    BodyCell(table.Cell(), r.ResolutionOverdue.ToString("N0"), alignRight: true);
                }
            });
        });
    }

    private static void Card(IContainer container, string title, string value)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(10)
            .Column(col =>
            {
                col.Item().Text(title).FontSize(9).FontColor(Colors.Grey.Darken2);
                col.Item().Text(value).FontSize(16).SemiBold();
            });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(6)
            .PaddingHorizontal(6)
            .AlignMiddle()
            .Text(text)
            .FontSize(9)
            .SemiBold();
    }

    private static void BodyCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(5)
            .PaddingHorizontal(6)
            .AlignMiddle();

        if (alignRight)
        {
            cell.AlignRight().Text(text).FontSize(9);
        }
        else
        {
            cell.AlignLeft().Text(text).FontSize(9);
        }
    }
}

