using System;
using System.Data;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal static class CaseTimelineService
{
    private const int MaxEventTypeLength = 50;
    private const int MaxTitleLength = 150;
    private const int MaxStatusLength = 30;
    private const int MaxDetailsLength = 8000;

    public static void Log(int caseId, string eventType, string title, string? details = null, string? fromStatus = null, string? toStatus = null, int? userId = null)
    {
        if (caseId <= 0)
        {
            return;
        }

        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();
            using var cmd = BuildInsertCommand(conn, null, caseId, eventType, title, details, fromStatus, toStatus, userId);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Unable to write blotter timeline entry.", ex);
        }
    }

    public static void LogTransactional(
        MySqlConnection conn,
        MySqlTransaction tx,
        int caseId,
        string eventType,
        string title,
        string? details = null,
        string? fromStatus = null,
        string? toStatus = null,
        int? userId = null)
    {
        if (caseId <= 0)
        {
            return;
        }

        try
        {
            using var cmd = BuildInsertCommand(conn, tx, caseId, eventType, title, details, fromStatus, toStatus, userId);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            // Best-effort logging; should not block the main workflow.
            AppLogger.LogWarning("Unable to write blotter timeline entry (transactional).", ex);
        }
    }

    public static DataTable LoadTimeline(int caseId, int limit = 80)
    {
        if (caseId <= 0)
        {
            return new DataTable();
        }

        int safeLimit = Math.Clamp(limit, 5, 200);
        string sql = $@"SELECT ct.timeline_id,
                               ct.created_at,
                               ct.event_type,
                               ct.event_title,
                               ct.event_details,
                               ct.from_status,
                               ct.to_status,
                               u.username AS created_by
                        FROM case_timeline ct
                        LEFT JOIN user_account u ON u.user_id = ct.created_by_user_id
                        WHERE ct.case_id = @id
                        ORDER BY ct.created_at DESC, ct.timeline_id DESC
                        LIMIT {safeLimit}";

        return DbHelper.LoadTable(sql, cmd => cmd.Parameters.AddWithValue("@id", caseId));
    }

    private static MySqlCommand BuildInsertCommand(
        MySqlConnection conn,
        MySqlTransaction? tx,
        int caseId,
        string eventType,
        string title,
        string? details,
        string? fromStatus,
        string? toStatus,
        int? userId)
    {
        string safeEventType = Normalize(eventType, MaxEventTypeLength, fallback: "EVENT");
        string safeTitle = Normalize(title, MaxTitleLength, fallback: "Update");
        string safeDetails = Normalize(details, MaxDetailsLength, fallback: string.Empty);
        string safeFrom = Normalize(fromStatus, MaxStatusLength, fallback: string.Empty);
        string safeTo = Normalize(toStatus, MaxStatusLength, fallback: string.Empty);

        var cmd = new MySqlCommand(
            @"INSERT INTO case_timeline
                (case_id, event_type, event_title, event_details, from_status, to_status, created_by_user_id)
              VALUES
                (@case_id, @event_type, @event_title, @event_details, @from_status, @to_status, @created_by)",
            conn);
        cmd.Transaction = tx;
        cmd.Parameters.AddWithValue("@case_id", caseId);
        cmd.Parameters.AddWithValue("@event_type", safeEventType);
        cmd.Parameters.AddWithValue("@event_title", safeTitle);
        cmd.Parameters.AddWithValue("@event_details", string.IsNullOrWhiteSpace(safeDetails) ? (object)DBNull.Value : safeDetails);
        cmd.Parameters.AddWithValue("@from_status", string.IsNullOrWhiteSpace(safeFrom) ? (object)DBNull.Value : safeFrom);
        cmd.Parameters.AddWithValue("@to_status", string.IsNullOrWhiteSpace(safeTo) ? (object)DBNull.Value : safeTo);
        cmd.Parameters.AddWithValue("@created_by", userId.HasValue ? userId.Value : (object)DBNull.Value);
        return cmd;
    }

    private static string Normalize(string? value, int maxLen, string fallback)
    {
        string trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return fallback;
        }

        return trimmed.Length <= maxLen ? trimmed : trimmed.Substring(0, maxLen);
    }
}

