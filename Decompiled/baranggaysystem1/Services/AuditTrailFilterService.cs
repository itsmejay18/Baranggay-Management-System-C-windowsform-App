using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Enhanced audit trail querying with filtering by user, date range,
/// module, action type, and entity. Provides paginated results for
/// the System Logs page.
/// </summary>
public static class AuditTrailFilterService
{
    /// <summary>
    /// Query audit trail with filters and pagination.
    /// </summary>
    public static AuditTrailQueryResult Query(AuditTrailFilter filter)
    {
        filter ??= new AuditTrailFilter();

        var conditions = new List<string>();
        var parameters = new List<(string Name, object Value)>();

        // Date range filter
        if (filter.FromDate.HasValue)
        {
            conditions.Add("at.action_at >= @fromDate");
            parameters.Add(("@fromDate", filter.FromDate.Value.Date));
        }
        if (filter.ToDate.HasValue)
        {
            conditions.Add("at.action_at < @toDate");
            parameters.Add(("@toDate", filter.ToDate.Value.Date.AddDays(1)));
        }

        // Module filter
        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            conditions.Add("at.module = @module");
            parameters.Add(("@module", filter.Module.Trim()));
        }

        // Action filter
        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            conditions.Add("at.action = @action");
            parameters.Add(("@action", filter.Action.Trim()));
        }

        // User filter
        if (filter.UserId.HasValue && filter.UserId.Value > 0)
        {
            conditions.Add("at.action_by = @userId");
            parameters.Add(("@userId", filter.UserId.Value));
        }

        // Entity type filter
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            conditions.Add("at.entity_type = @entityType");
            parameters.Add(("@entityType", filter.EntityType.Trim()));
        }

        // Entity ID filter
        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            conditions.Add("at.entity_id = @entityId");
            parameters.Add(("@entityId", filter.EntityId.Trim()));
        }

        // Search text (searches notes and entity_id)
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            conditions.Add("(at.notes LIKE @search OR at.entity_id LIKE @search OR at.action LIKE @search)");
            parameters.Add(("@search", $"%{filter.SearchText.Trim()}%"));
        }

        string whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        // Get total count
        int totalCount = GetTotalCount(whereClause, parameters);

        // Get paginated results
        int offset = (filter.Page - 1) * filter.PageSize;
        string sql = $@"
            SELECT at.audit_id, at.module, at.entity_type, at.entity_id, 
                   at.action, at.notes, at.action_by, at.action_at,
                   COALESCE(ua.username, CONCAT('User #', at.action_by)) AS username
            FROM audit_trail at
            LEFT JOIN user_account ua ON ua.user_id = at.action_by
            {whereClause}
            ORDER BY at.action_at DESC, at.audit_id DESC
            LIMIT @limit OFFSET @offset";

        var table = DbHelper.LoadTable(sql, cmd =>
        {
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.Parameters.AddWithValue("@limit", (object)filter.PageSize);
            cmd.Parameters.AddWithValue("@offset", (object)offset);
        });

        return new AuditTrailQueryResult
        {
            Data = table,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        };
    }

    /// <summary>
    /// Get distinct modules for filter dropdown.
    /// </summary>
    public static List<string> GetDistinctModules()
    {
        var modules = new List<string>();
        try
        {
            var table = DbHelper.LoadTable(
                "SELECT DISTINCT module FROM audit_trail ORDER BY module");
            foreach (DataRow row in table.Rows)
            {
                string module = Convert.ToString(row["module"]) ?? "";
                if (!string.IsNullOrWhiteSpace(module))
                    modules.Add(module);
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to load distinct modules.", ex);
        }
        return modules;
    }

    /// <summary>
    /// Get distinct actions for filter dropdown.
    /// </summary>
    public static List<string> GetDistinctActions()
    {
        var actions = new List<string>();
        try
        {
            var table = DbHelper.LoadTable(
                "SELECT DISTINCT action FROM audit_trail ORDER BY action");
            foreach (DataRow row in table.Rows)
            {
                string action = Convert.ToString(row["action"]) ?? "";
                if (!string.IsNullOrWhiteSpace(action))
                    actions.Add(action);
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to load distinct actions.", ex);
        }
        return actions;
    }

    /// <summary>
    /// Get users who have audit entries (for user filter dropdown).
    /// </summary>
    public static List<AuditUserOption> GetAuditUsers()
    {
        var users = new List<AuditUserOption>();
        try
        {
            var table = DbHelper.LoadTable(
                @"SELECT DISTINCT at.action_by, 
                         COALESCE(ua.username, CONCAT('User #', at.action_by)) AS username
                  FROM audit_trail at
                  LEFT JOIN user_account ua ON ua.user_id = at.action_by
                  WHERE at.action_by IS NOT NULL
                  ORDER BY username");
            foreach (DataRow row in table.Rows)
            {
                int userId = Convert.ToInt32(row["action_by"]);
                string username = Convert.ToString(row["username"]) ?? $"User #{userId}";
                users.Add(new AuditUserOption { UserId = userId, Username = username });
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to load audit users.", ex);
        }
        return users;
    }

    /// <summary>
    /// Get audit detail (before/after JSON) for a specific entry.
    /// </summary>
    public static AuditTrailDetail? GetDetail(long auditId)
    {
        if (auditId <= 0) return null;

        try
        {
            var table = DbHelper.LoadTable(
                @"SELECT audit_id, module, entity_type, entity_id, action,
                         before_json, after_json, notes, action_by, action_at
                  FROM audit_trail WHERE audit_id = @id LIMIT 1",
                cmd => cmd.Parameters.AddWithValue("@id", (object)auditId));

            if (table.Rows.Count == 0) return null;

            var row = table.Rows[0];
            return new AuditTrailDetail
            {
                AuditId = Convert.ToInt64(row["audit_id"]),
                Module = Convert.ToString(row["module"]) ?? "",
                EntityType = Convert.ToString(row["entity_type"]) ?? "",
                EntityId = Convert.ToString(row["entity_id"]) ?? "",
                Action = Convert.ToString(row["action"]) ?? "",
                BeforeJson = Convert.ToString(row["before_json"]) ?? "",
                AfterJson = Convert.ToString(row["after_json"]) ?? "",
                Notes = Convert.ToString(row["notes"]) ?? "",
                ActionBy = row["action_by"] == DBNull.Value ? 0 : Convert.ToInt32(row["action_by"]),
                ActionAt = row["action_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["action_at"])
            };
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to load audit detail.", ex);
            return null;
        }
    }

    /// <summary>
    /// Export filtered audit trail to CSV.
    /// </summary>
    public static string ExportToCsv(AuditTrailFilter filter, string outputPath)
    {
        // Remove pagination for export
        var exportFilter = new AuditTrailFilter
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Module = filter.Module,
            Action = filter.Action,
            UserId = filter.UserId,
            EntityType = filter.EntityType,
            EntityId = filter.EntityId,
            SearchText = filter.SearchText,
            Page = 1,
            PageSize = 10000 // Max export
        };

        var result = Query(exportFilter);
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("\"Timestamp\",\"Module\",\"Entity Type\",\"Entity ID\",\"Action\",\"User\",\"Notes\"");

        // Data
        foreach (DataRow row in result.Data.Rows)
        {
            string timestamp = row["action_at"] == DBNull.Value ? "" : Convert.ToDateTime(row["action_at"]).ToString("yyyy-MM-dd HH:mm:ss");
            string module = Escape(Convert.ToString(row["module"]));
            string entityType = Escape(Convert.ToString(row["entity_type"]));
            string entityId = Escape(Convert.ToString(row["entity_id"]));
            string action = Escape(Convert.ToString(row["action"]));
            string username = Escape(Convert.ToString(row["username"]));
            string notes = Escape(Convert.ToString(row["notes"]));

            sb.AppendLine($"\"{timestamp}\",\"{module}\",\"{entityType}\",\"{entityId}\",\"{action}\",\"{username}\",\"{notes}\"");
        }

        string filePath = System.IO.Path.Combine(outputPath,
            $"audit_trail_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);

        return filePath;
    }

    private static int GetTotalCount(string whereClause, List<(string Name, object Value)> parameters)
    {
        try
        {
            string sql = $"SELECT COUNT(*) FROM audit_trail at {whereClause}";
            return DbHelper.ExecuteScalar<int>(sql, cmd =>
            {
                foreach (var (name, value) in parameters)
                {
                    cmd.Parameters.AddWithValue(name, value);
                }
            });
        }
        catch
        {
            return 0;
        }
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"");
    }
}

/// <summary>
/// Filter criteria for audit trail queries.
/// </summary>
public sealed class AuditTrailFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public int? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paginated audit trail query result.
/// </summary>
public sealed class AuditTrailQueryResult
{
    public DataTable Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Full audit trail entry detail (including before/after JSON).
/// </summary>
public sealed class AuditTrailDetail
{
    public long AuditId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = string.Empty;
    public string AfterJson { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int ActionBy { get; set; }
    public DateTime ActionAt { get; set; }
}

/// <summary>
/// User option for audit trail filter dropdown.
/// </summary>
public sealed class AuditUserOption
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public override string ToString() => Username;
}
