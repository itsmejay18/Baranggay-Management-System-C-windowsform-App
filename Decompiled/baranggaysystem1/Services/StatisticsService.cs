using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1.Services;

/// <summary>
/// Aggregates counts and breakdowns across all modules for the Statistics page.
/// Every query is wrapped in try/catch inside the page so missing tables never break the UI.
/// </summary>
internal sealed class StatisticsService
{
    public sealed class Totals
    {
        public int Residents { get; set; }
        public int ActiveResidents { get; set; }
        public int Seniors { get; set; }
        public int Youth { get; set; }
        public int Pwd { get; set; }
        public int SoloParents { get; set; }
        public int Indigent { get; set; }
        public int FourPs { get; set; }
        public int Voters { get; set; }
        public int Households { get; set; }
        public int Puroks { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public int Deceased { get; set; }

        public int PendingCerts { get; set; }
        public int ReleasedCertsThisMonth { get; set; }
        public int OpenBlotter { get; set; }
        public int ResolvedBlotter { get; set; }

        public int UpcomingMeetings { get; set; }
        public int PendingBookings { get; set; }
        public int ShiftsToday { get; set; }
        public int PatrolLogsThisWeek { get; set; }

        public decimal RevenueThisMonth { get; set; }
        public int AyudaReleasedThisMonth { get; set; }
    }

