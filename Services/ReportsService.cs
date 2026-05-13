using System;
using System.Collections.Generic;
using System.Data;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Service for generating reports dashboard data.
/// </summary>
public static class ReportsService
{
    public static ReportsDashboardData LoadDashboard(DateTime from, DateTime to, ReportsFilters filters)
    {
        var summary = LoadSummary(from, to, filters);
        var serviceTimes = LoadServiceTimes(from, to, filters);
        var trends = LoadMonthlyTrends(from, to, filters);
        var staffPerformance = LoadStaffPerformance(from, to, filters);
        var hotspots = LoadHotspots(from, to, filters);

        return new ReportsDashboardData
        {
            Summary = summary,
            ServiceTimes = serviceTimes,
            Trends = trends,
            StaffPerformance = staffPerformance,
            Hotspots = hotspots
        };
    }

    private static ReportsSummary LoadSummary(DateTime from, DateTime to, ReportsFilters filters)
    {
        string purokCondition = filters.PurokId.HasValue ? " AND r.purok_id = @purokId" : "";

        int newResidents = DbHelper.ExecuteScalar<int>(
            $@"SELECT COUNT(*) FROM resident r
               WHERE IFNULL(r.is_deleted,0)=0
                 AND r.created_at BETWEEN @from AND @to{purokCondition}",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
                if (filters.PurokId.HasValue)
                    cmd.Parameters.AddWithValue("@purokId", filters.PurokId.Value);
            });

        string certStatusCondition = GetCertStatusCondition(filters.CertificateStatus);

