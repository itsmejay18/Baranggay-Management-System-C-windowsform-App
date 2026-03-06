using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal enum GlobalSearchScope
{
    All,
    Residents,
    Certificates,
    Blotter,
    Users
}

internal enum GlobalSearchEntityType
{
    Resident,
    Certificate,
    Blotter,
    User
}

internal sealed record GlobalSearchResult(
    GlobalSearchEntityType EntityType,
    int Id,
    string Title,
    string Subtitle,
    int? ResidentId = null);

internal static class GlobalSearchService
{
    internal static List<GlobalSearchResult> Search(string query, GlobalSearchScope scope, int limitPerType = 12)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length < 2)
        {
            return new List<GlobalSearchResult>();
        }

        int limit = Math.Clamp(limitPerType, 3, 40);
        string like = "%" + query + "%";
        bool hasId = int.TryParse(query, out int idValue);

        var results = new List<GlobalSearchResult>(limit * 4);

        if (scope is GlobalSearchScope.All or GlobalSearchScope.Residents)
        {
            results.AddRange(SearchResidents(like, hasId ? idValue : (int?)null, limit));
        }

        if (scope is GlobalSearchScope.All or GlobalSearchScope.Certificates)
        {
            results.AddRange(SearchCertificates(like, hasId ? idValue : (int?)null, limit));
        }

        if (scope is GlobalSearchScope.All or GlobalSearchScope.Blotter)
        {
            results.AddRange(SearchBlotter(like, hasId ? idValue : (int?)null, limit));
        }

        if (scope is GlobalSearchScope.All or GlobalSearchScope.Users)
        {
            if (Permissions.CanManageUsers)
            {
                results.AddRange(SearchUsers(like, hasId ? idValue : (int?)null, limit));
            }
        }

        // Keep a stable-ish ordering without spending too much effort on scoring.
        // Newest transactional items are already ordered by date in their queries.
        return results;
    }

    private static IEnumerable<GlobalSearchResult> SearchResidents(string like, int? idValue, int limit)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT resident_id, first_name, middle_name, last_name, contact_no, status");
        sql.AppendLine("FROM resident");
        sql.AppendLine("WHERE IFNULL(is_deleted,0)=0");
        sql.AppendLine("  AND (");
        sql.AppendLine("        CONCAT_WS(' ', first_name, middle_name, last_name) LIKE @q");
        sql.AppendLine("     OR first_name LIKE @q");
        sql.AppendLine("     OR middle_name LIKE @q");
        sql.AppendLine("     OR last_name LIKE @q");
        sql.AppendLine("     OR contact_no LIKE @q");
        if (idValue.HasValue)
        {
            sql.AppendLine("     OR resident_id = @id");
        }
        sql.AppendLine("  )");
        sql.AppendLine("ORDER BY last_name, first_name");
        sql.AppendLine($"LIMIT {limit}");

        DataTable table = DbHelper.LoadTable(sql.ToString(), cmd =>
        {
            cmd.Parameters.AddWithValue("@q", like);
            if (idValue.HasValue)
            {
                cmd.Parameters.AddWithValue("@id", idValue.Value);
            }
        });

        foreach (DataRow row in table.Rows)
        {
            int residentId = ReadInt(row, "resident_id");
            if (residentId <= 0) continue;

            string name = JoinNonEmpty(
                Convert.ToString(row["first_name"]),
                Convert.ToString(row["middle_name"]),
                Convert.ToString(row["last_name"]));
            string contact = Convert.ToString(row["contact_no"]) ?? string.Empty;
            string status = Convert.ToString(row["status"]) ?? string.Empty;

            string subtitle = string.IsNullOrWhiteSpace(contact)
                ? $"Status: {status}"
                : $"Contact: {contact} | Status: {status}";

            yield return new GlobalSearchResult(
                GlobalSearchEntityType.Resident,
                residentId,
                string.IsNullOrWhiteSpace(name) ? $"Resident #{residentId}" : name,
                subtitle,
                residentId);
        }
    }

    private static IEnumerable<GlobalSearchResult> SearchCertificates(string like, int? idValue, int limit)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT dr.doc_request_id AS certificate_id, dr.resident_id, dr.document_no, dr.purpose, dr.status, dr.requested_at,");
        sql.AppendLine("       dt.name AS certificate_type, r.first_name, r.middle_name, r.last_name");
        sql.AppendLine("FROM document_request dr");
        sql.AppendLine("LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id");
        sql.AppendLine("LEFT JOIN resident r ON r.resident_id = dr.resident_id");
        sql.AppendLine("WHERE IFNULL(r.is_deleted,0)=0");
        sql.AppendLine("  AND (");
        sql.AppendLine("        dr.document_no LIKE @q");
        sql.AppendLine("     OR dr.verification_token LIKE @q");
        sql.AppendLine("     OR dr.purpose LIKE @q");
        sql.AppendLine("     OR dr.status LIKE @q");
        sql.AppendLine("     OR dt.name LIKE @q");
        sql.AppendLine("     OR CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @q");
        if (idValue.HasValue)
        {
            sql.AppendLine("     OR dr.doc_request_id = @id");
        }
        sql.AppendLine("  )");
        sql.AppendLine("ORDER BY dr.requested_at DESC, dr.doc_request_id DESC");
        sql.AppendLine($"LIMIT {limit}");

        DataTable table = DbHelper.LoadTable(sql.ToString(), cmd =>
        {
            cmd.Parameters.AddWithValue("@q", like);
            if (idValue.HasValue)
            {
                cmd.Parameters.AddWithValue("@id", idValue.Value);
            }
        });

        foreach (DataRow row in table.Rows)
        {
            int certId = ReadInt(row, "certificate_id");
            int residentId = ReadInt(row, "resident_id");
            if (certId <= 0 || residentId <= 0) continue;

            string docNo = Convert.ToString(row["document_no"]) ?? string.Empty;
            string type = Convert.ToString(row["certificate_type"]) ?? "Certificate";
            string status = Convert.ToString(row["status"]) ?? string.Empty;
            DateTime? requestedAt = ReadDateTime(row, "requested_at");
            string residentName = JoinNonEmpty(
                Convert.ToString(row["first_name"]),
                Convert.ToString(row["middle_name"]),
                Convert.ToString(row["last_name"]));

            string title = $"{type} {(string.IsNullOrWhiteSpace(docNo) ? $"#{certId}" : docNo)}";
            if (!string.IsNullOrWhiteSpace(residentName))
            {
                title += $" | {residentName}";
            }

            string subtitle = string.IsNullOrWhiteSpace(status) ? "Certificate request" : $"Status: {status}";
            if (requestedAt.HasValue)
            {
                subtitle += $" | Requested: {requestedAt.Value:MMM dd, yyyy}";
            }

            yield return new GlobalSearchResult(
                GlobalSearchEntityType.Certificate,
                certId,
                title,
                subtitle,
                residentId);
        }
    }

    private static IEnumerable<GlobalSearchResult> SearchBlotter(string like, int? idValue, int limit)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT cr.case_id AS blotter_id, cr.complainant_id AS resident_id, cr.case_no, cr.respondent_name, cr.incident_type,");
        sql.AppendLine("       cr.incident_date, cr.status, r.first_name, r.middle_name, r.last_name");
        sql.AppendLine("FROM case_record cr");
        sql.AppendLine("LEFT JOIN resident r ON r.resident_id = cr.complainant_id");
        sql.AppendLine("WHERE cr.complainant_id IS NOT NULL");
        sql.AppendLine("  AND IFNULL(r.is_deleted,0)=0");
        sql.AppendLine("  AND (");
        sql.AppendLine("        cr.case_no LIKE @q");
        sql.AppendLine("     OR cr.respondent_name LIKE @q");
        sql.AppendLine("     OR cr.incident_type LIKE @q");
        sql.AppendLine("     OR cr.incident_location LIKE @q");
        sql.AppendLine("     OR cr.summary LIKE @q");
        sql.AppendLine("     OR cr.incident_details LIKE @q");
        sql.AppendLine("     OR CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @q");
        if (idValue.HasValue)
        {
            sql.AppendLine("     OR cr.case_id = @id");
        }
        sql.AppendLine("  )");
        sql.AppendLine("ORDER BY COALESCE(cr.incident_date, cr.date_filed) DESC, cr.case_id DESC");
        sql.AppendLine($"LIMIT {limit}");

        DataTable table = DbHelper.LoadTable(sql.ToString(), cmd =>
        {
            cmd.Parameters.AddWithValue("@q", like);
            if (idValue.HasValue)
            {
                cmd.Parameters.AddWithValue("@id", idValue.Value);
            }
        });

        foreach (DataRow row in table.Rows)
        {
            int blotterId = ReadInt(row, "blotter_id");
            int residentId = ReadInt(row, "resident_id");
            if (blotterId <= 0 || residentId <= 0) continue;

            string caseNo = Convert.ToString(row["case_no"]) ?? string.Empty;
            string respondent = Convert.ToString(row["respondent_name"]) ?? string.Empty;
            string incidentType = Convert.ToString(row["incident_type"]) ?? "Blotter";
            string status = Convert.ToString(row["status"]) ?? string.Empty;
            DateTime? incidentDate = ReadDateTime(row, "incident_date");
            string residentName = JoinNonEmpty(
                Convert.ToString(row["first_name"]),
                Convert.ToString(row["middle_name"]),
                Convert.ToString(row["last_name"]));

            string title = $"{incidentType} | {(string.IsNullOrWhiteSpace(respondent) ? $"Case #{blotterId}" : respondent)}";
            if (!string.IsNullOrWhiteSpace(caseNo))
            {
                title = $"{caseNo} | {title}";
            }

            var subtitleParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(residentName))
            {
                subtitleParts.Add("Complainant: " + residentName);
            }
            if (incidentDate.HasValue)
            {
                subtitleParts.Add("Incident: " + incidentDate.Value.ToString("MMM dd, yyyy"));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                subtitleParts.Add("Status: " + status);
            }

            yield return new GlobalSearchResult(
                GlobalSearchEntityType.Blotter,
                blotterId,
                title,
                subtitleParts.Count == 0 ? "Blotter case" : string.Join(" | ", subtitleParts),
                residentId);
        }
    }

    private static IEnumerable<GlobalSearchResult> SearchUsers(string like, int? idValue, int limit)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT ua.user_id, ua.username, ua.first_name, ua.middle_name, ua.last_name, ua.is_active,");
        sql.AppendLine("       COALESCE(r.name, 'Staff') AS role");
        sql.AppendLine("FROM user_account ua");
        sql.AppendLine("LEFT JOIN user_role ur ON ur.user_id = ua.user_id");
        sql.AppendLine("LEFT JOIN role r ON r.role_id = ur.role_id");
        sql.AppendLine("WHERE (");
        sql.AppendLine("       ua.username LIKE @q");
        sql.AppendLine("    OR ua.first_name LIKE @q");
        sql.AppendLine("    OR ua.middle_name LIKE @q");
        sql.AppendLine("    OR ua.last_name LIKE @q");
        sql.AppendLine("    OR ua.email LIKE @q");
        sql.AppendLine("    OR ua.contact_no LIKE @q");
        sql.AppendLine("    OR CONCAT_WS(' ', ua.first_name, ua.middle_name, ua.last_name) LIKE @q");
        if (idValue.HasValue)
        {
            sql.AppendLine("    OR ua.user_id = @id");
        }
        sql.AppendLine(")");
        sql.AppendLine("ORDER BY ua.username");
        sql.AppendLine($"LIMIT {limit}");

        DataTable table = DbHelper.LoadTable(sql.ToString(), cmd =>
        {
            cmd.Parameters.AddWithValue("@q", like);
            if (idValue.HasValue)
            {
                cmd.Parameters.AddWithValue("@id", idValue.Value);
            }
        });

        foreach (DataRow row in table.Rows)
        {
            int userId = ReadInt(row, "user_id");
            if (userId <= 0) continue;

            string username = Convert.ToString(row["username"]) ?? string.Empty;
            string name = JoinNonEmpty(
                Convert.ToString(row["first_name"]),
                Convert.ToString(row["middle_name"]),
                Convert.ToString(row["last_name"]));
            string role = Convert.ToString(row["role"]) ?? "Staff";
            bool isActive = ReadInt(row, "is_active") == 1;

            string title = string.IsNullOrWhiteSpace(name)
                ? string.IsNullOrWhiteSpace(username) ? $"User #{userId}" : username
                : string.IsNullOrWhiteSpace(username) ? name : $"{name} ({username})";

            string subtitle = $"{role} | {(isActive ? "Active" : "Inactive")}";

            yield return new GlobalSearchResult(
                GlobalSearchEntityType.User,
                userId,
                title,
                subtitle);
        }
    }

    private static int ReadInt(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return 0;
        }

        object? value = row[column];
        if (value == null || value == DBNull.Value)
        {
            return 0;
        }

        return int.TryParse(Convert.ToString(value), out int parsed) ? parsed : Convert.ToInt32(value);
    }

    private static DateTime? ReadDateTime(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return null;
        }

        object? value = row[column];
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        if (value is DateTime dt)
        {
            return dt;
        }

        return DateTime.TryParse(Convert.ToString(value), out var parsed) ? parsed : null;
    }

    private static string JoinNonEmpty(params string?[] parts)
    {
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
    }
}
