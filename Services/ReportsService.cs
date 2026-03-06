using System;
using System.Collections.Generic;
using System.Linq;
using baranggaysystem1.Database;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal enum CertificateStatusFilter
{
    AllNonDraft = 0,
    Pending = 1,
    Submitted = 2,
    Approved = 3,
    Released = 4,
    Cancelled = 5,
    Rejected = 6
}

internal enum BlotterStatusFilter
{
    All = 0,
    Active = 1,
    Settled = 2,
    Referred = 3,
    Closed = 4
}

internal sealed class ReportsFilters
{
    public int? PurokId { get; init; }

    public CertificateStatusFilter CertificateStatus { get; init; } = CertificateStatusFilter.AllNonDraft;

    public BlotterStatusFilter BlotterStatus { get; init; } = BlotterStatusFilter.All;
}

internal sealed class MonthlyTrendRow
{
    public string MonthKey { get; init; } = string.Empty; // yyyy-MM
    public string MonthLabel { get; init; } = string.Empty; // MMM yyyy
    public int Residents { get; init; }
    public int Certificates { get; init; }
    public int Blotters { get; init; }
}

internal sealed class ReportsSummary
{
    public int NewResidents { get; init; }
    public int CertificateRequests { get; init; }
    public int CertificatesReleased { get; init; }
    public int BlottersFiled { get; init; }

    public int TotalResidents { get; init; }
    public int PendingCertificates { get; init; }
    public int ActiveBlotters { get; init; }
}

internal sealed class ServiceTimeMetrics
{
    public int ApprovalSamples { get; init; }

    public double AvgRequestToApprovalSeconds { get; init; }

    public int ReleaseSamples { get; init; }

    public double AvgApprovalToReleaseSeconds { get; init; }
}

internal sealed class StaffPerformanceRow
{
    public int UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int ApprovalsCompleted { get; set; }

    public int ApprovalsOverdue { get; set; }

    public double AvgRequestToApprovalSeconds { get; set; }

    public int ReleasesCompleted { get; set; }

    public int ReleasesOverdue { get; set; }

    public double AvgApprovalToReleaseSeconds { get; set; }

    public int BlotterStatusChanges { get; set; }

    public int BlotterResolutions { get; set; }

    public int BlotterResolutionsOverdue { get; set; }

    public double AvgBlotterResolutionSeconds { get; set; }
}

internal sealed class HotspotPoint
{
    public int PurokId { get; init; }
    public string PurokName { get; init; } = string.Empty;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public int IncidentCount { get; init; }
}

internal sealed class ReportsDashboardData
{
    public IReadOnlyList<MonthlyTrendRow> Trends { get; init; } = Array.Empty<MonthlyTrendRow>();
    public ReportsSummary Summary { get; init; } = new ReportsSummary();
    public ServiceTimeMetrics ServiceTimes { get; init; } = new ServiceTimeMetrics();
    public IReadOnlyList<StaffPerformanceRow> StaffPerformance { get; init; } = Array.Empty<StaffPerformanceRow>();
    public IReadOnlyList<HotspotPoint> Hotspots { get; init; } = Array.Empty<HotspotPoint>();
}

