using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static QuestPDF.Helpers.Colors;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal static class AyudaReleaseReportService
{
	public static string GeneratePdf(AyudaReleaseReportData data)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (data.Beneficiaries.Count == 0)
		{
			throw new InvalidOperationException("At least one beneficiary is required to generate an ayuda release report.");
		}
		string text = (string.IsNullOrWhiteSpace(data.ReportFilePath) ? BuildOutputPath(data.BatchReference, data.ProgramName) : data.ReportFilePath);
		Directory.CreateDirectory(Path.GetDirectoryName(text));
		Settings.License = (LicenseType)0;
		string barangayName = LoadConfig("barangay_name", "Barangay");
		string municipality = LoadConfig("municipality", "Municipality");
		string province = LoadConfig("province", "Province");
		string region = LoadConfig("region", "Region");
		GenerateExtensions.GeneratePdf((IDocument)(object)Document.Create((Action<IDocumentContainer>)delegate(IDocumentContainer container)
		{
			PageExtensions.Page(container, (Action<PageDescriptor>)delegate(PageDescriptor page)
			{
				page.Size(PageSizes.A4);
				page.Margin(32f, (Unit)0);
				page.DefaultTextStyle((Func<TextStyle, TextStyle>)((TextStyle x) => TextStyleExtensions.FontFamily(TextStyleExtensions.FontSize(x, 10f), new string[1] { "Arial" })));
				ColumnExtensions.Column(page.Header(), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
				{
					//IL_002c: Unknown result type (might be due to invalid IL or missing references)
					//IL_0057: Unknown result type (might be due to invalid IL or missing references)
					//IL_0082: Unknown result type (might be due to invalid IL or missing references)
					col.Spacing(3f, (Unit)0);
					TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), region), 10f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), province), 10f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), municipality), 10f), Grey.Darken2);
					TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), barangayName), 14f));
					TextSpanDescriptorExtensions.Bold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(PaddingExtensions.PaddingTop(col.Item(), 10f, (Unit)0)), "AYUDA RELEASE REPORT"), 18f));
				});
				ColumnExtensions.Column(PaddingExtensions.PaddingTop(page.Content(), 16f, (Unit)0), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
				{
					//IL_004e: Unknown result type (might be due to invalid IL or missing references)
					col.Spacing(10f, (Unit)0);
					RowExtensions.Row(col.Item(), (Action<RowDescriptor>)delegate(RowDescriptor row)
					{
						ColumnExtensions.Column(row.RelativeItem(1f), (Action<ColumnDescriptor>)delegate(ColumnDescriptor left)
						{
							left.Spacing(2f, (Unit)0);
							TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(left.Item(), "Batch Reference: " + data.BatchReference));
							TextExtensions.Text(left.Item(), "Program: " + data.ProgramName);
							TextExtensions.Text(left.Item(), "Category: " + data.Category);
							TextExtensions.Text(left.Item(), $"Release Date: {data.ReleaseDate:MMMM dd, yyyy}");
						});
						ColumnExtensions.Column(row.RelativeItem(1f), (Action<ColumnDescriptor>)delegate(ColumnDescriptor right)
						{
							//IL_00de: Unknown result type (might be due to invalid IL or missing references)
							right.Spacing(2f, (Unit)0);
							TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignRight(right.Item()), $"Beneficiaries: {data.BeneficiaryCount:N0}"));
							TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignRight(right.Item()), $"Total Released: PHP {data.TotalAmount:N2}"));
							TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignRight(right.Item()), $"Generated: {DateTime.Now:MMM dd, yyyy hh:mm tt}"), Grey.Darken2);
						});
					});
					if (!string.IsNullOrWhiteSpace(data.Notes))
					{
						ColumnExtensions.Column(PaddingExtensions.Padding(StyledBoxExtensions.Background(col.Item(), Grey.Lighten4), 12f, (Unit)0), (Action<ColumnDescriptor>)delegate(ColumnDescriptor section)
						{
							section.Spacing(4f, (Unit)0);
							TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(section.Item(), "Batch Notes"), 11f));
							TextExtensions.Text(section.Item(), data.Notes.Trim());
						});
					}
					TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "Beneficiary List"), 12f));
					TableExtensions.Table(col.Item(), (Action<TableDescriptor>)delegate(TableDescriptor table)
					{
						table.ColumnsDefinition((Action<TableColumnsDefinitionDescriptor>)delegate(TableColumnsDefinitionDescriptor columns)
						{
							columns.RelativeColumn(2f);
							columns.RelativeColumn(3f);
							columns.RelativeColumn(2f);
							columns.RelativeColumn(2f);
						});
						table.Header((Action<TableCellDescriptor>)delegate(TableCellDescriptor header)
						{
							HeaderCell((IContainer)(object)header.Cell(), "Reference");
							HeaderCell((IContainer)(object)header.Cell(), "Beneficiary");
							HeaderCell((IContainer)(object)header.Cell(), "Contact");
							HeaderCell((IContainer)(object)header.Cell(), "Amount");
						});
						foreach (AyudaReleaseReportBeneficiaryRow item in data.Beneficiaries.OrderBy<AyudaReleaseReportBeneficiaryRow, string>((AyudaReleaseReportBeneficiaryRow row) => row.ResidentName, StringComparer.OrdinalIgnoreCase))
						{
							BodyCell((IContainer)(object)table.Cell(), item.ReferenceNo);
							BodyCell((IContainer)(object)table.Cell(), item.ResidentName);
							BodyCell((IContainer)(object)table.Cell(), string.IsNullOrWhiteSpace(item.ContactNo) ? "-" : item.ContactNo);
							BodyCell((IContainer)(object)table.Cell(), $"PHP {item.Amount:N2}", alignRight: true);
						}
					});
				});
				TextExtensions.Text(AlignmentExtensions.AlignCenter(page.Footer()), (Action<TextDescriptor>)delegate(TextDescriptor val)
				{
					//IL_0025: Unknown result type (might be due to invalid IL or missing references)
					//IL_0045: Unknown result type (might be due to invalid IL or missing references)
					//IL_0060: Unknown result type (might be due to invalid IL or missing references)
					//IL_0080: Unknown result type (might be due to invalid IL or missing references)
					//IL_009b: Unknown result type (might be due to invalid IL or missing references)
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(val.Span("Generated by " + data.GeneratedBy), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(val.Span(" | "), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextPageNumberDescriptor>(TextSpanDescriptorExtensions.FontSize<TextPageNumberDescriptor>(val.CurrentPageNumber(), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(val.Span(" / "), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextPageNumberDescriptor>(TextSpanDescriptorExtensions.FontSize<TextPageNumberDescriptor>(val.TotalPages(), 9f), Grey.Darken2);
				});
			});
		}), text);
		return text;
	}

	public static void TryOpenGeneratedFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = filePath,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to open generated ayuda release report automatically.", ex);
		}
	}

	private static string LoadConfig(string key, string defaultValue)
	{
		SystemConfigService.EnsureTable();
		return SystemConfigService.Get(key, defaultValue);
	}

	private static string BuildOutputPath(string batchReference, string programName)
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), MakeSafeFileName(SystemConfigService.GetSystemName()), "Ayuda Reports");
		string value = MakeSafeFileName(programName);
		string value2 = MakeSafeFileName(batchReference);
		string path2 = $"{value}_{value2}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		return Path.Combine(path, path2);
	}

	private static string MakeSafeFileName(string value)
	{
		StringBuilder stringBuilder = new StringBuilder(value.Length);
		string text = value ?? string.Empty;
		foreach (char c in text)
		{
			stringBuilder.Append(Enumerable.Contains(Path.GetInvalidFileNameChars(), c) ? '_' : c);
		}
		string text2 = stringBuilder.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return "Ayuda_Report";
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
