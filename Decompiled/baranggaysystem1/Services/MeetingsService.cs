using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Manages barangay meetings, attendance, resolutions, and ordinances.
/// </summary>
internal sealed class MeetingsService
{
    public static readonly string[] MeetingTypes = new[] { "REGULAR", "SPECIAL", "EMERGENCY", "COMMITTEE" };
    public static readonly string[] MeetingStatuses = new[] { "SCHEDULED", "ONGOING", "COMPLETED", "CANCELLED" };
    public static readonly string[] DocumentTypes = new[] { "RESOLUTION", "ORDINANCE", "MEMORANDUM" };
    public static readonly string[] DocumentStatuses = new[] { "DRAFT", "PENDING", "APPROVED", "ARCHIVED" };

    // ============================================================
    // MEETINGS
    // ============================================================

    public async Task<DataTable> LoadMeetingsAsync(string? searchText = null, string? statusFilter = null,
        string? meetingType = null)
    {
        string search = (searchText ?? string.Empty).Trim();
        string like = "%" + search + "%";
        string sql = @"SELECT meeting_id,
                              title,
                              meeting_type,
                              scheduled_at,
                              COALESCE(venue, '') AS venue,
                              status,
                              attendance_count,
                              quorum_reached
                         FROM barangay_meeting
                        WHERE (@search = '' OR title LIKE @like OR COALESCE(venue, '') LIKE @like
                               OR COALESCE(agenda, '') LIKE @like OR meeting_type LIKE @like)
                          AND (@status = '' OR status = @status)
                          AND (@type = '' OR meeting_type = @type)
                        ORDER BY scheduled_at DESC";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@search", (object)search);
            cmd.Parameters.AddWithValue("@like", (object)like);
            cmd.Parameters.AddWithValue("@status", (object)(statusFilter ?? string.Empty));
            cmd.Parameters.AddWithValue("@type", (object)(meetingType ?? string.Empty));
        }).ConfigureAwait(false);
    }

    public async Task<DataTable> GetMeetingAsync(int meetingId)
    {
        return await DatabaseManagerAsync.LoadTableAsync(
            "SELECT * FROM barangay_meeting WHERE meeting_id = @id LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@id", (object)meetingId)).ConfigureAwait(false);
    }

    public async Task<int> CreateMeetingAsync(string title, string meetingType, DateTime scheduledAt,
        string? venue, string? agenda)
    {
        const string sql = @"INSERT INTO barangay_meeting
                               (title, meeting_type, scheduled_at, venue, agenda, status)
                             VALUES
                               (@title, @type, @scheduled, @venue, @agenda, 'SCHEDULED')";
        int newId = 0;
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@title", (object)(title ?? "").Trim());
            cmd.Parameters.AddWithValue("@type", (object)NormalizeType(meetingType));
            cmd.Parameters.AddWithValue("@scheduled", (object)scheduledAt);
            cmd.Parameters.AddWithValue("@venue", DbNullIfEmpty(venue));
            cmd.Parameters.AddWithValue("@agenda", DbNullIfEmpty(agenda));
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            "SELECT meeting_id FROM barangay_meeting WHERE title = @title ORDER BY meeting_id DESC LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@title", (object)(title ?? "").Trim())).ConfigureAwait(false);
        if (idTable.Rows.Count > 0)
            newId = Convert.ToInt32(idTable.Rows[0]["meeting_id"]);

        AuditTrailService.Log("Meetings", "barangay_meeting", newId, "CREATE", null,
            new { title, meetingType, scheduledAt }, "Meeting scheduled.");
        return newId;
    }

    public async Task UpdateMeetingAsync(int meetingId, string title, string meetingType,
        DateTime scheduledAt, string? venue, string? agenda, string? minutes, string status,
        int attendanceCount, bool quorumReached)
    {
        const string sql = @"UPDATE barangay_meeting
                                SET title = @title,
                                    meeting_type = @type,
                                    scheduled_at = @scheduled,
                                    venue = @venue,
                                    agenda = @agenda,
                                    minutes = @minutes,
                                    status = @status,
                                    attendance_count = @attendance,
                                    quorum_reached = @quorum
                              WHERE meeting_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)meetingId);
            cmd.Parameters.AddWithValue("@title", (object)(title ?? "").Trim());
            cmd.Parameters.AddWithValue("@type", (object)NormalizeType(meetingType));
            cmd.Parameters.AddWithValue("@scheduled", (object)scheduledAt);
            cmd.Parameters.AddWithValue("@venue", DbNullIfEmpty(venue));
            cmd.Parameters.AddWithValue("@agenda", DbNullIfEmpty(agenda));
            cmd.Parameters.AddWithValue("@minutes", DbNullIfEmpty(minutes));
            cmd.Parameters.AddWithValue("@status", (object)NormalizeStatus(status));
            cmd.Parameters.AddWithValue("@attendance", (object)Math.Max(0, attendanceCount));
            cmd.Parameters.AddWithValue("@quorum", (object)(quorumReached ? 1 : 0));
        }).ConfigureAwait(false);

        AuditTrailService.Log("Meetings", "barangay_meeting", meetingId, "UPDATE", null,
            new { title, status }, "Meeting updated.");
    }

    public async Task DeleteMeetingAsync(int meetingId)
    {
        await DatabaseManagerAsync.ExecuteNonQueryAsync(
            "DELETE FROM barangay_meeting WHERE meeting_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", (object)meetingId)).ConfigureAwait(false);
        AuditTrailService.Log("Meetings", "barangay_meeting", meetingId, "DELETE", null, null, "Meeting deleted.");
    }

    // ============================================================
    // RESOLUTIONS / ORDINANCES
    // ============================================================

    public async Task<DataTable> LoadResolutionsAsync(string? searchText = null,
        string? documentType = null, string? statusFilter = null)
    {
        string search = (searchText ?? string.Empty).Trim();
        string like = "%" + search + "%";
        string sql = @"SELECT resolution_id,
                              document_type,
                              document_number,
                              series_year,
                              title,
                              status,
                              effectivity_date,
                              COALESCE(authored_by, '') AS authored_by,
                              created_at
                         FROM barangay_resolution
                        WHERE (@search = '' OR title LIKE @like OR document_number LIKE @like)
                          AND (@type = '' OR document_type = @type)
                          AND (@status = '' OR status = @status)
                        ORDER BY series_year DESC, document_number DESC";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@search", (object)search);
            cmd.Parameters.AddWithValue("@like", (object)like);
            cmd.Parameters.AddWithValue("@type", (object)(documentType ?? string.Empty));
            cmd.Parameters.AddWithValue("@status", (object)(statusFilter ?? string.Empty));
        }).ConfigureAwait(false);
    }

    public async Task<DataTable> GetResolutionAsync(int resolutionId)
    {
        return await DatabaseManagerAsync.LoadTableAsync(
            "SELECT * FROM barangay_resolution WHERE resolution_id = @id LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@id", (object)resolutionId)).ConfigureAwait(false);
    }

    public async Task<int> CreateResolutionAsync(string documentType, string documentNumber,
        int seriesYear, string title, string? description, string? fullText,
        DateTime? effectivityDate, string? authoredBy, int? meetingId)
    {
        const string sql = @"INSERT INTO barangay_resolution
                               (document_type, document_number, series_year, title, description,
                                full_text, effectivity_date, authored_by, meeting_id, status)
                             VALUES
                               (@type, @number, @year, @title, @desc, @fulltext, @eff, @author, @meeting, 'DRAFT')";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@type", (object)NormalizeDocumentType(documentType));
            cmd.Parameters.AddWithValue("@number", (object)(documentNumber ?? "").Trim());
            cmd.Parameters.AddWithValue("@year", (object)seriesYear);
            cmd.Parameters.AddWithValue("@title", (object)(title ?? "").Trim());
            cmd.Parameters.AddWithValue("@desc", DbNullIfEmpty(description));
            cmd.Parameters.AddWithValue("@fulltext", DbNullIfEmpty(fullText));
            cmd.Parameters.AddWithValue("@eff", effectivityDate.HasValue ? (object)effectivityDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@author", DbNullIfEmpty(authoredBy));
            cmd.Parameters.AddWithValue("@meeting", meetingId.HasValue && meetingId.Value > 0
                ? (object)meetingId.Value : DBNull.Value);
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            "SELECT resolution_id FROM barangay_resolution WHERE document_number = @num AND series_year = @year ORDER BY resolution_id DESC LIMIT 1",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@num", (object)(documentNumber ?? "").Trim());
                cmd.Parameters.AddWithValue("@year", (object)seriesYear);
            }).ConfigureAwait(false);
        int id = idTable.Rows.Count > 0 ? Convert.ToInt32(idTable.Rows[0]["resolution_id"]) : 0;
        AuditTrailService.Log("Resolutions", "barangay_resolution", id, "CREATE", null,
            new { documentType, documentNumber, seriesYear, title }, "Resolution created.");
        return id;
    }

    public async Task UpdateResolutionAsync(int resolutionId, string documentType, string documentNumber,
        int seriesYear, string title, string? description, string? fullText,
        DateTime? effectivityDate, string? authoredBy, string status)
    {
        const string sql = @"UPDATE barangay_resolution
                                SET document_type = @type,
                                    document_number = @number,
                                    series_year = @year,
                                    title = @title,
                                    description = @desc,
                                    full_text = @fulltext,
                                    effectivity_date = @eff,
                                    authored_by = @author,
                                    status = @status
                              WHERE resolution_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)resolutionId);
            cmd.Parameters.AddWithValue("@type", (object)NormalizeDocumentType(documentType));
            cmd.Parameters.AddWithValue("@number", (object)(documentNumber ?? "").Trim());
            cmd.Parameters.AddWithValue("@year", (object)seriesYear);
            cmd.Parameters.AddWithValue("@title", (object)(title ?? "").Trim());
            cmd.Parameters.AddWithValue("@desc", DbNullIfEmpty(description));
            cmd.Parameters.AddWithValue("@fulltext", DbNullIfEmpty(fullText));
            cmd.Parameters.AddWithValue("@eff", effectivityDate.HasValue ? (object)effectivityDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@author", DbNullIfEmpty(authoredBy));
            cmd.Parameters.AddWithValue("@status", (object)NormalizeDocumentStatus(status));
        }).ConfigureAwait(false);

        AuditTrailService.Log("Resolutions", "barangay_resolution", resolutionId, "UPDATE", null,
            new { documentNumber, status }, "Resolution updated.");
    }

    public async Task DeleteResolutionAsync(int resolutionId)
    {
        await DatabaseManagerAsync.ExecuteNonQueryAsync(
            "DELETE FROM barangay_resolution WHERE resolution_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", (object)resolutionId)).ConfigureAwait(false);
        AuditTrailService.Log("Resolutions", "barangay_resolution", resolutionId, "DELETE", null, null, "Resolution deleted.");
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static string NormalizeType(string? type)
    {
        string t = (type ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(MeetingTypes, t) >= 0 ? t : "REGULAR";
    }

    private static string NormalizeStatus(string? status)
    {
        string s = (status ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(MeetingStatuses, s) >= 0 ? s : "SCHEDULED";
    }

    private static string NormalizeDocumentType(string? type)
    {
        string t = (type ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(DocumentTypes, t) >= 0 ? t : "RESOLUTION";
    }

    private static string NormalizeDocumentStatus(string? status)
    {
        string s = (status ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(DocumentStatuses, s) >= 0 ? s : "DRAFT";
    }

    private static object DbNullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return value.Trim();
    }
}