internal static class ReportsService
{
    public static ReportsDashboardData LoadDashboard(DateTime fromDate, DateTime toDate, ReportsFilters? filters = null)
    {
        if (fromDate > toDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        ReportsFilters safeFilters = filters ?? new ReportsFilters();

        DateTime from = fromDate.Date;
        DateTime to = toDate.Date;
        DateTime toExclusive = to.AddDays(1);

        using var conn = Database.DBConnection.GetConnection();
        conn.Open();

        var trends = LoadMonthlyTrends(conn, from, to, toExclusive, safeFilters.PurokId, safeFilters.CertificateStatus, safeFilters.BlotterStatus);
        var summary = LoadSummary(conn, from, to, toExclusive, safeFilters.PurokId);
        var serviceTimes = LoadServiceTimes(conn, from, to, toExclusive, safeFilters.PurokId);
        var staff = LoadStaffPerformance(conn, from, toExclusive, safeFilters.PurokId);
        var hotspots = LoadHotspots(conn, from, toExclusive, safeFilters.PurokId, safeFilters.BlotterStatus);

        return new ReportsDashboardData
        {
            Trends = trends,
            Summary = summary,
            ServiceTimes = serviceTimes,
            StaffPerformance = staff,
            Hotspots = hotspots
        };
    }

    private static List<MonthlyTrendRow> LoadMonthlyTrends(
        MySqlConnection conn,
        DateTime from,
        DateTime to,
        DateTime toExclusive,
        int? purokId,
        CertificateStatusFilter certificateStatus,
        BlotterStatusFilter blotterStatus)
    {
        var residentsByMonth = LoadMonthlyCounts(
            conn,
            @"SELECT DATE_FORMAT(date_registered, '%Y-%m') AS ym, COUNT(*) AS cnt
              FROM resident
              WHERE IFNULL(is_deleted,0)=0
                AND date_registered BETWEEN @from AND @to
                AND (@purokId IS NULL OR purok_id = @purokId)
              GROUP BY ym
              ORDER BY ym",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to);
                cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
            });

        string certStatusClause = BuildCertificateStatusClause(certificateStatus);
        var certsByMonth = LoadMonthlyCounts(
            conn,
            $@"SELECT DATE_FORMAT(dr.requested_at, '%Y-%m') AS ym, COUNT(*) AS cnt
              FROM document_request dr
              INNER JOIN resident r ON r.resident_id = dr.resident_id
              WHERE {certStatusClause}
                AND dr.requested_at >= @from
                AND dr.requested_at < @toExcl
                AND (@purokId IS NULL OR r.purok_id = @purokId)
              GROUP BY ym
              ORDER BY ym",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@toExcl", toExclusive);
                cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
            });

        string blotterStatusClause = BuildBlotterStatusClause(blotterStatus);
        var blottersByMonth = LoadMonthlyCounts(
            conn,
            $@"SELECT DATE_FORMAT(cr.date_filed, '%Y-%m') AS ym, COUNT(*) AS cnt
              FROM case_record cr
              LEFT JOIN resident r ON r.resident_id = cr.complainant_id
              WHERE cr.date_filed BETWEEN @from AND @to
                {blotterStatusClause}
                AND (@purokId IS NULL OR r.purok_id = @purokId)
              GROUP BY ym
              ORDER BY ym",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@to", to);
                cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
            });

        DateTime cursor = new DateTime(from.Year, from.Month, 1);
        DateTime end = new DateTime(to.Year, to.Month, 1);

        var rows = new List<MonthlyTrendRow>();
        while (cursor <= end)
        {
            string key = cursor.ToString("yyyy-MM");
            rows.Add(new MonthlyTrendRow
            {
                MonthKey = key,
                MonthLabel = cursor.ToString("MMM yyyy"),
                Residents = residentsByMonth.TryGetValue(key, out int r) ? r : 0,
                Certificates = certsByMonth.TryGetValue(key, out int c) ? c : 0,
                Blotters = blottersByMonth.TryGetValue(key, out int b) ? b : 0
            });

            cursor = cursor.AddMonths(1);
        }

        return rows;
    }

    private static ReportsSummary LoadSummary(MySqlConnection conn, DateTime from, DateTime to, DateTime toExclusive, int? purokId)
    {
        return new ReportsSummary
        {
            NewResidents = ExecuteCount(
                conn,
                @"SELECT COUNT(*)
                  FROM resident
                  WHERE IFNULL(is_deleted,0)=0
                    AND date_registered BETWEEN @from AND @to
                    AND (@purokId IS NULL OR purok_id = @purokId)",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
                }),

            CertificateRequests = ExecuteCount(
                conn,
                @"SELECT COUNT(*)
                  FROM document_request dr
                  INNER JOIN resident r ON r.resident_id = dr.resident_id
                  WHERE UPPER(dr.status) <> 'DRAFT'
                    AND dr.requested_at >= @from
                    AND dr.requested_at < @toExcl
                    AND (@purokId IS NULL OR r.purok_id = @purokId)",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@toExcl", toExclusive);
                    cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
                }),

            CertificatesReleased = ExecuteCount(
                conn,
                @"SELECT COUNT(*)
                  FROM document_request dr
                  INNER JOIN resident r ON r.resident_id = dr.resident_id
                  WHERE dr.released_at IS NOT NULL
                    AND dr.released_at >= @from
                    AND dr.released_at < @toExcl
                    AND (@purokId IS NULL OR r.purok_id = @purokId)",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@toExcl", toExclusive);
                    cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
                }),

            BlottersFiled = ExecuteCount(
                conn,
                @"SELECT COUNT(*)
                  FROM case_record cr
                  LEFT JOIN resident r ON r.resident_id = cr.complainant_id
                  WHERE cr.date_filed BETWEEN @from AND @to
                    AND (@purokId IS NULL OR r.purok_id = @purokId)",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
                }),

            TotalResidents = ExecuteCount(
                conn,
                "SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND (@purokId IS NULL OR purok_id = @purokId)",
                cmd => cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value)),

            PendingCertificates = ExecuteCount(
                conn,
                @"SELECT COUNT(*)
                  FROM document_request dr
                  INNER JOIN resident r ON r.resident_id = dr.resident_id
                  WHERE UPPER(dr.status) IN ('SUBMITTED','APPROVED','REQUESTED')
                    AND (@purokId IS NULL OR r.purok_id = @purokId)",
                cmd => cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value)),

            ActiveBlotters = ExecuteCount(
                conn,
                @"SELECT COUNT(*)
                  FROM case_record cr
                  LEFT JOIN resident r ON r.resident_id = cr.complainant_id
                  WHERE UPPER(cr.status) IN ('OPEN','ONGOING')
                    AND (@purokId IS NULL OR r.purok_id = @purokId)",
                cmd => cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value))
        };
    }

    private static ServiceTimeMetrics LoadServiceTimes(MySqlConnection conn, DateTime from, DateTime to, DateTime toExclusive, int? purokId)
    {
        // "Service time" is computed based on events completed within the selected window:
        // - Request->Approve: approved_at is within [from, toExclusive)
        // - Approve->Release: released_at is within [from, toExclusive)
        var approval = ExecuteCountAndAverageSeconds(
            conn,
            @"SELECT COUNT(*) AS n,
                     AVG(TIMESTAMPDIFF(SECOND, requested_at, approved_at)) AS avg_seconds
              FROM document_request dr
              INNER JOIN resident r ON r.resident_id = dr.resident_id
              WHERE dr.requested_at IS NOT NULL
                AND dr.approved_at IS NOT NULL
                AND dr.approved_at >= @from
                AND dr.approved_at < @toExcl
                AND dr.approved_at >= dr.requested_at
                AND (@purokId IS NULL OR r.purok_id = @purokId)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@toExcl", toExclusive);
                cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
            });

        var release = ExecuteCountAndAverageSeconds(
            conn,
            @"SELECT COUNT(*) AS n,
                     AVG(TIMESTAMPDIFF(SECOND, approved_at, released_at)) AS avg_seconds
              FROM document_request dr
              INNER JOIN resident r ON r.resident_id = dr.resident_id
              WHERE dr.approved_at IS NOT NULL
                AND dr.released_at IS NOT NULL
                AND dr.released_at >= @from
                AND dr.released_at < @toExcl
                AND dr.released_at >= dr.approved_at
                AND (@purokId IS NULL OR r.purok_id = @purokId)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@from", from);
                cmd.Parameters.AddWithValue("@toExcl", toExclusive);
                cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
            });

        return new ServiceTimeMetrics
        {
            ApprovalSamples = approval.Count,
            AvgRequestToApprovalSeconds = approval.AverageSeconds,
            ReleaseSamples = release.Count,
            AvgApprovalToReleaseSeconds = release.AverageSeconds
        };
    }

    private static List<StaffPerformanceRow> LoadStaffPerformance(MySqlConnection conn, DateTime from, DateTime toExclusive, int? purokId)
    {
        // Build baseline user list first, then fill aggregated metrics.
        var users = new Dictionary<int, StaffPerformanceRow>();
        using (var cmd = new MySqlCommand(
                   @"SELECT user_id,
                            username,
                            COALESCE(NULLIF(full_name,''), NULLIF(CONCAT_WS(' ', first_name, last_name), ''), username) AS display_name,
                            IFNULL(is_active,1) AS is_active
                     FROM user_account
                     ORDER BY IFNULL(is_active,1) DESC, username",
                   conn))
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int id = Convert.ToInt32(reader["user_id"]);
                string username = reader["username"]?.ToString() ?? string.Empty;
                string display = reader["display_name"]?.ToString() ?? username;
                bool active = false;
                if (reader["is_active"] != DBNull.Value)
                {
                    active = Convert.ToInt32(reader["is_active"]) != 0;
                }

                users[id] = new StaffPerformanceRow
                {
                    UserId = id,
                    Username = username,
                    DisplayName = display,
                    IsActive = active
                };
            }
        }

        // Certificate approvals
        using (var cmd = new MySqlCommand(
                   $@"SELECT approved_by_user_id AS user_id,
                             COUNT(*) AS completed,
                             SUM(CASE
                                   WHEN DATE(approved_at) > DATE_ADD(DATE(requested_at), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY) THEN 1
                                   ELSE 0
                                 END) AS overdue_completed,
                             AVG(TIMESTAMPDIFF(SECOND, requested_at, approved_at)) AS avg_seconds
                      FROM document_request dr
                      INNER JOIN resident r ON r.resident_id = dr.resident_id
                      WHERE dr.approved_by_user_id IS NOT NULL
                        AND dr.requested_at IS NOT NULL
                        AND dr.approved_at IS NOT NULL
                        AND dr.approved_at >= @from
                        AND dr.approved_at < @toExcl
                        AND dr.approved_at >= dr.requested_at
                        AND (@purokId IS NULL OR r.purok_id = @purokId)
                      GROUP BY approved_by_user_id",
                   conn))
        {
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@toExcl", toExclusive);
            cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int userId = Convert.ToInt32(reader["user_id"]);
                var row = GetOrCreateUser(users, userId);

                row.ApprovalsCompleted = ReadInt(reader, "completed");
                row.ApprovalsOverdue = ReadInt(reader, "overdue_completed");
                row.AvgRequestToApprovalSeconds = ReadDouble(reader, "avg_seconds");
            }
        }

        // Certificate releases
        using (var cmd = new MySqlCommand(
                   $@"SELECT released_by_user_id AS user_id,
                             COUNT(*) AS completed,
                             SUM(CASE
                                   WHEN DATE(released_at) > DATE_ADD(DATE(approved_at), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY) THEN 1
                                   ELSE 0
                                 END) AS overdue_completed,
                             AVG(TIMESTAMPDIFF(SECOND, approved_at, released_at)) AS avg_seconds
                      FROM document_request dr
                      INNER JOIN resident r ON r.resident_id = dr.resident_id
                      WHERE dr.released_by_user_id IS NOT NULL
                        AND dr.approved_at IS NOT NULL
                        AND dr.released_at IS NOT NULL
                        AND dr.released_at >= @from
                        AND dr.released_at < @toExcl
                        AND dr.released_at >= dr.approved_at
                        AND (@purokId IS NULL OR r.purok_id = @purokId)
                      GROUP BY released_by_user_id",
                   conn))
        {
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@toExcl", toExclusive);
            cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int userId = Convert.ToInt32(reader["user_id"]);
                var row = GetOrCreateUser(users, userId);

                row.ReleasesCompleted = ReadInt(reader, "completed");
                row.ReleasesOverdue = ReadInt(reader, "overdue_completed");
                row.AvgApprovalToReleaseSeconds = ReadDouble(reader, "avg_seconds");
            }
        }

        // Blotter status changes / resolutions
        using (var cmd = new MySqlCommand(
                   $@"SELECT ct.created_by_user_id AS user_id,
                             COUNT(*) AS status_changes,
                             SUM(CASE WHEN ct.to_status IN ('SETTLED','REFERRED','CLOSED') THEN 1 ELSE 0 END) AS resolutions,
                             SUM(CASE
                                   WHEN ct.to_status IN ('SETTLED','REFERRED','CLOSED')
                                        AND DATE(ct.created_at) > DATE_ADD(DATE(cr.created_at), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY)
                                   THEN 1
                                   ELSE 0
                                 END) AS resolutions_overdue,
                             AVG(CASE WHEN ct.to_status IN ('SETTLED','REFERRED','CLOSED')
                                      THEN TIMESTAMPDIFF(SECOND, cr.created_at, ct.created_at)
                                      ELSE NULL
                                 END) AS avg_resolution_seconds
                      FROM case_timeline ct
                      INNER JOIN case_record cr ON cr.case_id = ct.case_id
                      LEFT JOIN resident r ON r.resident_id = cr.complainant_id
                      WHERE ct.created_by_user_id IS NOT NULL
                        AND ct.event_type = 'STATUS_CHANGE'
                        AND ct.created_at >= @from
                        AND ct.created_at < @toExcl
                        AND (@purokId IS NULL OR r.purok_id = @purokId)
                      GROUP BY ct.created_by_user_id",
                   conn))
        {
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@toExcl", toExclusive);
            cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int userId = Convert.ToInt32(reader["user_id"]);
                var row = GetOrCreateUser(users, userId);

                row.BlotterStatusChanges = ReadInt(reader, "status_changes");
                row.BlotterResolutions = ReadInt(reader, "resolutions");
                row.BlotterResolutionsOverdue = ReadInt(reader, "resolutions_overdue");
                row.AvgBlotterResolutionSeconds = ReadDouble(reader, "avg_resolution_seconds");
            }
        }

        return users.Values.ToList();
    }

    private static List<HotspotPoint> LoadHotspots(
        MySqlConnection conn,
        DateTime from,
        DateTime toExclusive,
        int? purokId,
        BlotterStatusFilter blotterStatus)
    {
        string blotterStatusClause = BuildBlotterStatusClause(blotterStatus);

        using var cmd = new MySqlCommand(
            $@"SELECT p.purok_id,
                      p.name AS purok_name,
                      p.latitude,
                      p.longitude,
                      COUNT(cr.case_id) AS incident_count
               FROM purok_sitio p
               LEFT JOIN resident r
                      ON r.purok_id = p.purok_id
                     AND IFNULL(r.is_deleted,0) = 0
               LEFT JOIN case_record cr
                      ON cr.complainant_id = r.resident_id
                     AND cr.date_filed >= @from
                     AND cr.date_filed < @toExcl
                     {blotterStatusClause}
               WHERE p.barangay_id = @barangayId
                 AND (@purokId IS NULL OR p.purok_id = @purokId)
               GROUP BY p.purok_id, p.name, p.latitude, p.longitude
               ORDER BY incident_count DESC, p.name ASC",
            conn);
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@toExcl", toExclusive);
        cmd.Parameters.AddWithValue("@barangayId", SchemaDefaults.DefaultBarangayId);
        cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);

        var rows = new List<HotspotPoint>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new HotspotPoint
            {
                PurokId = Convert.ToInt32(reader["purok_id"]),
                PurokName = Convert.ToString(reader["purok_name"]) ?? string.Empty,
                Latitude = reader["latitude"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["latitude"]),
                Longitude = reader["longitude"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["longitude"]),
                IncidentCount = reader["incident_count"] == DBNull.Value ? 0 : Convert.ToInt32(reader["incident_count"])
            });
        }

        return rows;
    }

    private static StaffPerformanceRow GetOrCreateUser(Dictionary<int, StaffPerformanceRow> users, int userId)
    {
        if (users.TryGetValue(userId, out StaffPerformanceRow? existing))
        {
            return existing;
        }

        var placeholder = new StaffPerformanceRow
        {
            UserId = userId,
            Username = $"#{userId}",
            DisplayName = $"User #{userId}",
            IsActive = false
        };
        users[userId] = placeholder;
        return placeholder;
    }

    private static int ReadInt(MySqlDataReader reader, string column)
    {
        object value = reader[column];
        return value == DBNull.Value ? 0 : Convert.ToInt32(value);
    }

    private static double ReadDouble(MySqlDataReader reader, string column)
    {
        object value = reader[column];
        return value == DBNull.Value ? 0 : Convert.ToDouble(value);
    }

    private static string BuildCertificateStatusClause(CertificateStatusFilter filter)
    {
        return filter switch
        {
            CertificateStatusFilter.Pending => "UPPER(dr.status) IN ('SUBMITTED','APPROVED','REQUESTED')",
            CertificateStatusFilter.Submitted => "UPPER(dr.status) IN ('SUBMITTED','REQUESTED')",
            CertificateStatusFilter.Approved => "UPPER(dr.status) = 'APPROVED'",
            CertificateStatusFilter.Released => "UPPER(dr.status) IN ('RELEASED','ISSUED')",
            CertificateStatusFilter.Cancelled => "UPPER(dr.status) = 'CANCELLED'",
            CertificateStatusFilter.Rejected => "UPPER(dr.status) = 'REJECTED'",
            _ => "UPPER(dr.status) <> 'DRAFT'"
        };
    }

    private static string BuildBlotterStatusClause(BlotterStatusFilter filter)
    {
        return filter switch
        {
            BlotterStatusFilter.Active => "AND UPPER(cr.status) IN ('OPEN','ONGOING')",
            BlotterStatusFilter.Settled => "AND UPPER(cr.status) = 'SETTLED'",
            BlotterStatusFilter.Referred => "AND UPPER(cr.status) = 'REFERRED'",
            BlotterStatusFilter.Closed => "AND UPPER(cr.status) = 'CLOSED'",
            _ => string.Empty
        };
    }

    private static Dictionary<string, int> LoadMonthlyCounts(MySqlConnection conn, string sql, Action<MySqlCommand> configure)
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);

        using var cmd = new MySqlCommand(sql, conn);
        configure(cmd);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string ym = reader["ym"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ym))
            {
                continue;
            }

            int cnt = reader["cnt"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cnt"]);
            dict[ym] = cnt;
        }

        return dict;
    }

    private static (int Count, double AverageSeconds) ExecuteCountAndAverageSeconds(
        MySqlConnection conn,
        string sql,
        Action<MySqlCommand> configure)
    {
        using var cmd = new MySqlCommand(sql, conn);
        configure(cmd);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return (0, 0);
        }

        int count = reader["n"] == DBNull.Value ? 0 : Convert.ToInt32(reader["n"]);
        double avgSeconds = reader["avg_seconds"] == DBNull.Value ? 0 : Convert.ToDouble(reader["avg_seconds"]);
        if (count <= 0 || avgSeconds < 0)
        {
            return (0, 0);
        }

        return (count, avgSeconds);
    }

    private static int ExecuteCount(MySqlConnection conn, string sql, Action<MySqlCommand>? configure = null)
    {
        using var cmd = new MySqlCommand(sql, conn);
        configure?.Invoke(cmd);
        object? result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt32(result);
    }
}
