using System;
using System.Data;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

/// <summary>
/// Service for managing case timeline entries.
/// </summary>
public static class CaseTimelineService
{
    /// <summary>
    /// Adds a timeline entry for a case (simple overload matching AddEntry).
    /// </summary>
    public static void AddEntry(int caseId, string action, string? details = null, int? userId = null)
    {
        int resolvedUserId = userId ?? UserSession.UserId;

        DbHelper.ExecuteNonQuery(
            @"INSERT INTO case_timeline (case_id, action, details, user_id, created_at)
              VALUES (@caseId, @action, @details, @userId, NOW())",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@caseId", caseId);
                cmd.Parameters.AddWithValue("@action", action);
                cmd.Parameters.AddWithValue("@details", details ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@userId", resolvedUserId);
            });
    }

    /// <summary>
    /// Logs a timeline entry for a case with full parameters.
    /// </summary>
    public static void Log(
        int caseId,
        string eventType,
        string summary,
        string? details,
        string? fromStatus,
        string? toStatus,
        int? userId = null)
    {
        int resolvedUserId = userId ?? UserSession.UserId;

        DbHelper.ExecuteNonQuery(
            @"INSERT INTO case_timeline (case_id, action, summary, details, from_status, to_status, user_id, created_at)
              VALUES (@caseId, @action, @summary, @details, @fromStatus, @toStatus, @userId, NOW())",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@caseId", caseId);
                cmd.Parameters.AddWithValue("@action", eventType);
                cmd.Parameters.AddWithValue("@summary", summary ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@details", details ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@fromStatus", fromStatus ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@toStatus", toStatus ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@userId", resolvedUserId);
            });
    }

    /// <summary>
    /// Logs a timeline entry within an existing transaction.
    /// </summary>
    public static void LogTransactional(
        MySqlConnection conn,
        MySqlTransaction tx,
        int caseId,
        string eventType,
        string summary,
        string? details,
        string? fromStatus,
        string? toStatus,
        int? userId = null)
    {
        int resolvedUserId = userId ?? UserSession.UserId;

        using var cmd = new MySqlCommand(
            @"INSERT INTO case_timeline (case_id, action, summary, details, from_status, to_status, user_id, created_at)
              VALUES (@caseId, @action, @summary, @details, @fromStatus, @toStatus, @userId, NOW())",
            conn, tx);

        cmd.Parameters.AddWithValue("@caseId", caseId);
        cmd.Parameters.AddWithValue("@action", eventType);
        cmd.Parameters.AddWithValue("@summary", summary ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@details", details ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@fromStatus", fromStatus ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@toStatus", toStatus ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@userId", resolvedUserId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Gets the timeline entries for a case, ordered by most recent first.
    /// </summary>
    public static DataTable GetTimeline(int caseId)
    {
        return DbHelper.LoadTable(
            @"SELECT ct.timeline_id, ct.case_id, ct.action, ct.summary, ct.details,
                     ct.from_status, ct.to_status, ct.user_id, ct.created_at, u.username
              FROM case_timeline ct
              LEFT JOIN users u ON u.user_id = ct.user_id
              WHERE ct.case_id = @caseId
              ORDER BY ct.created_at DESC",
            cmd => cmd.Parameters.AddWithValue("@caseId", caseId));
    }

    /// <summary>
    /// Loads timeline entries for a case with an optional limit.
    /// </summary>
    public static DataTable LoadTimeline(int caseId, int limit = 100)
    {
        return DbHelper.LoadTable(
            $@"SELECT ct.timeline_id, ct.case_id, ct.action, ct.summary, ct.details,
                      ct.from_status, ct.to_status, ct.user_id, ct.created_at, u.username
               FROM case_timeline ct
               LEFT JOIN users u ON u.user_id = ct.user_id
               WHERE ct.case_id = @caseId
               ORDER BY ct.created_at DESC
               LIMIT {limit}",
            cmd => cmd.Parameters.AddWithValue("@caseId", caseId));
    }
}
