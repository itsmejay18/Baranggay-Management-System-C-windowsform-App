using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using baranggaysystem1.Database;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class HouseholdCertificateService
{
	private sealed class CertificateMemberRow
	{
		public string FullName { get; init; } = string.Empty;

		public string AgeLabel { get; init; } = string.Empty;

		public string SexLabel { get; init; } = string.Empty;

		public string StatusLabel { get; init; } = string.Empty;

		public bool IsHead { get; init; }
	}

	private sealed class SignatoryInfo
	{
		public string Name { get; init; } = string.Empty;

		public string Position { get; init; } = string.Empty;
	}

	public static string GeneratePdf(int householdId, HouseholdCertificateRequest request)
	{
		if (householdId <= 0)
		{
			throw new InvalidOperationException("A household must be selected first.");
		}
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		if (string.IsNullOrWhiteSpace(request.Purpose))
		{
			throw new InvalidOperationException("Certificate purpose is required.");
		}
		int barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		HouseholdRepository householdRepository = new HouseholdRepository();
		HouseholdDetailsDto details = householdRepository.GetDetails(householdId, barangayId);
		if (details == null)
		{
			throw new InvalidOperationException("Selected household could not be loaded.");
		}
		List<CertificateMemberRow> members = LoadMembers(householdId, barangayId);
		string barangayName = LoadConfig("barangay_name", "Barangay San Jose");
		string municipality = LoadConfig("municipality", "Municipality");
		string province = LoadConfig("province", "Province");
		string region = LoadConfig("region", "Region");
		SignatoryInfo signatory = LoadPrimarySignatory(barangayId);
		string text = BuildOutputPath(details);
		Directory.CreateDirectory(Path.GetDirectoryName(text));
		Settings.License = (LicenseType)0;
		GenerateExtensions.GeneratePdf((IDocument)(object)Document.Create((Action<IDocumentContainer>)delegate(IDocumentContainer container)
		{
			PageExtensions.Page(container, (Action<PageDescriptor>)delegate(PageDescriptor page)
			{
				page.Size(PageSizes.A4);
				page.Margin(34f, (Unit)0);
				page.DefaultTextStyle((Func<TextStyle, TextStyle>)((TextStyle x) => TextStyleExtensions.FontFamily(TextStyleExtensions.FontSize(x, 11f), new string[1] { "Arial" })));
				ColumnExtensions.Column(page.Header(), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
				{
					//IL_002c: Unknown result type (might be due to invalid IL or missing references)
					//IL_0057: Unknown result type (might be due to invalid IL or missing references)
					//IL_0082: Unknown result type (might be due to invalid IL or missing references)
					col.Spacing(4f, (Unit)0);
					TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), region), 10f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), province), 10f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), municipality), 10f), Grey.Darken2);
					TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(col.Item()), barangayName), 15f));
					TextSpanDescriptorExtensions.Bold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(PaddingExtensions.PaddingTop(col.Item(), 10f, (Unit)0)), "HOUSEHOLD CERTIFICATION"), 18f));
				});
				ColumnExtensions.Column(PaddingExtensions.PaddingTop(page.Content(), 16f, (Unit)0), (Action<ColumnDescriptor>)delegate(ColumnDescriptor col)
				{
					//IL_0055: Unknown result type (might be due to invalid IL or missing references)
					//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
					col.Spacing(12f, (Unit)0);
					TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), $"Issued on {request.IssuedDate:MMMM dd, yyyy}"), 10f), Grey.Darken2);
					if (!string.IsNullOrWhiteSpace(request.PresentedTo))
					{
						TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "To: " + request.PresentedTo.Trim()));
					}
					TextExtensions.Text(col.Item(), (Action<TextDescriptor>)delegate(TextDescriptor val)
					{
						val.Span("This is to certify that the household located at ");
						TextSpanDescriptorExtensions.SemiBold<TextSpanDescriptor>(val.Span(details.FullAddress));
						val.Span(", ");
						TextSpanDescriptorExtensions.SemiBold<TextSpanDescriptor>(val.Span(details.PurokName));
						val.Span(", is a registered household of ");
						TextSpanDescriptorExtensions.SemiBold<TextSpanDescriptor>(val.Span(barangayName));
						val.Span(" with ");
						TextSpanDescriptorExtensions.SemiBold<TextSpanDescriptor>(val.Span(details.MemberCount.ToString(CultureInfo.InvariantCulture)));
						val.Span(" member(s) currently on record.");
					});
					TextExtensions.Text(col.Item(), (Action<TextDescriptor>)delegate(TextDescriptor val)
					{
						val.Span("Purpose: ");
						TextSpanDescriptorExtensions.SemiBold<TextSpanDescriptor>(val.Span(request.Purpose.Trim()));
					});
					ColumnExtensions.Column(PaddingExtensions.Padding(StyledBoxExtensions.Background(col.Item(), Grey.Lighten4), 14f, (Unit)0), (Action<ColumnDescriptor>)delegate(ColumnDescriptor section)
					{
						section.Spacing(6f, (Unit)0);
						TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(section.Item(), "Household Summary"), 12f));
						TextExtensions.Text(section.Item(), "Address: " + details.FullAddress);
						TextExtensions.Text(section.Item(), "Purok: " + details.PurokName);
						TextExtensions.Text(section.Item(), $"Members: {details.MemberCount}");
						TextExtensions.Text(section.Item(), $"Senior Citizens: {details.SeniorCount}");
						TextExtensions.Text(section.Item(), $"PWD Members: {details.PwdCount}");
						TextExtensions.Text(section.Item(), $"4Ps Members: {details.FourPsCount}");
						TextExtensions.Text(section.Item(), $"Registered Voters: {details.VoterCount}");
					});
					if (request.IncludeMemberRoster && members.Count > 0)
					{
						TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(col.Item(), "Certified Household Members"), 12f));
						TableExtensions.Table(col.Item(), (Action<TableDescriptor>)delegate(TableDescriptor table)
						{
							table.ColumnsDefinition((Action<TableColumnsDefinitionDescriptor>)delegate(TableColumnsDefinitionDescriptor columns)
							{
								columns.RelativeColumn(4f);
								columns.RelativeColumn(1f);
								columns.RelativeColumn(1f);
								columns.RelativeColumn(2f);
							});
							table.Header((Action<TableCellDescriptor>)delegate(TableCellDescriptor header)
							{
								HeaderCell((IContainer)(object)header.Cell(), "Name");
								HeaderCell((IContainer)(object)header.Cell(), "Age");
								HeaderCell((IContainer)(object)header.Cell(), "Sex");
								HeaderCell((IContainer)(object)header.Cell(), "Status");
							});
							foreach (CertificateMemberRow item in members)
							{
								string text2 = (item.IsHead ? (item.FullName + " (Head of Family)") : item.FullName);
								BodyCell((IContainer)(object)table.Cell(), text2);
								BodyCell((IContainer)(object)table.Cell(), item.AgeLabel, alignRight: true);
								BodyCell((IContainer)(object)table.Cell(), item.SexLabel);
								BodyCell((IContainer)(object)table.Cell(), item.StatusLabel);
							}
						});
					}
					TextSpanDescriptorExtensions.LineHeight<TextBlockDescriptor>(TextExtensions.Text(PaddingExtensions.PaddingTop(col.Item(), 12f, (Unit)0), "Issued upon request for whatever lawful purpose it may serve."), (float?)1.4f);
					ColumnExtensions.Column(AlignmentExtensions.AlignRight(PaddingExtensions.PaddingTop(col.Item(), 34f, (Unit)0)), (Action<ColumnDescriptor>)delegate(ColumnDescriptor signature)
					{
						//IL_0028: Unknown result type (might be due to invalid IL or missing references)
						//IL_0079: Unknown result type (might be due to invalid IL or missing references)
						signature.Spacing(2f, (Unit)0);
						StyledBoxExtensions.BorderColor(StyledBoxExtensions.BorderBottom(ConstrainedExtensions.Width(signature.Item(), 180f, (Unit)0), 1f, (Unit)0), Colors.Black);
						TextSpanDescriptorExtensions.SemiBold<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(signature.Item()), signatory.Name));
						TextSpanDescriptorExtensions.FontColor<TextBlockDescriptor>(TextSpanDescriptorExtensions.FontSize<TextBlockDescriptor>(TextExtensions.Text(AlignmentExtensions.AlignCenter(signature.Item()), signatory.Position), 10f), Grey.Darken2);
					});
				});
				TextExtensions.Text(AlignmentExtensions.AlignCenter(page.Footer()), (Action<TextDescriptor>)delegate(TextDescriptor val)
				{
					//IL_0025: Unknown result type (might be due to invalid IL or missing references)
					//IL_0045: Unknown result type (might be due to invalid IL or missing references)
					//IL_008f: Unknown result type (might be due to invalid IL or missing references)
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(val.Span("Generated by " + request.GeneratedBy), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(val.Span(" | "), 9f), Grey.Darken2);
					TextSpanDescriptorExtensions.FontColor<TextSpanDescriptor>(TextSpanDescriptorExtensions.FontSize<TextSpanDescriptor>(val.Span($"Household #{details.HouseholdId}"), 9f), Grey.Darken2);
				});
			});
		}), text);
		AuditTrailService.Log("Households", "household", householdId, "GENERATE_CERTIFICATE", null, new
		{
			HouseholdId = details.HouseholdId,
			FullAddress = details.FullAddress,
			Purpose = request.Purpose,
			PresentedTo = request.PresentedTo,
			IncludeMemberRoster = request.IncludeMemberRoster,
			FilePath = text
		}, "Household certificate generated.");
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
			AppLogger.LogWarning("Unable to open generated household certificate automatically.", ex);
		}
	}

	private static string LoadConfig(string key, string defaultValue)
	{
		SystemConfigService.EnsureTable();
		return SystemConfigService.Get(key, defaultValue);
	}

	private static string BuildOutputPath(HouseholdDetailsDto details)
	{
		string path = MakeSafeFileName(SystemConfigService.GetSystemName());
		string path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path, "Household Certificates");
		string value = MakeSafeFileName(string.IsNullOrWhiteSpace(details.FullAddress) ? $"Household_{details.HouseholdId}" : details.FullAddress);
		string path3 = $"{value}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		return Path.Combine(path2, path3);
	}

	private static string MakeSafeFileName(string value)
	{
		StringBuilder stringBuilder = new StringBuilder(value.Length);
		foreach (char c in value)
		{
			stringBuilder.Append(Enumerable.Contains(Path.GetInvalidFileNameChars(), c) ? '_' : c);
		}
		string text = stringBuilder.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "Household_Certificate";
	}

	private static List<CertificateMemberRow> LoadMembers(int householdId, int barangayId)
	{
		TryEnsureResidentSchemaCompatibility();
		try
		{
			return LoadMembersInternal(householdId, barangayId, includeHeadOfFamily: true);
		}
		catch (Exception ex) when (IsMissingColumn(ex, "is_head_of_family"))
		{
			AppLogger.LogWarning("resident.is_head_of_family was not available while loading household certificate members. Retrying with compatibility query.", ex);
			return LoadMembersInternal(householdId, barangayId, includeHeadOfFamily: false);
		}
	}

	private static List<CertificateMemberRow> LoadMembersInternal(int householdId, int barangayId, bool includeHeadOfFamily)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		List<CertificateMemberRow> list = new List<CertificateMemberRow>();
		string text = (includeHeadOfFamily ? "IFNULL(is_head_of_family, 0) AS is_head_of_family" : "0 AS is_head_of_family");
		string text2 = (includeHeadOfFamily ? "is_head_of_family DESC, " : string.Empty);
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT CONCAT_WS(' ', first_name, middle_name, last_name) AS full_name,\n                     CASE\n                       WHEN birth_date IS NULL THEN NULL\n                       ELSE TIMESTAMPDIFF(YEAR, birth_date, CURDATE())\n                     END AS age,\n                     COALESCE(sex, '') AS sex,\n                     COALESCE(status, 'ACTIVE') AS status,\n                     " + text + "\n              FROM resident\n              WHERE household_id = @householdId\n                AND barangay_id = @barangayId\n                AND IFNULL(is_deleted, 0) = 0\n              ORDER BY " + text2 + "last_name, first_name, middle_name", connection);
			try
			{
				val.Parameters.AddWithValue("@householdId", (object)householdId);
				val.Parameters.AddWithValue("@barangayId", (object)barangayId);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						string value = Convert.ToString(((DbDataReader)(object)val2)["sex"]) ?? string.Empty;
						list.Add(new CertificateMemberRow
						{
							FullName = (Convert.ToString(((DbDataReader)(object)val2)["full_name"]) ?? "Household Member"),
							AgeLabel = ((((DbDataReader)(object)val2)["age"] == DBNull.Value) ? "-" : (Convert.ToString(((DbDataReader)(object)val2)["age"], CultureInfo.InvariantCulture) ?? "-")),
							SexLabel = NormalizeSex(value),
							StatusLabel = (Convert.ToString(((DbDataReader)(object)val2)["status"]) ?? "ACTIVE"),
							IsHead = (Convert.ToInt32(((DbDataReader)(object)val2)["is_head_of_family"], CultureInfo.InvariantCulture) == 1)
						});
					}
					return list;
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static void TryEnsureResidentSchemaCompatibility()
	{
		try
		{
			SchemaGuard.EnsureDatabaseReady();
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to finish household certificate schema compatibility checks before loading members.", ex);
		}
	}

	private static bool IsMissingColumn(Exception ex, string columnName)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			MySqlException ex3 = (MySqlException)(object)((ex2 is MySqlException) ? ex2 : null);
			if (ex3 != null && ex3.Number == 1054)
			{
				return true;
			}
			if ((ex2.Message ?? string.Empty).IndexOf(columnName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static SignatoryInfo LoadPrimarySignatory(int barangayId)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				MySqlCommand val = new MySqlCommand("SELECT CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,\n                         COALESCE(bo.position, '') AS position\n                  FROM barangay_official bo\n                  INNER JOIN official_term ot ON ot.term_id = bo.term_id\n                  INNER JOIN resident r ON r.resident_id = bo.resident_id\n                  WHERE ot.barangay_id = @barangayId\n                    AND UPPER(COALESCE(bo.status, 'ACTIVE')) = 'ACTIVE'\n                  ORDER BY\n                    CASE\n                      WHEN UPPER(bo.position) LIKE '%PUNONG%' THEN 0\n                      WHEN UPPER(bo.position) LIKE '%CAPTAIN%' THEN 1\n                      WHEN UPPER(bo.position) LIKE '%CHAIR%' THEN 2\n                      ELSE 9\n                    END,\n                    bo.position,\n                    r.last_name,\n                    r.first_name\n                  LIMIT 1", connection);
				try
				{
					val.Parameters.AddWithValue("@barangayId", (object)barangayId);
					MySqlDataReader val2 = val.ExecuteReader();
					try
					{
						if (((DbDataReader)(object)val2).Read())
						{
							string text = Convert.ToString(((DbDataReader)(object)val2)["full_name"]) ?? string.Empty;
							string text2 = Convert.ToString(((DbDataReader)(object)val2)["position"]) ?? string.Empty;
							if (!string.IsNullOrWhiteSpace(text))
							{
								return new SignatoryInfo
								{
									Name = text,
									Position = (string.IsNullOrWhiteSpace(text2) ? "Barangay Official" : text2)
								};
							}
						}
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to load primary barangay signatory for household certificate.", ex);
		}
		return new SignatoryInfo
		{
			Name = (string.IsNullOrWhiteSpace(UserSession.Username) ? "Barangay Staff" : UserSession.Username),
			Position = "Issuing Officer"
		};
	}

	private static string NormalizeSex(string value)
	{
		if (string.Equals(value, "M", StringComparison.OrdinalIgnoreCase))
		{
			return "Male";
		}
		if (string.Equals(value, "F", StringComparison.OrdinalIgnoreCase))
		{
			return "Female";
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "-";
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
