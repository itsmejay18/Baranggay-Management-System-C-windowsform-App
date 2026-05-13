using System;
using System.Collections.Generic;
using System.Data;
using baranggaysystem1.Database;

namespace baranggaysystem1;

/// <summary>
/// Service for performing global searches across multiple entity types.
/// </summary>
public static class GlobalSearchService
{
    /// <summary>
    /// Searches residents, certificates, blotter, and users based on scope.
    /// </summary>
    public static List<GlobalSearchResult> Search(string query, GlobalSearchScope scope, int maxResults = 50)
    {
        var results = new List<GlobalSearchResult>();

        if (string.IsNullOrWhiteSpace(query))
        {
            return results;
        }

        string likeParam = $"%{query.Trim()}%";

        if (scope == GlobalSearchScope.All || scope == GlobalSearchScope.Residents)
        {
            var table = DbHelper.LoadTable(
                @"SELECT resident_id, CONCAT(first_name, ' ', last_name) AS full_name,
                         COALESCE(contact_number, '') AS contact
                  FROM resident
                  WHERE IFNULL(is_deleted, 0) = 0
                    AND (CONCAT(first_name, ' ', last_name) LIKE @q OR contact_number LIKE @q)
                  LIMIT @limit",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@q", likeParam);
                    cmd.Parameters.AddWithValue("@limit", maxResults);
                });

            foreach (DataRow row in table.Rows)
            {
                results.Add(new GlobalSearchResult
                {
                    Id = Convert.ToInt32(row["resident_id"]),
                    EntityType = GlobalSearchEntityType.Resident,
                    Title = row["full_name"]?.ToString() ?? string.Empty,
                    Subtitle = row["contact"]?.ToString() ?? string.Empty,
                    ResidentId = Convert.ToInt32(row["resident_id"])
                });
            }
        }

        if (scope == GlobalSearchScope.All || scope == GlobalSearchScope.Certificates)
        {
            var table = DbHelper.LoadTable(
                @"SELECT c.certificate_id, c.certificate_type, c.status,
                         CONCAT(r.first_name, ' ', r.last_name) AS resident_name, c.resident_id
                  FROM certificate c
                  INNER JOIN resident r ON r.resident_id = c.resident_id
                  WHERE (c.certificate_type LIKE @q OR CONCAT(r.first_name, ' ', r.last_name) LIKE @q)
                  LIMIT @limit",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@q", likeParam);
                    cmd.Parameters.AddWithValue("@limit", maxResults);
                });

            foreach (DataRow row in table.Rows)
            {
                results.Add(new GlobalSearchResult
                {
                    Id = Convert.ToInt32(row["certificate_id"]),
                    EntityType = GlobalSearchEntityType.Certificate,
                    Title = row["certificate_type"]?.ToString() ?? string.Empty,
                    Subtitle = row["resident_name"]?.ToString() ?? string.Empty,
                    ResidentId = row["resident_id"] != DBNull.Value ? Convert.ToInt32(row["resident_id"]) : null
                });
            }
        }

        if (scope == GlobalSearchScope.All || scope == GlobalSearchScope.Blotter)
        {
            var table = DbHelper.LoadTable(
                @"SELECT cr.case_id, cr.incident_type, cr.status,
                         cr.incident_details, cr.complainant_id
                  FROM case_record cr
                  WHERE (cr.incident_type LIKE @q OR cr.incident_details LIKE @q)
                  LIMIT @limit",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@q", likeParam);
                    cmd.Parameters.AddWithValue("@limit", maxResults);
                });

            foreach (DataRow row in table.Rows)
            {
                results.Add(new GlobalSearchResult
                {
                    Id = Convert.ToInt32(row["case_id"]),
                    EntityType = GlobalSearchEntityType.Blotter,
                    Title = row["incident_type"]?.ToString() ?? string.Empty,
                    Subtitle = row["status"]?.ToString() ?? string.Empty,
                    ResidentId = row["complainant_id"] != DBNull.Value ? Convert.ToInt32(row["complainant_id"]) : null
                });
            }
        }

        if (scope == GlobalSearchScope.All || scope == GlobalSearchScope.Users)
        {
            var table = DbHelper.LoadTable(
                @"SELECT user_id, username, COALESCE(display_name, '') AS display_name
                  FROM users
                  WHERE (username LIKE @q OR display_name LIKE @q)
                  LIMIT @limit",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@q", likeParam);
                    cmd.Parameters.AddWithValue("@limit", maxResults);
                });

            foreach (DataRow row in table.Rows)
            {
                results.Add(new GlobalSearchResult
                {
                    Id = Convert.ToInt32(row["user_id"]),
                    EntityType = GlobalSearchEntityType.User,
                    Title = row["username"]?.ToString() ?? string.Empty,
                    Subtitle = row["display_name"]?.ToString() ?? string.Empty,
                    ResidentId = null
                });
            }
        }

        // Trim to maxResults total
        if (results.Count > maxResults)
        {
            results.RemoveRange(maxResults, results.Count - maxResults);
        }

        return results;
    }
}
