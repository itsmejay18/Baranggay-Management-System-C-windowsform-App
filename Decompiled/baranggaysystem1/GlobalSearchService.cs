using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class GlobalSearchService
{
	internal static List<GlobalSearchResult> Search(string query, GlobalSearchScope scope, int limitPerType = 12)
	{
		query = (query ?? string.Empty).Trim();
		if (query.Length < 2)
		{
			return new List<GlobalSearchResult>();
		}
		int num = Math.Clamp(limitPerType, 3, 40);
		string like = "%" + query + "%";
		int result;
		bool flag = int.TryParse(query, out result);
		List<GlobalSearchResult> list = new List<GlobalSearchResult>(num * 4);
		if ((uint)scope <= 1u)
		{
			list.AddRange(SearchResidents(like, flag ? new int?(result) : ((int?)null), num));
		}
		if ((scope == GlobalSearchScope.All || scope == GlobalSearchScope.Certificates) ? true : false)
		{
			list.AddRange(SearchCertificates(like, flag ? new int?(result) : ((int?)null), num));
		}
		if ((scope == GlobalSearchScope.All || scope == GlobalSearchScope.Blotter) ? true : false)
		{
			list.AddRange(SearchBlotter(like, flag ? new int?(result) : ((int?)null), num));
		}
		bool flag2 = ((scope == GlobalSearchScope.All || scope == GlobalSearchScope.Users) ? true : false);
		if (flag2 && Permissions.CanManageUsers)
		{
			list.AddRange(SearchUsers(like, flag ? new int?(result) : ((int?)null), num));
		}
		return list;
	}

	private static IEnumerable<GlobalSearchResult> SearchResidents(string like, int? idValue, int limit)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("SELECT resident_id, first_name, middle_name, last_name, contact_no, status");
		stringBuilder.AppendLine("FROM resident");
		stringBuilder.AppendLine("WHERE IFNULL(is_deleted,0)=0");
		stringBuilder.AppendLine("  AND (");
		stringBuilder.AppendLine("        CONCAT_WS(' ', first_name, middle_name, last_name) LIKE @q");
		stringBuilder.AppendLine("     OR first_name LIKE @q");
		stringBuilder.AppendLine("     OR middle_name LIKE @q");
		stringBuilder.AppendLine("     OR last_name LIKE @q");
		stringBuilder.AppendLine("     OR contact_no LIKE @q");
		if (idValue.HasValue)
		{
			stringBuilder.AppendLine("     OR resident_id = @id");
		}
		stringBuilder.AppendLine("  )");
		stringBuilder.AppendLine("ORDER BY last_name, first_name");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler.AppendLiteral("LIMIT ");
		handler.AppendFormatted(limit);
		stringBuilder2.AppendLine(ref handler);
		DataTable dataTable = DbHelper.LoadTable(stringBuilder.ToString(), delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@q", (object)like);
			if (idValue.HasValue)
			{
				cmd.Parameters.AddWithValue("@id", (object)idValue.Value);
			}
		});
		foreach (DataRow row in dataTable.Rows)
		{
			int num = ReadInt(row, "resident_id");
			if (num > 0)
			{
				string text = JoinNonEmpty(Convert.ToString(row["first_name"]), Convert.ToString(row["middle_name"]), Convert.ToString(row["last_name"]));
				string text2 = Convert.ToString(row["contact_no"]) ?? string.Empty;
				string text3 = Convert.ToString(row["status"]) ?? string.Empty;
				string subtitle = (string.IsNullOrWhiteSpace(text2) ? ("Status: " + text3) : ("Contact: " + text2 + " | Status: " + text3));
				yield return new GlobalSearchResult(GlobalSearchEntityType.Resident, num, string.IsNullOrWhiteSpace(text) ? $"Resident #{num}" : text, subtitle, num);
			}
		}
	}

	private static IEnumerable<GlobalSearchResult> SearchCertificates(string like, int? idValue, int limit)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("SELECT dr.doc_request_id AS certificate_id, dr.resident_id, dr.document_no, dr.purpose, dr.status, dr.requested_at,");
		stringBuilder.AppendLine("       dt.name AS certificate_type, r.first_name, r.middle_name, r.last_name");
		stringBuilder.AppendLine("FROM document_request dr");
		stringBuilder.AppendLine("LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id");
		stringBuilder.AppendLine("LEFT JOIN resident r ON r.resident_id = dr.resident_id");
		stringBuilder.AppendLine("WHERE IFNULL(r.is_deleted,0)=0");
		stringBuilder.AppendLine("  AND (");
		stringBuilder.AppendLine("        dr.document_no LIKE @q");
		stringBuilder.AppendLine("     OR dr.verification_token LIKE @q");
		stringBuilder.AppendLine("     OR dr.purpose LIKE @q");
		stringBuilder.AppendLine("     OR dr.status LIKE @q");
		stringBuilder.AppendLine("     OR dt.name LIKE @q");
		stringBuilder.AppendLine("     OR CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @q");
		if (idValue.HasValue)
		{
			stringBuilder.AppendLine("     OR dr.doc_request_id = @id");
		}
		stringBuilder.AppendLine("  )");
		stringBuilder.AppendLine("ORDER BY dr.requested_at DESC, dr.doc_request_id DESC");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler.AppendLiteral("LIMIT ");
		handler.AppendFormatted(limit);
		stringBuilder2.AppendLine(ref handler);
		DataTable dataTable = DbHelper.LoadTable(stringBuilder.ToString(), delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@q", (object)like);
			if (idValue.HasValue)
			{
				cmd.Parameters.AddWithValue("@id", (object)idValue.Value);
			}
		});
		foreach (DataRow row in dataTable.Rows)
		{
			int num = ReadInt(row, "certificate_id");
			int num2 = ReadInt(row, "resident_id");
			if (num > 0 && num2 > 0)
			{
				string text = Convert.ToString(row["document_no"]) ?? string.Empty;
				string text2 = Convert.ToString(row["certificate_type"]) ?? "Certificate";
				string text3 = Convert.ToString(row["status"]) ?? string.Empty;
				DateTime? dateTime = ReadDateTime(row, "requested_at");
				string text4 = JoinNonEmpty(Convert.ToString(row["first_name"]), Convert.ToString(row["middle_name"]), Convert.ToString(row["last_name"]));
				string text5 = text2 + " " + (string.IsNullOrWhiteSpace(text) ? $"#{num}" : text);
				if (!string.IsNullOrWhiteSpace(text4))
				{
					text5 = text5 + " | " + text4;
				}
				string text6 = (string.IsNullOrWhiteSpace(text3) ? "Certificate request" : ("Status: " + text3));
				if (dateTime.HasValue)
				{
					text6 += $" | Requested: {dateTime.Value:MMM dd, yyyy}";
				}
				yield return new GlobalSearchResult(GlobalSearchEntityType.Certificate, num, text5, text6, num2);
			}
		}
	}

	private static IEnumerable<GlobalSearchResult> SearchBlotter(string like, int? idValue, int limit)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("SELECT cr.case_id AS blotter_id, cr.complainant_id AS resident_id, cr.case_no, cr.respondent_name, cr.incident_type,");
		stringBuilder.AppendLine("       cr.incident_date, cr.status, r.first_name, r.middle_name, r.last_name");
		stringBuilder.AppendLine("FROM case_record cr");
		stringBuilder.AppendLine("LEFT JOIN resident r ON r.resident_id = cr.complainant_id");
		stringBuilder.AppendLine("WHERE cr.complainant_id IS NOT NULL");
		stringBuilder.AppendLine("  AND IFNULL(r.is_deleted,0)=0");
		stringBuilder.AppendLine("  AND (");
		stringBuilder.AppendLine("        cr.case_no LIKE @q");
		stringBuilder.AppendLine("     OR cr.respondent_name LIKE @q");
		stringBuilder.AppendLine("     OR cr.incident_type LIKE @q");
		stringBuilder.AppendLine("     OR cr.incident_location LIKE @q");
		stringBuilder.AppendLine("     OR cr.summary LIKE @q");
		stringBuilder.AppendLine("     OR cr.incident_details LIKE @q");
		stringBuilder.AppendLine("     OR CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @q");
		if (idValue.HasValue)
		{
			stringBuilder.AppendLine("     OR cr.case_id = @id");
		}
		stringBuilder.AppendLine("  )");
		stringBuilder.AppendLine("ORDER BY COALESCE(cr.incident_date, cr.date_filed) DESC, cr.case_id DESC");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler.AppendLiteral("LIMIT ");
		handler.AppendFormatted(limit);
		stringBuilder2.AppendLine(ref handler);
		DataTable dataTable = DbHelper.LoadTable(stringBuilder.ToString(), delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@q", (object)like);
			if (idValue.HasValue)
			{
				cmd.Parameters.AddWithValue("@id", (object)idValue.Value);
			}
		});
		foreach (DataRow row in dataTable.Rows)
		{
			int num = ReadInt(row, "blotter_id");
			int num2 = ReadInt(row, "resident_id");
			if (num > 0 && num2 > 0)
			{
				string text = Convert.ToString(row["case_no"]) ?? string.Empty;
				string text2 = Convert.ToString(row["respondent_name"]) ?? string.Empty;
				string text3 = Convert.ToString(row["incident_type"]) ?? "Blotter";
				string text4 = Convert.ToString(row["status"]) ?? string.Empty;
				DateTime? dateTime = ReadDateTime(row, "incident_date");
				string text5 = JoinNonEmpty(Convert.ToString(row["first_name"]), Convert.ToString(row["middle_name"]), Convert.ToString(row["last_name"]));
				string text6 = text3 + " | " + (string.IsNullOrWhiteSpace(text2) ? $"Case #{num}" : text2);
				if (!string.IsNullOrWhiteSpace(text))
				{
					text6 = text + " | " + text6;
				}
				List<string> list = new List<string>();
				if (!string.IsNullOrWhiteSpace(text5))
				{
					list.Add("Complainant: " + text5);
				}
				if (dateTime.HasValue)
				{
					list.Add("Incident: " + dateTime.Value.ToString("MMM dd, yyyy"));
				}
				if (!string.IsNullOrWhiteSpace(text4))
				{
					list.Add("Status: " + text4);
				}
				yield return new GlobalSearchResult(GlobalSearchEntityType.Blotter, num, text6, (list.Count == 0) ? "Blotter case" : string.Join(" | ", list), num2);
			}
		}
	}

	private static IEnumerable<GlobalSearchResult> SearchUsers(string like, int? idValue, int limit)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("SELECT ua.user_id, ua.username, ua.first_name, ua.middle_name, ua.last_name, ua.is_active,");
		stringBuilder.AppendLine("       COALESCE(r.name, 'Staff') AS role");
		stringBuilder.AppendLine("FROM user_account ua");
		stringBuilder.AppendLine("LEFT JOIN user_role ur ON ur.user_id = ua.user_id");
		stringBuilder.AppendLine("LEFT JOIN role r ON r.role_id = ur.role_id");
		stringBuilder.AppendLine("WHERE (");
		stringBuilder.AppendLine("       ua.username LIKE @q");
		stringBuilder.AppendLine("    OR ua.first_name LIKE @q");
		stringBuilder.AppendLine("    OR ua.middle_name LIKE @q");
		stringBuilder.AppendLine("    OR ua.last_name LIKE @q");
		stringBuilder.AppendLine("    OR ua.email LIKE @q");
		stringBuilder.AppendLine("    OR ua.contact_no LIKE @q");
		stringBuilder.AppendLine("    OR CONCAT_WS(' ', ua.first_name, ua.middle_name, ua.last_name) LIKE @q");
		if (idValue.HasValue)
		{
			stringBuilder.AppendLine("    OR ua.user_id = @id");
		}
		stringBuilder.AppendLine(")");
		stringBuilder.AppendLine("ORDER BY ua.username");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler.AppendLiteral("LIMIT ");
		handler.AppendFormatted(limit);
		stringBuilder2.AppendLine(ref handler);
		DataTable dataTable = DbHelper.LoadTable(stringBuilder.ToString(), delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@q", (object)like);
			if (idValue.HasValue)
			{
				cmd.Parameters.AddWithValue("@id", (object)idValue.Value);
			}
		});
		foreach (DataRow row in dataTable.Rows)
		{
			int num = ReadInt(row, "user_id");
			if (num > 0)
			{
				string text = Convert.ToString(row["username"]) ?? string.Empty;
				string text2 = JoinNonEmpty(Convert.ToString(row["first_name"]), Convert.ToString(row["middle_name"]), Convert.ToString(row["last_name"]));
				string? obj = Convert.ToString(row["role"]) ?? "Staff";
				bool flag = ReadInt(row, "is_active") == 1;
				string title = ((!string.IsNullOrWhiteSpace(text2)) ? (string.IsNullOrWhiteSpace(text) ? text2 : (text2 + " (" + text + ")")) : (string.IsNullOrWhiteSpace(text) ? $"User #{num}" : text));
				string subtitle = obj + " | " + (flag ? "Active" : "Inactive");
				yield return new GlobalSearchResult(GlobalSearchEntityType.User, num, title, subtitle);
			}
		}
	}

	private static int ReadInt(DataRow row, string column)
	{
		if (!row.Table.Columns.Contains(column))
		{
			return 0;
		}
		object obj = row[column];
		if (obj == null || obj == DBNull.Value)
		{
			return 0;
		}
		if (!int.TryParse(Convert.ToString(obj), out var result))
		{
			return Convert.ToInt32(obj);
		}
		return result;
	}

	private static DateTime? ReadDateTime(DataRow row, string column)
	{
		if (!row.Table.Columns.Contains(column))
		{
			return null;
		}
		object obj = row[column];
		if (obj == null || obj == DBNull.Value)
		{
			return null;
		}
		if (obj is DateTime)
		{
			return (DateTime)obj;
		}
		if (!DateTime.TryParse(Convert.ToString(obj), out var result))
		{
			return null;
		}
		return result;
	}

	private static string JoinNonEmpty(params string?[] parts)
	{
		return string.Join(" ", from p in parts
			where !string.IsNullOrWhiteSpace(p)
			select p.Trim());
	}
}
