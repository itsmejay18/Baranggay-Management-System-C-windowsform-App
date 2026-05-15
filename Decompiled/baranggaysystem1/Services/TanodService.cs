using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Manages Barangay Tanod members, patrol shifts, and patrol logs.
/// </summary>
internal sealed class TanodService
{
    public static readonly string[] ShiftTypes = new[] { "MORNING", "AFTERNOON", "EVENING", "GRAVEYARD" };
    public static readonly string[] Severities = new[] { "LOW", "MEDIUM", "HIGH", "CRITICAL" };
    public static readonly string[] AttendanceStatuses = new[] { "SCHEDULED", "PRESENT", "ABSENT", "LATE" };

    // ============================================================
    // TANOD MEMBERS
    // ============================================================

    public async Task<DataTable> LoadMembersAsync(bool includeInactive = false)
    {
        string sql = @"SELECT tanod_id,
                              full_name,
                              COALESCE(contact_number, '') AS contact_number,
                              COALESCE(rank_title, '') AS rank_title,
                              date_assigned,
                              is_active,
                              COALESCE(remarks, '') AS remarks
                         FROM tanod_member
                        WHERE (@includeInactive = 1 OR is_active = 1)
                        ORDER BY full_name";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@includeInactive", (object)(includeInactive ? 1 : 0));
        }).ConfigureAwait(false);
    }

    public async Task<int> CreateMemberAsync(string fullName, string? contact, string? rank,
        DateTime? dateAssigned, int? residentId, string? remarks)
    {
        const string sql = @"INSERT INTO tanod_member
                               (full_name, contact_number, rank_title, date_assigned, resident_id, remarks, is_active)
                             VALUES
                               (@name, @contact, @rank, @assigned, @rid, @remarks, 1)";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@name", (object)(fullName ?? "").Trim());
            cmd.Parameters.AddWithValue("@contact", DbNullIfEmpty(contact));
            cmd.Parameters.AddWithValue("@rank", DbNullIfEmpty(rank));
            cmd.Parameters.AddWithValue("@assigned", dateAssigned.HasValue ? (object)dateAssigned.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@rid", residentId.HasValue && residentId.Value > 0
                ? (object)residentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@remarks", DbNullIfEmpty(remarks));
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            "SELECT tanod_id FROM tanod_member WHERE full_name = @name ORDER BY tanod_id DESC LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@name", (object)(fullName ?? "").Trim())).ConfigureAwait(false);
        int id = idTable.Rows.Count > 0 ? Convert.ToInt32(idTable.Rows[0]["tanod_id"]) : 0;
        AuditTrailService.Log("Tanod", "tanod_member", id, "CREATE", null,
            new { fullName, rank }, "Tanod member registered.");
        return id;
    }

    public async Task UpdateMemberAsync(int tanodId, string fullName, string? contact, string? rank,
        DateTime? dateAssigned, bool isActive, string? remarks)
    {
        const string sql = @"UPDATE tanod_member
                                SET full_name = @name,
                                    contact_number = @contact,
                                    rank_title = @rank,
                                    date_assigned = @assigned,
                                    is_active = @active,
                                    remarks = @remarks
                              WHERE tanod_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)tanodId);
            cmd.Parameters.AddWithValue("@name", (object)(fullName ?? "").Trim());
            cmd.Parameters.AddWithValue("@contact", DbNullIfEmpty(contact));
            cmd.Parameters.AddWithValue("@rank", DbNullIfEmpty(rank));
            cmd.Parameters.AddWithValue("@assigned", dateAssigned.HasValue ? (object)dateAssigned.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@active", (object)(isActive ? 1 : 0));
            cmd.Parameters.AddWithValue("@remarks", DbNullIfEmpty(remarks));
        }).ConfigureAwait(false);
        AuditTrailService.Log("Tanod", "tanod_member", tanodId, "UPDATE", null,
            new { fullName, isActive }, "Tanod member updated.");
    }

    // ============================================================
    // SHIFTS
    // ============================================================

    public async Task<DataTable> LoadShiftsAsync(DateTime? fromDate = null, DateTime? toDate = null,
        string? shiftType = null)
    {
        string sql = @"SELECT s.shift_id,
                              s.shift_date,
                              s.shift_type,
                              s.start_time,
                              s.end_time,
                              COALESCE(s.area_assignment, '') AS area_assignment,
                              (SELECT COUNT(*) FROM tanod_shift_assignment a WHERE a.shift_id = s.shift_id) AS assigned_count,
                              COALESCE(s.notes, '') AS notes
                         FROM tanod_shift s
                        WHERE (@hasFrom = 0 OR s.shift_date >= @from)
                          AND (@hasTo = 0 OR s.shift_date <= @to)
                          AND (@type = '' OR s.shift_type = @type)
                        ORDER BY s.shift_date DESC, s.start_time";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@hasFrom", (object)(fromDate.HasValue ? 1 : 0));
            cmd.Parameters.AddWithValue("@from", fromDate.HasValue ? (object)fromDate.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@hasTo", (object)(toDate.HasValue ? 1 : 0));
            cmd.Parameters.AddWithValue("@to", toDate.HasValue ? (object)toDate.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@type", (object)(shiftType ?? string.Empty));
        }).ConfigureAwait(false);
    }

    public async Task<DataTable> GetShiftAssignmentsAsync(int shiftId)
    {
        return await DatabaseManagerAsync.LoadTableAsync(
            @"SELECT a.assignment_id,
                     a.tanod_id,
                     m.full_name,
                     COALESCE(m.rank_title, '') AS rank_title,
                     a.attendance_status,
                     a.check_in_at,
                     a.check_out_at
                FROM tanod_shift_assignment a
                JOIN tanod_member m ON m.tanod_id = a.tanod_id
               WHERE a.shift_id = @sid
               ORDER BY m.full_name",
            cmd => cmd.Parameters.AddWithValue("@sid", (object)shiftId)).ConfigureAwait(false);
    }

    public async Task<int> CreateShiftAsync(DateTime shiftDate, string shiftType,
        TimeSpan startTime, TimeSpan endTime, string? areaAssignment, string? notes,
        int[]? tanodIds = null)
    {
        const string sql = @"INSERT INTO tanod_shift
                               (shift_date, shift_type, start_time, end_time, area_assignment, notes)
                             VALUES
                               (@date, @type, @start, @end, @area, @notes)";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@date", (object)shiftDate.Date);
            cmd.Parameters.AddWithValue("@type", (object)NormalizeShiftType(shiftType));
            cmd.Parameters.AddWithValue("@start", (object)startTime);
            cmd.Parameters.AddWithValue("@end", (object)endTime);
            cmd.Parameters.AddWithValue("@area", DbNullIfEmpty(areaAssignment));
            cmd.Parameters.AddWithValue("@notes", DbNullIfEmpty(notes));
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            "SELECT shift_id FROM tanod_shift WHERE shift_date = @date AND shift_type = @type ORDER BY shift_id DESC LIMIT 1",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@date", (object)shiftDate.Date);
                cmd.Parameters.AddWithValue("@type", (object)NormalizeShiftType(shiftType));
            }).ConfigureAwait(false);
        int shiftId = idTable.Rows.Count > 0 ? Convert.ToInt32(idTable.Rows[0]["shift_id"]) : 0;

        if (shiftId > 0 && tanodIds != null && tanodIds.Length > 0)
        {
            foreach (int tid in tanodIds)
            {
                if (tid <= 0) continue;
                // Use INSERT OR IGNORE which works on both MySQL 8+ and SQLite
                // (MySQL supports INSERT IGNORE, SQLite supports INSERT OR IGNORE).
                // We use NOT EXISTS for portability.
                await DatabaseManagerAsync.ExecuteNonQueryAsync(
                    @"INSERT INTO tanod_shift_assignment (shift_id, tanod_id, attendance_status)
                      SELECT @sid, @tid, 'SCHEDULED'
                      WHERE NOT EXISTS (
                          SELECT 1 FROM tanod_shift_assignment
                          WHERE shift_id = @sid AND tanod_id = @tid
                      )",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@sid", (object)shiftId);
                        cmd.Parameters.AddWithValue("@tid", (object)tid);
                    }).ConfigureAwait(false);
            }
        }

        AuditTrailService.Log("Tanod", "tanod_shift", shiftId, "CREATE", null,
            new { shiftDate, shiftType }, "Patrol shift scheduled.");
        return shiftId;
    }

    public async Task UpdateAttendanceAsync(int assignmentId, string status)
    {
        string normalized = NormalizeAttendanceStatus(status);
        bool isCheckIn = normalized == "PRESENT" || normalized == "LATE";
        const string sql = @"UPDATE tanod_shift_assignment
                                SET attendance_status = @status,
                                    check_in_at = CASE WHEN @checkin = 1 AND check_in_at IS NULL
                                                       THEN CURRENT_TIMESTAMP ELSE check_in_at END
                              WHERE assignment_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)assignmentId);
            cmd.Parameters.AddWithValue("@status", (object)normalized);
            cmd.Parameters.AddWithValue("@checkin", (object)(isCheckIn ? 1 : 0));
        }).ConfigureAwait(false);
    }

    public async Task DeleteShiftAsync(int shiftId)
    {
        await DatabaseManagerAsync.ExecuteNonQueryAsync(
            "DELETE FROM tanod_shift WHERE shift_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", (object)shiftId)).ConfigureAwait(false);
        AuditTrailService.Log("Tanod", "tanod_shift", shiftId, "DELETE", null, null, "Patrol shift deleted.");
    }

    // ============================================================
    // PATROL LOGS
    // ============================================================

    public async Task<DataTable> LoadPatrolLogsAsync(DateTime? fromDate = null, DateTime? toDate = null,
        string? severityFilter = null, string? searchText = null)
    {
        string search = (searchText ?? string.Empty).Trim();
        string like = "%" + search + "%";
        string sql = @"SELECT l.log_id,
                              l.logged_at,
                              COALESCE(l.location, '') AS location,
                              COALESCE(l.incident_type, '') AS incident_type,
                              l.description,
                              l.severity,
                              COALESCE(l.action_taken, '') AS action_taken,
                              COALESCE(l.reported_by, '') AS reported_by
                         FROM tanod_patrol_log l
                        WHERE (@hasFrom = 0 OR l.logged_at >= @from)
                          AND (@hasTo = 0 OR l.logged_at <= @to)
                          AND (@severity = '' OR l.severity = @severity)
                          AND (@search = '' OR l.description LIKE @like OR COALESCE(l.location, '') LIKE @like)
                        ORDER BY l.logged_at DESC";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@hasFrom", (object)(fromDate.HasValue ? 1 : 0));
            cmd.Parameters.AddWithValue("@from", fromDate.HasValue ? (object)fromDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@hasTo", (object)(toDate.HasValue ? 1 : 0));
            cmd.Parameters.AddWithValue("@to", toDate.HasValue ? (object)toDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@severity", (object)(severityFilter ?? string.Empty));
            cmd.Parameters.AddWithValue("@search", (object)search);
            cmd.Parameters.AddWithValue("@like", (object)like);
        }).ConfigureAwait(false);
    }

    public async Task<int> CreatePatrolLogAsync(int? shiftId, string? location, string? incidentType,
        string description, string severity, string? actionTaken, string? reportedBy)
    {
        const string sql = @"INSERT INTO tanod_patrol_log
                               (shift_id, location, incident_type, description, severity,
                                action_taken, reported_by)
                             VALUES
                               (@sid, @loc, @type, @desc, @sev, @action, @reporter)";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@sid", shiftId.HasValue && shiftId.Value > 0
                ? (object)shiftId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@loc", DbNullIfEmpty(location));
            cmd.Parameters.AddWithValue("@type", DbNullIfEmpty(incidentType));
            cmd.Parameters.AddWithValue("@desc", (object)(description ?? "").Trim());
            cmd.Parameters.AddWithValue("@sev", (object)NormalizeSeverity(severity));
            cmd.Parameters.AddWithValue("@action", DbNullIfEmpty(actionTaken));
            cmd.Parameters.AddWithValue("@reporter", DbNullIfEmpty(reportedBy));
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            "SELECT log_id FROM tanod_patrol_log ORDER BY log_id DESC LIMIT 1").ConfigureAwait(false);
        int id = idTable.Rows.Count > 0 ? Convert.ToInt32(idTable.Rows[0]["log_id"]) : 0;
        AuditTrailService.Log("Tanod", "tanod_patrol_log", id, "CREATE", null,
            new { location, severity }, "Patrol log entry added.");
        return id;
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static string NormalizeShiftType(string? type)
    {
        string t = (type ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(ShiftTypes, t) >= 0 ? t : "MORNING";
    }

    private static string NormalizeSeverity(string? sev)
    {
        string s = (sev ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(Severities, s) >= 0 ? s : "LOW";
    }

    private static string NormalizeAttendanceStatus(string? status)
    {
        string s = (status ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(AttendanceStatuses, s) >= 0 ? s : "SCHEDULED";
    }

    private static object DbNullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return value.Trim();
    }
}