        int certRequests = DbHelper.ExecuteScalar<int>(
            $@"SELECT COUNT(*) FROM document_request dr
               WHERE dr.requested_at BETWEEN @from AND @to{certStatusCondition}",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
            });

        int certReleased = DbHelper.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM document_request
              WHERE status = 'RELEASED' AND released_at BETWEEN @from AND @to",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
            });

        string blotterStatusCondition = GetBlotterStatusCondition(filters.BlotterStatus);

        int blottersFiled = DbHelper.ExecuteScalar<int>(
            $@"SELECT COUNT(*) FROM case_record cr
               WHERE cr.created_at BETWEEN @from AND @to{blotterStatusCondition}",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
            });

        int pendingCerts = DbHelper.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM document_request WHERE status IN ('SUBMITTED','APPROVED')");

        int activeBlotters = DbHelper.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM case_record WHERE status IN ('OPEN','ONGOING')");

        return new ReportsSummary
        {
            NewResidents = newResidents,
            CertificateRequests = certRequests,
            CertificatesReleased = certReleased,
            BlottersFiled = blottersFiled,
            PendingCertificates = pendingCerts,
            ActiveBlotters = activeBlotters
        };
    }

    private static ServiceTimeMetrics LoadServiceTimes(DateTime from, DateTime to, ReportsFilters filters)
    {
        var table = DbHelper.LoadTable(
            @"SELECT
                AVG(TIMESTAMPDIFF(SECOND, requested_at, approved_at)) AS avg_req_to_approval,
                COUNT(CASE WHEN approved_at IS NOT NULL THEN 1 END) AS approval_samples,
                AVG(TIMESTAMPDIFF(SECOND, approved_at, released_at)) AS avg_approval_to_release,
                COUNT(CASE WHEN released_at IS NOT NULL THEN 1 END) AS release_samples
              FROM document_request
              WHERE requested_at BETWEEN @from AND @to",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
            });

        if (table.Rows.Count == 0)
        {
            return new ServiceTimeMetrics();
        }

        var row = table.Rows[0];
        return new ServiceTimeMetrics
        {
            AvgRequestToApprovalSeconds = row["avg_req_to_approval"] != DBNull.Value ? Convert.ToDouble(row["avg_req_to_approval"]) : 0,
            ApprovalSamples = row["approval_samples"] != DBNull.Value ? Convert.ToInt32(row["approval_samples"]) : 0,
            AvgApprovalToReleaseSeconds = row["avg_approval_to_release"] != DBNull.Value ? Convert.ToDouble(row["avg_approval_to_release"]) : 0,
            ReleaseSamples = row["release_samples"] != DBNull.Value ? Convert.ToInt32(row["release_samples"]) : 0
        };
    }

    private static IReadOnlyList<MonthlyTrendRow> LoadMonthlyTrends(DateTime from, DateTime to, ReportsFilters filters)
    {
        var table = DbHelper.LoadTable(
            @"SELECT
                DATE_FORMAT(d.month_date, '%b %Y') AS month_label,
                COALESCE(res.cnt, 0) AS residents,
                COALESCE(cert.cnt, 0) AS certificates,
                COALESCE(blot.cnt, 0) AS blotters
              FROM (
                SELECT DISTINCT DATE_FORMAT(requested_at, '%Y-%m-01') AS month_date
                FROM document_request WHERE requested_at BETWEEN @from AND @to
                UNION
                SELECT DISTINCT DATE_FORMAT(created_at, '%Y-%m-01')
                FROM resident WHERE created_at BETWEEN @from AND @to
                UNION
                SELECT DISTINCT DATE_FORMAT(created_at, '%Y-%m-01')
                FROM case_record WHERE created_at BETWEEN @from AND @to
              ) d
              LEFT JOIN (SELECT DATE_FORMAT(created_at, '%Y-%m-01') AS m, COUNT(*) AS cnt FROM resident WHERE IFNULL(is_deleted,0)=0 AND created_at BETWEEN @from AND @to GROUP BY m) res ON res.m = d.month_date
              LEFT JOIN (SELECT DATE_FORMAT(requested_at, '%Y-%m-01') AS m, COUNT(*) AS cnt FROM document_request WHERE requested_at BETWEEN @from AND @to GROUP BY m) cert ON cert.m = d.month_date
              LEFT JOIN (SELECT DATE_FORMAT(created_at, '%Y-%m-01') AS m, COUNT(*) AS cnt FROM case_record WHERE created_at BETWEEN @from AND @to GROUP BY m) blot ON blot.m = d.month_date
              ORDER BY d.month_date ASC",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
            });

        var trends = new List<MonthlyTrendRow>();
        foreach (DataRow row in table.Rows)
        {
            trends.Add(new MonthlyTrendRow
            {
                MonthLabel = row["month_label"]?.ToString() ?? string.Empty,
                Residents = Convert.ToInt32(row["residents"]),
                Certificates = Convert.ToInt32(row["certificates"]),
                Blotters = Convert.ToInt32(row["blotters"])
            });
        }

        return trends;
    }

    private static IReadOnlyList<StaffPerformanceRow> LoadStaffPerformance(DateTime from, DateTime to, ReportsFilters filters)
    {
        var table = DbHelper.LoadTable(
            @"SELECT u.user_id, u.username, u.display_name, u.is_active,
                     COALESCE(appr.cnt, 0) AS approvals_completed,
                     COALESCE(appr_overdue.cnt, 0) AS approvals_overdue,
                     COALESCE(appr.avg_seconds, 0) AS avg_req_to_approval,
                     COALESCE(rel.cnt, 0) AS releases_completed,
                     COALESCE(rel_overdue.cnt, 0) AS releases_overdue,
                     COALESCE(rel.avg_seconds, 0) AS avg_approval_to_release,
                     COALESCE(blot_changes.cnt, 0) AS blotter_status_changes,
                     COALESCE(blot_res.cnt, 0) AS blotter_resolutions,
                     COALESCE(blot_res_overdue.cnt, 0) AS blotter_resolutions_overdue,
                     COALESCE(blot_res.avg_seconds, 0) AS avg_blotter_resolution
              FROM user_account u
              LEFT JOIN (SELECT approved_by, COUNT(*) AS cnt, AVG(TIMESTAMPDIFF(SECOND, requested_at, approved_at)) AS avg_seconds FROM document_request WHERE approved_at BETWEEN @from AND @to GROUP BY approved_by) appr ON appr.approved_by = u.user_id
              LEFT JOIN (SELECT approved_by, COUNT(*) AS cnt FROM document_request WHERE approved_at BETWEEN @from AND @to AND TIMESTAMPDIFF(DAY, requested_at, approved_at) > 3 GROUP BY approved_by) appr_overdue ON appr_overdue.approved_by = u.user_id
              LEFT JOIN (SELECT released_by, COUNT(*) AS cnt, AVG(TIMESTAMPDIFF(SECOND, approved_at, released_at)) AS avg_seconds FROM document_request WHERE released_at BETWEEN @from AND @to GROUP BY released_by) rel ON rel.released_by = u.user_id
              LEFT JOIN (SELECT released_by, COUNT(*) AS cnt FROM document_request WHERE released_at BETWEEN @from AND @to AND TIMESTAMPDIFF(DAY, approved_at, released_at) > 3 GROUP BY released_by) rel_overdue ON rel_overdue.released_by = u.user_id
              LEFT JOIN (SELECT updated_by, COUNT(*) AS cnt FROM case_record WHERE updated_at BETWEEN @from AND @to GROUP BY updated_by) blot_changes ON blot_changes.updated_by = u.user_id
              LEFT JOIN (SELECT resolved_by, COUNT(*) AS cnt, AVG(TIMESTAMPDIFF(SECOND, created_at, resolved_at)) AS avg_seconds FROM case_record WHERE resolved_at BETWEEN @from AND @to GROUP BY resolved_by) blot_res ON blot_res.resolved_by = u.user_id
              LEFT JOIN (SELECT resolved_by, COUNT(*) AS cnt FROM case_record WHERE resolved_at BETWEEN @from AND @to AND TIMESTAMPDIFF(DAY, created_at, resolved_at) > 15 GROUP BY resolved_by) blot_res_overdue ON blot_res_overdue.resolved_by = u.user_id
              ORDER BY u.is_active DESC, (COALESCE(appr.cnt,0) + COALESCE(rel.cnt,0) + COALESCE(blot_res.cnt,0)) DESC",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
            });

        var staff = new List<StaffPerformanceRow>();
        foreach (DataRow row in table.Rows)
        {
            staff.Add(new StaffPerformanceRow
            {
                UserId = Convert.ToInt32(row["user_id"]),
                Username = row["username"]?.ToString() ?? string.Empty,
                DisplayName = row["display_name"]?.ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(row["is_active"]),
                ApprovalsCompleted = Convert.ToInt32(row["approvals_completed"]),
                ApprovalsOverdue = Convert.ToInt32(row["approvals_overdue"]),
                AvgRequestToApprovalSeconds = Convert.ToDouble(row["avg_req_to_approval"]),
                ReleasesCompleted = Convert.ToInt32(row["releases_completed"]),
                ReleasesOverdue = Convert.ToInt32(row["releases_overdue"]),
                AvgApprovalToReleaseSeconds = Convert.ToDouble(row["avg_approval_to_release"]),
                BlotterStatusChanges = Convert.ToInt32(row["blotter_status_changes"]),
                BlotterResolutions = Convert.ToInt32(row["blotter_resolutions"]),
                BlotterResolutionsOverdue = Convert.ToInt32(row["blotter_resolutions_overdue"]),
                AvgBlotterResolutionSeconds = Convert.ToDouble(row["avg_blotter_resolution"])
            });
        }

        return staff;
    }

    private static IReadOnlyList<HotspotPoint> LoadHotspots(DateTime from, DateTime to, ReportsFilters filters)
    {
        var table = DbHelper.LoadTable(
            @"SELECT p.purok_id, p.purok_name, p.latitude, p.longitude,
                     COUNT(cr.case_id) AS incident_count
              FROM purok p
              LEFT JOIN resident r ON r.purok_id = p.purok_id AND IFNULL(r.is_deleted,0)=0
              LEFT JOIN case_record cr ON cr.complainant_id = r.resident_id
                   AND cr.created_at BETWEEN @from AND @to
              GROUP BY p.purok_id, p.purok_name, p.latitude, p.longitude
              HAVING incident_count > 0
              ORDER BY incident_count DESC",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1));
            });

        var hotspots = new List<HotspotPoint>();
        foreach (DataRow row in table.Rows)
        {
            hotspots.Add(new HotspotPoint
            {
                PurokId = Convert.ToInt32(row["purok_id"]),
                PurokName = row["purok_name"]?.ToString() ?? string.Empty,
                Latitude = row["latitude"] != DBNull.Value ? Convert.ToDouble(row["latitude"]) : null,
                Longitude = row["longitude"] != DBNull.Value ? Convert.ToDouble(row["longitude"]) : null,
                IncidentCount = Convert.ToInt32(row["incident_count"])
            });
        }

        return hotspots;
    }

    private static string GetCertStatusCondition(CertificateStatusFilter filter)
    {
        return filter switch
        {
            CertificateStatusFilter.Pending => " AND dr.status IN ('SUBMITTED','APPROVED')",
            CertificateStatusFilter.Submitted => " AND dr.status = 'SUBMITTED'",
            CertificateStatusFilter.Approved => " AND dr.status = 'APPROVED'",
            CertificateStatusFilter.Released => " AND dr.status = 'RELEASED'",
            CertificateStatusFilter.Cancelled => " AND dr.status = 'CANCELLED'",
            CertificateStatusFilter.Rejected => " AND dr.status = 'REJECTED'",
            _ => " AND dr.status <> 'DRAFT'"
        };
    }

    private static string GetBlotterStatusCondition(BlotterStatusFilter filter)
    {
        return filter switch
        {
            BlotterStatusFilter.Active => " AND cr.status IN ('OPEN','ONGOING')",
            BlotterStatusFilter.Settled => " AND cr.status = 'SETTLED'",
            BlotterStatusFilter.Referred => " AND cr.status = 'REFERRED'",
            BlotterStatusFilter.Closed => " AND cr.status = 'CLOSED'",
            _ => ""
        };
    }
}
