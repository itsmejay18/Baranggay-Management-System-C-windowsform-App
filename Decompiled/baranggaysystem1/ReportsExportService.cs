using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static QuestPDF.Helpers.Colors;

namespace baranggaysystem1;

internal static class ReportsExportService
{
	public static void ExportDashboardExcel(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		EnsureDirectory(filePath);
		XLWorkbook val = new XLWorkbook();
		try
		{
			WriteSummarySheet((IXLWorkbook)(object)val, data, from, to);
			WriteTrendsSheet((IXLWorkbook)(object)val, data);
			WriteHotspotsSheet((IXLWorkbook)(object)val, data);
			WriteStaffSheet((IXLWorkbook)(object)val, data);
			val.SaveAs(filePath);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static void ExportDashboardPdf(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		EnsureDirectory(filePath);
		Settings.License = (LicenseType)0;
		GenerateExtensions.GeneratePdf((IDocument)(object)Document.Create((Action<IDocumentContainer>)delegate(IDocumentContainer container)
		{
			PageExtensions.Page(container, (Action<PageDescriptor>)delegate(PageDescriptor page)
			{
				page.Size(PageSizes.A4);
				page.Margin(32f, (Unit)0);
				page.DefaultTextStyle((Func<TextStyle, TextStyle>)((TextStyle x) => TextStyleExtensions.FontSize(x, 11f)));
				ElementExtensions.Element(page.Header(), (Action<IContainer>)delegate(IContainer header)
				{
					RowExtensions.Row(header, (Action<RowDescriptor>)delegate(RowDescriptor row)
					{
						ColumnExtensions.Column(row.RelativeItem(1f), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
						{
							//IL_0082: Unknown result type (might be due to invalid IL or missing references)
							TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "Barangay System Reports"), 18f));
							TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), $"Range: {from:MMM dd, yyyy} - {to:MMM dd, yyyy}"), 10f), Grey.Darken2);
						});
						ColumnExtensions.Column(AlignmentExtensions.AlignRight(row.ConstantItem(160f, (Unit)0)), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
						{
							//IL_0043: Unknown result type (might be due to invalid IL or missing references)
							TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), $"Generated: {DateTime.Now:MMM dd, yyyy hh:mm tt}"), 9f), Grey.Darken2);
						});
					});
				}, "header =>\n                {\n                    header.Row(row =>\n                    {\n                        row.RelativeItem().Column(col =>\n                        {\n                            col.Item().Text(\"Barangay System Reports\").FontSize(18).SemiBold();\n                            col.Item().Text($\"Range: {from:MMM dd, yyyy} - {to:MMM dd, yyyy}\").FontSize(10).FontColor(Colors.Grey.Darken2);\n                        });\n                        row.ConstantItem(160).AlignRight().Column(col =>\n                        {\n                            col.Item().Text($\"Generated: {DateTime.Now:MMM dd, yyyy hh:mm tt}\").FontSize(9).FontColor(Colors.Grey.Darken2);\n                        });\n                    });\n                }", "ExportDashboardPdf", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 53);
				ColumnExtensions.Column(page.Content(), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
				{
					col.Spacing(14f, (Unit)0);
					ElementExtensions.Element(col.Item(), (Action<IContainer>)delegate(IContainer x)
					{
						BuildSummaryCards(x, data);
					}, "x => BuildSummaryCards(x, data)", "ExportDashboardPdf", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 73);
					ElementExtensions.Element(col.Item(), (Action<IContainer>)delegate(IContainer x)
					{
						BuildServiceTimes(x, data);
					}, "x => BuildServiceTimes(x, data)", "ExportDashboardPdf", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 74);
					ElementExtensions.Element(col.Item(), (Action<IContainer>)delegate(IContainer x)
					{
						BuildTrendsTable(x, data.Trends);
					}, "x => BuildTrendsTable(x, data.Trends)", "ExportDashboardPdf", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 75);
					ElementExtensions.Element(col.Item(), (Action<IContainer>)delegate(IContainer x)
					{
						BuildHotspotsTable(x, data.Hotspots);
					}, "x => BuildHotspotsTable(x, data.Hotspots)", "ExportDashboardPdf", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 76);
					ElementExtensions.Element(col.Item(), (Action<IContainer>)delegate(IContainer x)
					{
						BuildStaffTable(x, data.StaffPerformance);
					}, "x => BuildStaffTable(x, data.StaffPerformance)", "ExportDashboardPdf", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 79);
				});
				TextExtensions.Text(AlignmentExtensions.AlignCenter(page.Footer()), (Action<TextDescriptor>)delegate(TextDescriptor text)
				{
					//IL_0015: Unknown result type (might be due to invalid IL or missing references)
					//IL_0035: Unknown result type (might be due to invalid IL or missing references)
					//IL_0050: Unknown result type (might be due to invalid IL or missing references)
					//IL_0070: Unknown result type (might be due to invalid IL or missing references)
					//IL_008b: Unknown result type (might be due to invalid IL or missing references)
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(text.Span("Barangay System"), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(text.Span("  |  "), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextPageNumberDescriptor>(TextSpanDescriptorExtensions.FontSize<TextPageNumberDescriptor>(text.CurrentPageNumber(), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(text.Span(" / "), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextPageNumberDescriptor>(TextSpanDescriptorExtensions.FontSize<TextPageNumberDescriptor>(text.TotalPages(), 9f), Grey.Darken2);
				});
			});
		}), filePath);
	}

	private static void EnsureDirectory(string filePath)
	{
		string directoryName = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
	}

	private static void WriteSummarySheet(IXLWorkbook wb, ReportsDashboardData data, DateTime from, DateTime to)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		IXLWorksheet obj = wb.AddWorksheet("Summary");
		obj.Cell(1, 1).Value = "Barangay System Reports";
		((IXLFontBase)obj.Cell(1, 1).Style.Font).Bold = true;
		((IXLFontBase)obj.Cell(1, 1).Style.Font).FontSize = 16.0;
		obj.Cell(2, 1).Value = "Date range:";
		obj.Cell(2, 2).Value = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
		obj.Cell(4, 1).Value = "Summary";
		((IXLFontBase)obj.Cell(4, 1).Style.Font).Bold = true;
		int num = 5;
		WriteKeyValue(obj, num++, "New residents", data.Summary.NewResidents);
		WriteKeyValue(obj, num++, "Certificate requests", data.Summary.CertificateRequests);
		WriteKeyValue(obj, num++, "Certificates released", data.Summary.CertificatesReleased);
		WriteKeyValue(obj, num++, "Blotter cases filed", data.Summary.BlottersFiled);
		WriteKeyValue(obj, num++, "Total residents", data.Summary.TotalResidents);
		WriteKeyValue(obj, num++, "Pending certificates", data.Summary.PendingCertificates);
		WriteKeyValue(obj, num++, "Active blotter cases", data.Summary.ActiveBlotters);
		num++;
		obj.Cell(num, 1).Value = "Service times";
		((IXLFontBase)obj.Cell(num, 1).Style.Font).Bold = true;
		num++;
		obj.Cell(num, 1).Value = "Avg request -> approval";
		obj.Cell(num, 2).Value = FormatDuration(data.ServiceTimes.AvgRequestToApprovalSeconds);
		obj.Cell(num, 3).Value = $"Samples: {data.ServiceTimes.ApprovalSamples}";
		num++;
		obj.Cell(num, 1).Value = "Avg approval -> release";
		obj.Cell(num, 2).Value = FormatDuration(data.ServiceTimes.AvgApprovalToReleaseSeconds);
		obj.Cell(num, 3).Value = $"Samples: {data.ServiceTimes.ReleaseSamples}";
		obj.Columns().AdjustToContents();
	}

	private static void WriteTrendsSheet(IXLWorkbook wb, ReportsDashboardData data)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		IXLWorksheet val = wb.AddWorksheet("Monthly Trends");
		val.Cell(1, 1).Value = "Month";
		val.Cell(1, 2).Value = "Residents";
		val.Cell(1, 3).Value = "Certificates";
		val.Cell(1, 4).Value = "Blotter";
		((IXLFontBase)((IXLRangeBase)val.Range(1, 1, 1, 4)).Style.Font).Bold = true;
		((IXLRangeBase)val.Range(1, 1, 1, 4)).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
		int num = 2;
		foreach (MonthlyTrendRow item in data.Trends ?? Array.Empty<MonthlyTrendRow>())
		{
			val.Cell(num, 1).Value = item.MonthLabel;
			val.Cell(num, 2).Value = item.Residents;
			val.Cell(num, 3).Value = item.Certificates;
			val.Cell(num, 4).Value = item.Blotters;
			num++;
		}
		val.Columns().AdjustToContents();
		val.SheetView.FreezeRows(1);
	}

	private static void WriteStaffSheet(IXLWorkbook wb, ReportsDashboardData data)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		IXLWorksheet val = wb.AddWorksheet("Staff Performance");
		string[] array = new string[13]
		{
			"User", "Completed", "Overdue", "Cert Approvals", "Approval Overdue", "Avg Req->Approve", "Cert Releases", "Release Overdue", "Avg Approve->Release", "Blotter Updates",
			"Resolutions", "Resolution Overdue", "Avg Resolution"
		};
		for (int i = 0; i < array.Length; i++)
		{
			val.Cell(1, i + 1).Value = array[i];
		}
		((IXLFontBase)((IXLRangeBase)val.Range(1, 1, 1, array.Length)).Style.Font).Bold = true;
		((IXLRangeBase)val.Range(1, 1, 1, array.Length)).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
		var list = (from r in (data.StaffPerformance ?? Array.Empty<StaffPerformanceRow>()).Where((StaffPerformanceRow r) => r.IsActive || HasAnyActivity(r)).Select(delegate(StaffPerformanceRow r)
			{
				int completed = r.ApprovalsCompleted + r.ReleasesCompleted + r.BlotterResolutions;
				int overdue = r.ApprovalsOverdue + r.ReleasesOverdue + r.BlotterResolutionsOverdue;
				return new
				{
					User = FormatUser(r),
					Completed = completed,
					Overdue = overdue,
					ApprovalsCompleted = r.ApprovalsCompleted,
					ApprovalsOverdue = r.ApprovalsOverdue,
					AvgApproval = FormatDuration(r.AvgRequestToApprovalSeconds),
					ReleasesCompleted = r.ReleasesCompleted,
					ReleasesOverdue = r.ReleasesOverdue,
					AvgRelease = FormatDuration(r.AvgApprovalToReleaseSeconds),
					BlotterStatusChanges = r.BlotterStatusChanges,
					BlotterResolutions = r.BlotterResolutions,
					BlotterResolutionsOverdue = r.BlotterResolutionsOverdue,
					AvgResolution = FormatDuration(r.AvgBlotterResolutionSeconds)
				};
			})
			orderby r.Completed descending, r.Overdue
			select r).ThenBy(r => r.User, StringComparer.OrdinalIgnoreCase).ToList();
		int num = 2;
		foreach (var item in list)
		{
			val.Cell(num, 1).Value = item.User;
			val.Cell(num, 2).Value = item.Completed;
			val.Cell(num, 3).Value = item.Overdue;
			val.Cell(num, 4).Value = item.ApprovalsCompleted;
			val.Cell(num, 5).Value = item.ApprovalsOverdue;
			val.Cell(num, 6).Value = item.AvgApproval;
			val.Cell(num, 7).Value = item.ReleasesCompleted;
			val.Cell(num, 8).Value = item.ReleasesOverdue;
			val.Cell(num, 9).Value = item.AvgRelease;
			val.Cell(num, 10).Value = item.BlotterStatusChanges;
			val.Cell(num, 11).Value = item.BlotterResolutions;
			val.Cell(num, 12).Value = item.BlotterResolutionsOverdue;
			val.Cell(num, 13).Value = item.AvgResolution;
			num++;
		}
		val.Columns().AdjustToContents();
		val.SheetView.FreezeRows(1);
	}

	private static void WriteHotspotsSheet(IXLWorkbook wb, ReportsDashboardData data)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		IXLWorksheet val = wb.AddWorksheet("Hotspots");
		val.Cell(1, 1).Value = "Purok";
		val.Cell(1, 2).Value = "Incidents";
		val.Cell(1, 3).Value = "Latitude";
		val.Cell(1, 4).Value = "Longitude";
		val.Cell(1, 5).Value = "Coordinates";
		((IXLFontBase)((IXLRangeBase)val.Range(1, 1, 1, 5)).Style.Font).Bold = true;
		((IXLRangeBase)val.Range(1, 1, 1, 5)).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
		int num = 2;
		foreach (HotspotPoint item in (data.Hotspots ?? Array.Empty<HotspotPoint>()).OrderByDescending((HotspotPoint point) => point.IncidentCount).ThenBy<HotspotPoint, string>((HotspotPoint point) => point.PurokName, StringComparer.OrdinalIgnoreCase))
		{
			val.Cell(num, 1).Value = item.PurokName;
			val.Cell(num, 2).Value = item.IncidentCount;
			if (item.Latitude.HasValue)
			{
				val.Cell(num, 3).Value = item.Latitude.Value;
			}
			else
			{
				val.Cell(num, 3).Value = string.Empty;
			}
			if (item.Longitude.HasValue)
			{
				val.Cell(num, 4).Value = item.Longitude.Value;
			}
			else
			{
				val.Cell(num, 4).Value = string.Empty;
			}
			val.Cell(num, 5).Value = (item.Latitude.HasValue && item.Longitude.HasValue) ? $"{item.Latitude.Value:0.0000}, {item.Longitude.Value:0.0000}" : "Not mapped";
			num++;
		}
		val.Columns().AdjustToContents();
		val.SheetView.FreezeRows(1);
	}

	private static void WriteKeyValue(IXLWorksheet ws, int row, string key, int value)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		ws.Cell(row, 1).Value = key;
		ws.Cell(row, 2).Value = value;
	}

	private static bool HasAnyActivity(StaffPerformanceRow row)
	{
		if (row.ApprovalsCompleted <= 0 && row.ReleasesCompleted <= 0 && row.BlotterStatusChanges <= 0)
		{
			return row.BlotterResolutions > 0;
		}
		return true;
	}

	private static string FormatUser(StaffPerformanceRow row)
	{
		string text = (string.IsNullOrWhiteSpace(row.DisplayName) ? row.Username : row.DisplayName);
		if (!string.IsNullOrWhiteSpace(row.Username) && !string.Equals(text, row.Username, StringComparison.OrdinalIgnoreCase))
		{
			text = row.Username + " (" + text + ")";
		}
		if (!row.IsActive)
		{
			text += " [inactive]";
		}
		return text;
	}

	private static string FormatDuration(double seconds)
	{
		if (seconds <= 0.0)
		{
			return "-";
		}
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
		if (timeSpan.TotalMinutes < 1.0)
		{
			return "<1m";
		}
		if (!(timeSpan.TotalHours < 1.0))
		{
			if (!(timeSpan.TotalDays < 1.0))
			{
				int num = (int)Math.Floor(timeSpan.TotalDays);
				if (num < 10 && timeSpan.Hours > 0)
				{
					return $"{num}d {timeSpan.Hours}h";
				}
				return $"{timeSpan.TotalDays:0.#}d";
			}
			return $"{timeSpan.TotalHours:0.#}h";
		}
		return $"{timeSpan.TotalMinutes:0}m";
	}

	private static void BuildSummaryCards(IContainer container, ReportsDashboardData data)
	{
		RowExtensions.Row(container, (Action<RowDescriptor>)delegate(RowDescriptor row)
		{
			row.Spacing(10f, (Unit)0);
			ElementExtensions.Element(row.RelativeItem(1f), (Action<IContainer>)delegate(IContainer c)
			{
				Card(c, "New residents", data.Summary.NewResidents.ToString("N0"));
			}, "c => Card(c, \"New residents\", data.Summary.NewResidents.ToString(\"N0\"))", "BuildSummaryCards", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 358);
			ElementExtensions.Element(row.RelativeItem(1f), (Action<IContainer>)delegate(IContainer c)
			{
				Card(c, "Cert requests", data.Summary.CertificateRequests.ToString("N0"));
			}, "c => Card(c, \"Cert requests\", data.Summary.CertificateRequests.ToString(\"N0\"))", "BuildSummaryCards", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 359);
			ElementExtensions.Element(row.RelativeItem(1f), (Action<IContainer>)delegate(IContainer c)
			{
				Card(c, "Cert released", data.Summary.CertificatesReleased.ToString("N0"));
			}, "c => Card(c, \"Cert released\", data.Summary.CertificatesReleased.ToString(\"N0\"))", "BuildSummaryCards", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 360);
			ElementExtensions.Element(row.RelativeItem(1f), (Action<IContainer>)delegate(IContainer c)
			{
				Card(c, "Blotter filed", data.Summary.BlottersFiled.ToString("N0"));
			}, "c => Card(c, \"Blotter filed\", data.Summary.BlottersFiled.ToString(\"N0\"))", "BuildSummaryCards", "C:\\Users\\Mahiru Shiina\\Documents\\vscode projects\\Baranggay-Management-System-C-windowsform-App - Copy\\Services\\ReportsExportService.cs", 361);
		});
	}

	private static void BuildServiceTimes(IContainer container, ReportsDashboardData data)
	{
		ColumnExtensions.Column(container, (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
		{
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			col.Spacing(4f, (Unit)0);
			TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "Service Time Metrics"));
			TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), $"Avg request -> approval: {FormatDuration(data.ServiceTimes.AvgRequestToApprovalSeconds)} (samples: {data.ServiceTimes.ApprovalSamples:N0})"), 10f), Grey.Darken2);
			TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), $"Avg approval -> release: {FormatDuration(data.ServiceTimes.AvgApprovalToReleaseSeconds)} (samples: {data.ServiceTimes.ReleaseSamples:N0})"), 10f), Grey.Darken2);
		});
	}

	private static void BuildTrendsTable(IContainer container, IReadOnlyList<MonthlyTrendRow> trends)
	{
		ColumnExtensions.Column(container, (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
		{
			col.Spacing(6f, (Unit)0);
			TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "Monthly Trends"));
			TableExtensions.Table(col.Item(), (Action<TableDescriptor>)delegate(TableDescriptor table)
			{
				table.ColumnsDefinition((Action<TableColumnsDefinitionDescriptor>)delegate(TableColumnsDefinitionDescriptor columns)
				{
					columns.RelativeColumn(2f);
					columns.RelativeColumn(1f);
					columns.RelativeColumn(1f);
					columns.RelativeColumn(1f);
				});
				table.Header((Action<TableCellDescriptor>)delegate(TableCellDescriptor header)
				{
					HeaderCell((IContainer)(object)header.Cell(), "Month");
					HeaderCell((IContainer)(object)header.Cell(), "Residents");
					HeaderCell((IContainer)(object)header.Cell(), "Certificates");
					HeaderCell((IContainer)(object)header.Cell(), "Blotter");
				});
				foreach (MonthlyTrendRow item in trends ?? Array.Empty<MonthlyTrendRow>())
				{
					BodyCell((IContainer)(object)table.Cell(), item.MonthLabel);
					BodyCell((IContainer)(object)table.Cell(), item.Residents.ToString("N0"), alignRight: true);
					BodyCell((IContainer)(object)table.Cell(), item.Certificates.ToString("N0"), alignRight: true);
					BodyCell((IContainer)(object)table.Cell(), item.Blotters.ToString("N0"), alignRight: true);
				}
			});
		});
	}

	private static void BuildHotspotsTable(IContainer container, IReadOnlyList<HotspotPoint> hotspots)
	{
		List<HotspotPoint> rows = (hotspots ?? Array.Empty<HotspotPoint>()).OrderByDescending((HotspotPoint point) => point.IncidentCount).ThenBy<HotspotPoint, string>((HotspotPoint point) => point.PurokName, StringComparer.OrdinalIgnoreCase).Take(8)
			.ToList();
		ColumnExtensions.Column(container, (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
		{
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			col.Spacing(6f, (Unit)0);
			TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "Hotspot Watchlist"));
			if (rows.Count == 0 || rows.All((HotspotPoint row) => row.IncidentCount <= 0))
			{
				TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "No hotspot incidents were recorded in the selected date range."), 10f), Grey.Darken2);
			}
			else
			{
				TableExtensions.Table(col.Item(), (Action<TableDescriptor>)delegate(TableDescriptor table)
				{
					table.ColumnsDefinition((Action<TableColumnsDefinitionDescriptor>)delegate(TableColumnsDefinitionDescriptor columns)
					{
						columns.RelativeColumn(2f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(2f);
					});
					table.Header((Action<TableCellDescriptor>)delegate(TableCellDescriptor header)
					{
						HeaderCell((IContainer)(object)header.Cell(), "Purok");
						HeaderCell((IContainer)(object)header.Cell(), "Incidents");
						HeaderCell((IContainer)(object)header.Cell(), "Coordinates");
					});
					foreach (HotspotPoint item in rows)
					{
						BodyCell((IContainer)(object)table.Cell(), item.PurokName);
						BodyCell((IContainer)(object)table.Cell(), item.IncidentCount.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), (item.Latitude.HasValue && item.Longitude.HasValue) ? $"{item.Latitude.Value:0.0000}, {item.Longitude.Value:0.0000}" : "Not mapped");
					}
				});
			}
		});
	}

	private static void BuildStaffTable(IContainer container, IReadOnlyList<StaffPerformanceRow> staff)
	{
		var rows = (from r in (staff ?? Array.Empty<StaffPerformanceRow>()).Where((StaffPerformanceRow r) => r.IsActive || HasAnyActivity(r)).Select(delegate(StaffPerformanceRow r)
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
			orderby r.Completed descending, r.Overdue
			select r).ThenBy(r => r.User, StringComparer.OrdinalIgnoreCase).ToList();
		ColumnExtensions.Column(container, (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
		{
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			col.Spacing(6f, (Unit)0);
			TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "Staff Performance"));
			if (rows.Count == 0)
			{
				TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "No staff activity in the selected date range."), 10f), Grey.Darken2);
			}
			else
			{
				TableExtensions.Table(col.Item(), (Action<TableDescriptor>)delegate(TableDescriptor table)
				{
					table.ColumnsDefinition((Action<TableColumnsDefinitionDescriptor>)delegate(TableColumnsDefinitionDescriptor columns)
					{
						columns.RelativeColumn(3f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(1f);
						columns.RelativeColumn(1f);
					});
					table.Header((Action<TableCellDescriptor>)delegate(TableCellDescriptor header)
					{
						HeaderCell((IContainer)(object)header.Cell(), "User");
						HeaderCell((IContainer)(object)header.Cell(), "Done");
						HeaderCell((IContainer)(object)header.Cell(), "Overdue");
						HeaderCell((IContainer)(object)header.Cell(), "Appr");
						HeaderCell((IContainer)(object)header.Cell(), "Appr OD");
						HeaderCell((IContainer)(object)header.Cell(), "Rel");
						HeaderCell((IContainer)(object)header.Cell(), "Rel OD");
						HeaderCell((IContainer)(object)header.Cell(), "Res");
						HeaderCell((IContainer)(object)header.Cell(), "Res OD");
					});
					foreach (var item in rows)
					{
						BodyCell((IContainer)(object)table.Cell(), item.User);
						BodyCell((IContainer)(object)table.Cell(), item.Completed.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), item.Overdue.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), item.Approvals.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), item.ApprovalOverdue.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), item.Releases.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), item.ReleaseOverdue.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), item.Resolutions.ToString("N0"), alignRight: true);
						BodyCell((IContainer)(object)table.Cell(), item.ResolutionOverdue.ToString("N0"), alignRight: true);
					}
				});
			}
		});
	}

	private static void Card(IContainer container, string title, string value)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		ColumnExtensions.Column(PaddingExtensions.Padding(StyledBoxExtensions.Background(StyledBoxExtensions.BorderColor(StyledBoxExtensions.Border(container, 1f, (Unit)0), Grey.Lighten2), Colors.White), 10f, (Unit)0), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), title), 9f), Grey.Darken2);
			TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), value), 16f));
		});
	}

	private static void HeaderCell(IContainer container, string text)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignMiddle(PaddingExtensions.PaddingHorizontal(PaddingExtensions.PaddingVertical(StyledBoxExtensions.BorderColor(StyledBoxExtensions.BorderBottom(StyledBoxExtensions.Background(container, Grey.Lighten3), 1f, (Unit)0), Grey.Lighten2), 6f, (Unit)0), 6f, (Unit)0)), text), 9f));
	}

	private static void BodyCell(IContainer container, string text, bool alignRight = false)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		IContainer val = AlignmentExtensions.AlignMiddle(PaddingExtensions.PaddingHorizontal(PaddingExtensions.PaddingVertical(StyledBoxExtensions.BorderColor(StyledBoxExtensions.BorderBottom(container, 1f, (Unit)0), Grey.Lighten3), 5f, (Unit)0), 6f, (Unit)0));
		if (alignRight)
		{
			TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignRight(val), text), 9f);
		}
		else
		{
			TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignLeft(val), text), 9f);
		}
	}
}
