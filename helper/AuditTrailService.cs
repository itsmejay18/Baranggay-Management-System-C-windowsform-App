using System;
using System.Text.Json;
using baranggaysystem1.Database;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.helper;

internal static class AuditTrailService
{
    private static readonly object Sync = new();
    private static bool _schemaEnsured;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false
    };

    internal static void Log(
        string module,
        string entityType,
        object? entityId,
        string action,
        object? beforeState = null,
        object? afterState = null,
        string? notes = null,
        int? actionBy = null)
    {
        try
        {
            EnsureSchema();

            using var conn = DBConnection.GetConnection();
            conn.Open();
            LogTransactional(conn, null, module, entityType, entityId, action, beforeState, afterState, notes, actionBy);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("AuditTrailService.Log failed.", ex);
        }
    }

    internal static void LogTransactional(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        string module,
        string entityType,
        object? entityId,
        string action,
        object? beforeState = null,
        object? afterState = null,
        string? notes = null,
        int? actionBy = null)
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Audit logging requires an open connection.");
        }

        EnsureSchema();

        using var cmd = new MySqlCommand(
            @"INSERT INTO audit_trail
                (module, entity_type, entity_id, action, before_json, after_json, notes, action_by)
              VALUES
                (@module, @entityType, @entityId, @action, @before, @after, @notes, @by)",
            connection,
            transaction);

        cmd.Parameters.AddWithValue("@module", module);
        cmd.Parameters.AddWithValue("@entityType", entityType);
        cmd.Parameters.AddWithValue("@entityId", entityId?.ToString());
        cmd.Parameters.AddWithValue("@action", action);
        cmd.Parameters.AddWithValue("@before", ToDbValue(Serialize(beforeState)));
        cmd.Parameters.AddWithValue("@after", ToDbValue(Serialize(afterState)));
        cmd.Parameters.AddWithValue("@notes", ToDbValue(notes));
        cmd.Parameters.AddWithValue("@by", actionBy ?? UserSession.UserId);
        cmd.ExecuteNonQuery();
    }

    internal static void EnsureSchema()
    {
        if (_schemaEnsured)
        {
            return;
        }

        lock (Sync)
        {
            if (_schemaEnsured)
            {
                return;
            }

            DbHelper.ExecuteNonQuery(
                @"CREATE TABLE IF NOT EXISTS audit_trail (
                    audit_id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    module VARCHAR(60) NOT NULL,
                    entity_type VARCHAR(60) NOT NULL,
                    entity_id VARCHAR(64) NULL,
                    action VARCHAR(60) NOT NULL,
                    before_json LONGTEXT NULL,
                    after_json LONGTEXT NULL,
                    notes TEXT NULL,
                    action_by INT NULL,
                    action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    INDEX idx_audit_entity (entity_type, entity_id),
                    INDEX idx_audit_module (module),
                    INDEX idx_audit_action_at (action_at)
                  )");

            _schemaEnsured = true;
        }
    }

    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }

    private static string? Serialize(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