    public sealed class CategoryCount
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public async Task<Totals> LoadTotalsAsync()
    {
        var totals = new Totals();
        totals.Residents = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_deleted,0)=0"));
        totals.ActiveResidents = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE UPPER(COALESCE(status,'ACTIVE'))='ACTIVE' AND COALESCE(is_deleted,0)=0"));
        totals.Seniors = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_senior,0)=1 AND COALESCE(is_deleted,0)=0"));
        totals.Youth = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_youth,0)=1 AND COALESCE(is_deleted,0)=0"));
        totals.Pwd = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_pwd,0)=1 AND COALESCE(is_deleted,0)=0"));
        totals.SoloParents = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_solo_parent,0)=1 AND COALESCE(is_deleted,0)=0"));
        totals.Indigent = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_indigent,0)=1 AND COALESCE(is_deleted,0)=0"));
        totals.FourPs = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_4ps_beneficiary,0)=1 AND COALESCE(is_deleted,0)=0"));
        totals.Voters = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE COALESCE(is_registered_voter,0)=1 AND COALESCE(is_deleted,0)=0"));
        totals.Households = await Safe(() => Count("SELECT COUNT(*) FROM household"));
        totals.Puroks = await Safe(() => Count("SELECT COUNT(*) FROM purok_sitio WHERE COALESCE(is_active,1)=1"));
        totals.Male = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE UPPER(COALESCE(sex,''))='M' AND COALESCE(is_deleted,0)=0"));
        totals.Female = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE UPPER(COALESCE(sex,''))='F' AND COALESCE(is_deleted,0)=0"));
        totals.Deceased = await Safe(() => Count("SELECT COUNT(*) FROM resident WHERE UPPER(COALESCE(status,''))='DECEASED' AND COALESCE(is_deleted,0)=0"));

        totals.PendingCerts = await Safe(() => Count("SELECT COUNT(*) FROM document_request WHERE UPPER(COALESCE(status,''))='SUBMITTED'"));
        totals.ReleasedCertsThisMonth = await Safe(() => Count(
            "SELECT COUNT(*) FROM document_request WHERE UPPER(COALESCE(status,''))='RELEASED' " +
            "AND strftime('%Y-%m', COALESCE(released_at, created_at)) = strftime('%Y-%m','now')"));
        totals.OpenBlotter = await Safe(() => Count("SELECT COUNT(*) FROM case_record WHERE UPPER(COALESCE(status,'')) IN ('OPEN','ONGOING')"));
        totals.ResolvedBlotter = await Safe(() => Count("SELECT COUNT(*) FROM case_record WHERE UPPER(COALESCE(status,'')) IN ('RESOLVED','CLOSED')"));

        totals.UpcomingMeetings = await Safe(() => Count("SELECT COUNT(*) FROM barangay_meeting WHERE UPPER(status)='SCHEDULED'"));
        totals.PendingBookings = await Safe(() => Count("SELECT COUNT(*) FROM facility_booking WHERE UPPER(status)='PENDING'"));
        totals.ShiftsToday = await Safe(() => Count("SELECT COUNT(*) FROM tanod_shift WHERE DATE(shift_date) = DATE('now')"));
        totals.PatrolLogsThisWeek = await Safe(() => Count(
            "SELECT COUNT(*) FROM tanod_patrol_log WHERE DATE(logged_at) >= DATE('now','-7 days')"));

        totals.RevenueThisMonth = await Safe(() => CountDecimal(
            "SELECT COALESCE(SUM(amount),0) FROM document_payment " +
            "WHERE strftime('%Y-%m', paid_at) = strftime('%Y-%m','now')"));
        totals.AyudaReleasedThisMonth = await Safe(() => Count(
            "SELECT COUNT(*) FROM ayuda_release " +
            "WHERE strftime('%Y-%m', released_at) = strftime('%Y-%m','now')"));

        return totals;
    }

    public async Task<List<CategoryCount>> LoadResidentsByPurokAsync(int limit = 10)
    {
        return await Safe(async () =>
        {
            var table = await DatabaseManagerAsync.LoadTableAsync(
                @"SELECT COALESCE(p.name,'Unassigned') AS label, COUNT(r.resident_id) AS count
                    FROM resident r
                    LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id
                   WHERE COALESCE(r.is_deleted,0) = 0
                   GROUP BY label
                   ORDER BY count DESC
                   LIMIT " + limit).ConfigureAwait(false);
            return ToList(table);
        }, new List<CategoryCount>());
    }

    public async Task<List<CategoryCount>> LoadResidentsByAgeBracketAsync()
    {
        return await Safe(async () =>
        {
            var table = await DatabaseManagerAsync.LoadTableAsync(
                @"SELECT CASE
                           WHEN CAST(strftime('%Y','now') AS INTEGER) - CAST(strftime('%Y', birth_date) AS INTEGER) < 13 THEN '0-12 (Child)'
                           WHEN CAST(strftime('%Y','now') AS INTEGER) - CAST(strftime('%Y', birth_date) AS INTEGER) < 18 THEN '13-17 (Teen)'
                           WHEN CAST(strftime('%Y','now') AS INTEGER) - CAST(strftime('%Y', birth_date) AS INTEGER) < 30 THEN '18-29 (Youth)'
                           WHEN CAST(strftime('%Y','now') AS INTEGER) - CAST(strftime('%Y', birth_date) AS INTEGER) < 60 THEN '30-59 (Adult)'
                           ELSE '60+ (Senior)'
                         END AS label,
                         COUNT(*) AS count
                    FROM resident
                   WHERE COALESCE(is_deleted,0) = 0 AND birth_date IS NOT NULL
                   GROUP BY label
                   ORDER BY MIN(birth_date) DESC").ConfigureAwait(false);
            return ToList(table);
        }, new List<CategoryCount>());
    }

    public async Task<List<CategoryCount>> LoadBlotterByTypeAsync(int limit = 6)
    {
        return await Safe(async () =>
        {
            var table = await DatabaseManagerAsync.LoadTableAsync(
                @"SELECT COALESCE(NULLIF(TRIM(incident_type),''),'Uncategorized') AS label,
                         COUNT(*) AS count
                    FROM case_record
                   GROUP BY label
                   ORDER BY count DESC
                   LIMIT " + limit).ConfigureAwait(false);
            return ToList(table);
        }, new List<CategoryCount>());
    }

    public async Task<List<CategoryCount>> LoadCertificatesByTypeAsync(int limit = 6)
    {
        return await Safe(async () =>
        {
            var table = await DatabaseManagerAsync.LoadTableAsync(
                @"SELECT COALESCE(dt.name,'Other') AS label, COUNT(*) AS count
                    FROM document_request dr
                    LEFT JOIN document_type dt ON dt.document_type_id = dr.document_type_id
                   GROUP BY label
                   ORDER BY count DESC
                   LIMIT " + limit).ConfigureAwait(false);
            return ToList(table);
        }, new List<CategoryCount>());
    }

    public async Task<List<CategoryCount>> LoadBookingsByFacilityAsync(int limit = 6)
    {
        return await Safe(async () =>
        {
            var table = await DatabaseManagerAsync.LoadTableAsync(
                @"SELECT f.facility_name AS label, COUNT(*) AS count
                    FROM facility_booking b
                    JOIN barangay_facility f ON f.facility_id = b.facility_id
                   GROUP BY f.facility_name
                   ORDER BY count DESC
                   LIMIT " + limit).ConfigureAwait(false);
            return ToList(table);
        }, new List<CategoryCount>());
    }

    public async Task<List<CategoryCount>> LoadPatrolLogsBySeverityAsync()
    {
        return await Safe(async () =>
        {
            var table = await DatabaseManagerAsync.LoadTableAsync(
                @"SELECT COALESCE(severity,'LOW') AS label, COUNT(*) AS count
                    FROM tanod_patrol_log
                   GROUP BY COALESCE(severity,'LOW')
                   ORDER BY count DESC").ConfigureAwait(false);
            return ToList(table);
        }, new List<CategoryCount>());
    }

    public async Task<List<CategoryCount>> LoadMonthlyCertificateTrendAsync()
    {
        return await Safe(async () =>
        {
            var table = await DatabaseManagerAsync.LoadTableAsync(
                @"SELECT strftime('%Y-%m', COALESCE(released_at, created_at)) AS label,
                         COUNT(*) AS count
                    FROM document_request
                   WHERE COALESCE(released_at, created_at) >= date('now','-6 months')
                   GROUP BY label
                   ORDER BY label").ConfigureAwait(false);
            return ToList(table);
        }, new List<CategoryCount>());
    }

    // ============================================================
    // Internals
    // ============================================================

    public async Task<string> ExportSummaryCsvAsync(Totals totals, string filePath)
    {
        var lines = new System.Text.StringBuilder();
        lines.AppendLine("Metric,Value");
        lines.AppendLine($"Total Residents,{totals.Residents}");
        lines.AppendLine($"Active Residents,{totals.ActiveResidents}");
        lines.AppendLine($"Households,{totals.Households}");
        lines.AppendLine($"Active Puroks,{totals.Puroks}");
        lines.AppendLine($"Male,{totals.Male}");
        lines.AppendLine($"Female,{totals.Female}");
        lines.AppendLine($"Deceased,{totals.Deceased}");
        lines.AppendLine($"Seniors,{totals.Seniors}");
        lines.AppendLine($"Youth,{totals.Youth}");
        lines.AppendLine($"PWD,{totals.Pwd}");
        lines.AppendLine($"Solo Parents,{totals.SoloParents}");
        lines.AppendLine($"Indigent,{totals.Indigent}");
        lines.AppendLine($"4Ps Beneficiaries,{totals.FourPs}");
        lines.AppendLine($"Registered Voters,{totals.Voters}");
        lines.AppendLine($"Pending Certificates,{totals.PendingCerts}");
        lines.AppendLine($"Released Certificates (Month),{totals.ReleasedCertsThisMonth}");
        lines.AppendLine($"Open Blotter,{totals.OpenBlotter}");
        lines.AppendLine($"Resolved Blotter,{totals.ResolvedBlotter}");
        lines.AppendLine($"Upcoming Meetings,{totals.UpcomingMeetings}");
        lines.AppendLine($"Pending Bookings,{totals.PendingBookings}");
        lines.AppendLine($"Shifts Today,{totals.ShiftsToday}");
        lines.AppendLine($"Patrol Logs (7 days),{totals.PatrolLogsThisWeek}");
        lines.AppendLine($"Revenue This Month,{totals.RevenueThisMonth:F2}");
        lines.AppendLine($"Ayuda Released (Month),{totals.AyudaReleasedThisMonth}");
        lines.AppendLine($"Exported At,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await System.IO.File.WriteAllTextAsync(filePath, lines.ToString()).ConfigureAwait(false);
        return filePath;
    }

    private static List<CategoryCount> ToList(DataTable table)
    {
        var list = new List<CategoryCount>();
        foreach (DataRow row in table.Rows)
        {
            list.Add(new CategoryCount
            {
                Label = row["label"]?.ToString() ?? "",
                Count = row["count"] == DBNull.Value ? 0 : Convert.ToInt32(row["count"])
            });
        }
        return list;
    }

    private static async Task<int> Count(string sql)
    {
        return await DatabaseManagerAsync.ExecuteScalarAsync<int>(sql).ConfigureAwait(false);
    }

    private static async Task<decimal> CountDecimal(string sql)
    {
        return await DatabaseManagerAsync.ExecuteScalarAsync<decimal>(sql).ConfigureAwait(false);
    }

    private static async Task<T> Safe<T>(Func<Task<T>> action, T fallback = default!)
    {
        try { return await action().ConfigureAwait(false); }
        catch { return fallback!; }
    }
}
