using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal static class SchemaGuard
{
    public static void EnsureDatabaseReady()
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();

        MigrationRunner.ApplyPendingMigrations(conn);

        SchemaBootstrap.EnsureCoreDefaults(conn);
        EnsureAppCompatColumns(conn);
        EnsureAppCompatTables(conn);
        EnsureAppCompatIndexes(conn);
        SchemaBootstrap.EnsureCoreDefaults(conn);
    }

    public static StartupHealthReport RunStartupHealthChecks()
    {
        var report = new StartupHealthReport();

        MySqlConnection conn;
        try
        {
            conn = DBConnection.GetConnection();
            conn.Open();
            report.Add("DB connectivity", StartupHealthLevel.Ok, "Connected successfully.");
        }
        catch (Exception ex)
        {
            report.Add("DB connectivity", StartupHealthLevel.Critical, ex.Message);
            return report;
        }

        using (conn)
        {
            try
            {
                if (!MigrationRunner.HasMigrationFiles())
                {
                    report.Add("Migration files", StartupHealthLevel.Warning,
                        "Migration SQL files were not found in output/project path.");
                }
                else
                {
                    report.Add("Migration files", StartupHealthLevel.Ok, "Migration SQL files found.");
                }
            }
            catch (Exception ex)
            {
                report.Add("Migration files", StartupHealthLevel.Warning, ex.Message);
            }

            try
            {
                string[] requiredTables =
                {
                    "barangay",
                    "purok_sitio",
                    "household",
                    "resident",
                    "role",
                    "user_account",
                    "document_type",
                    "document_request",
                    "case_record",
                    "record_attachment",
                    "outbound_notification",
                    "resident_transfer_history",
                    "role_permission",
                    "backup_run",
                    "schema_migrations"
                };

                var missingTables = requiredTables.Where(t => !TableExists(conn, t)).ToList();
                if (missingTables.Count == 0)
                {
                    report.Add("Required tables", StartupHealthLevel.Ok, "All required tables are present.");
                }
                else
                {
                    report.Add("Required tables", StartupHealthLevel.Critical,
                        "Missing: " + FormatList(missingTables));
                }
            }
            catch (Exception ex)
            {
                report.Add("Required tables", StartupHealthLevel.Critical, ex.Message);
            }

            try
            {
                var requiredColumns = new (string Table, string Column)[]
                {
                    ("resident", "is_deleted"),
                    ("resident", "photo"),
                    ("document_request", "document_no"),
                    ("document_request", "verification_token"),
                    ("document_request", "or_number"),
                    ("document_request", "fee"),
                    ("document_request", "expires_at"),
                    ("document_request", "renewed_from_request_id"),
                    ("case_record", "respondent_resident_id"),
                    ("case_record", "respondent_name"),
                    ("case_record", "ai_summary"),
                    ("purok_sitio", "latitude"),
                    ("purok_sitio", "longitude"),
                    ("user_account", "photo_url"),
                    ("backup_run", "status"),
                    ("backup_run", "started_at"),
                    ("backup_run", "backup_type"),
                    ("backup_run", "base_started_at"),
                    ("backup_run", "base_backup_run_id"),
                };

                var missingColumns = new List<string>();
                foreach ((string table, string column) in requiredColumns)
                {
                    if (!TableExists(conn, table))
                    {
                        missingColumns.Add($"{table}.{column} (table missing)");
                        continue;
                    }

                    if (!ColumnExists(conn, table, column))
                    {
                        missingColumns.Add($"{table}.{column}");
                    }
                }

                if (missingColumns.Count == 0)
                {
                    report.Add("Required columns", StartupHealthLevel.Ok, "All required columns are present.");
                }
                else
                {
                    report.Add("Required columns", StartupHealthLevel.Critical,
                        "Missing: " + FormatList(missingColumns));
                }
            }
            catch (Exception ex)
            {
                report.Add("Required columns", StartupHealthLevel.Critical, ex.Message);
            }

            try
            {
                var pendingAuto = MigrationRunner.GetPendingAutoMigrationNames(conn);
                if (pendingAuto.Count == 0)
                {
                    report.Add("Pending auto migrations", StartupHealthLevel.Ok, "No pending auto migrations.");
                }
                else
                {
                    report.Add("Pending auto migrations", StartupHealthLevel.Warning,
                        "Pending: " + FormatList(pendingAuto));
                }
            }
            catch (Exception ex)
            {
                report.Add("Pending auto migrations", StartupHealthLevel.Warning, ex.Message);
            }

            try
            {
                var pendingManual = MigrationRunner.GetPendingManualMigrationNames(conn);
                if (pendingManual.Count == 0)
                {
                    report.Add("Pending manual migrations", StartupHealthLevel.Ok, "No pending manual migrations.");
                }
                else
                {
                    report.Add("Pending manual migrations", StartupHealthLevel.Warning,
                        "Needs manual run: " + FormatList(pendingManual));
                }
            }
            catch (Exception ex)
            {
                report.Add("Pending manual migrations", StartupHealthLevel.Warning, ex.Message);
            }
        }
        return report;
    }

    private static string FormatList(IReadOnlyList<string> values, int max = 6)
    {
        if (values.Count <= max)
        {
            return string.Join(", ", values);
        }

        int extra = values.Count - max;
        return string.Join(", ", values.Take(max)) + $", +{extra} more";
    }

    private static void EnsureAppCompatColumns(MySqlConnection conn)
    {
        var columns = new (string Table, string Column, string Sql)[]
        {
            ("resident", "photo", "ALTER TABLE resident ADD COLUMN photo LONGBLOB NULL"),
            ("resident", "is_deleted", "ALTER TABLE resident ADD COLUMN is_deleted TINYINT(1) NOT NULL DEFAULT 0"),
            ("resident", "deleted_at", "ALTER TABLE resident ADD COLUMN deleted_at DATETIME NULL"),
            ("resident", "deleted_by_user_id", "ALTER TABLE resident ADD COLUMN deleted_by_user_id INT NULL"),
            ("resident", "delete_reason", "ALTER TABLE resident ADD COLUMN delete_reason VARCHAR(255) NULL"),

            ("user_account", "first_name", "ALTER TABLE user_account ADD COLUMN first_name VARCHAR(100) NULL"),
            ("user_account", "middle_name", "ALTER TABLE user_account ADD COLUMN middle_name VARCHAR(100) NULL"),
            ("user_account", "last_name", "ALTER TABLE user_account ADD COLUMN last_name VARCHAR(100) NULL"),
            ("user_account", "position", "ALTER TABLE user_account ADD COLUMN position VARCHAR(100) NULL"),
            ("user_account", "department", "ALTER TABLE user_account ADD COLUMN department VARCHAR(100) NULL"),
            ("user_account", "last_project", "ALTER TABLE user_account ADD COLUMN last_project VARCHAR(255) NULL"),
            ("user_account", "photo_url", "ALTER TABLE user_account ADD COLUMN photo_url VARCHAR(255) NULL"),

            ("document_type", "validity_days", "ALTER TABLE document_type ADD COLUMN validity_days INT NULL"),
            ("document_type", "renewal_reminder_days", "ALTER TABLE document_type ADD COLUMN renewal_reminder_days INT NULL"),

            ("document_request", "document_no", "ALTER TABLE document_request ADD COLUMN document_no VARCHAR(50) NULL"),
            ("document_request", "fee", "ALTER TABLE document_request ADD COLUMN fee DECIMAL(10,2) DEFAULT 0"),
            ("document_request", "or_number", "ALTER TABLE document_request ADD COLUMN or_number VARCHAR(100) NULL"),
            ("document_request", "business_name", "ALTER TABLE document_request ADD COLUMN business_name VARCHAR(255) NULL"),
            ("document_request", "business_nature", "ALTER TABLE document_request ADD COLUMN business_nature VARCHAR(255) NULL"),
            ("document_request", "print_count", "ALTER TABLE document_request ADD COLUMN print_count INT NOT NULL DEFAULT 0"),
            ("document_request", "last_printed_at", "ALTER TABLE document_request ADD COLUMN last_printed_at DATETIME NULL"),
            ("document_request", "remarks", "ALTER TABLE document_request ADD COLUMN remarks TEXT NULL"),
            ("document_request", "verification_token", "ALTER TABLE document_request ADD COLUMN verification_token VARCHAR(32) NULL"),
            ("document_request", "verification_token_created_at", "ALTER TABLE document_request ADD COLUMN verification_token_created_at DATETIME NULL"),
            ("document_request", "expires_at", "ALTER TABLE document_request ADD COLUMN expires_at DATETIME NULL"),
            ("document_request", "renewed_from_request_id", "ALTER TABLE document_request ADD COLUMN renewed_from_request_id INT NULL"),
            ("document_request", "renewal_notified_at", "ALTER TABLE document_request ADD COLUMN renewal_notified_at DATETIME NULL"),
            ("document_request", "release_notified_at", "ALTER TABLE document_request ADD COLUMN release_notified_at DATETIME NULL"),

            ("purok_sitio", "latitude", "ALTER TABLE purok_sitio ADD COLUMN latitude DECIMAL(10,8) NULL"),
            ("purok_sitio", "longitude", "ALTER TABLE purok_sitio ADD COLUMN longitude DECIMAL(11,8) NULL"),

            ("case_record", "complainant_id", "ALTER TABLE case_record ADD COLUMN complainant_id INT NULL"),
            ("case_record", "respondent_resident_id", "ALTER TABLE case_record ADD COLUMN respondent_resident_id INT NULL"),
            ("case_record", "respondent_name", "ALTER TABLE case_record ADD COLUMN respondent_name VARCHAR(255) NULL"),
            ("case_record", "incident_type", "ALTER TABLE case_record ADD COLUMN incident_type VARCHAR(100) NULL"),
            ("case_record", "incident_time", "ALTER TABLE case_record ADD COLUMN incident_time TIME NULL"),
            ("case_record", "witness_names", "ALTER TABLE case_record ADD COLUMN witness_names TEXT NULL"),
            ("case_record", "action_taken", "ALTER TABLE case_record ADD COLUMN action_taken TEXT NULL"),
            ("case_record", "resolution_details", "ALTER TABLE case_record ADD COLUMN resolution_details TEXT NULL"),
            ("case_record", "referral_destination", "ALTER TABLE case_record ADD COLUMN referral_destination VARCHAR(255) NULL"),
            ("case_record", "closure_notes", "ALTER TABLE case_record ADD COLUMN closure_notes TEXT NULL"),
            ("case_record", "closed_at", "ALTER TABLE case_record ADD COLUMN closed_at DATETIME NULL"),
            ("case_record", "closed_by_user_id", "ALTER TABLE case_record ADD COLUMN closed_by_user_id INT NULL"),
            ("case_record", "incident_details", "ALTER TABLE case_record ADD COLUMN incident_details TEXT NULL"),
            ("case_record", "recorded_by", "ALTER TABLE case_record ADD COLUMN recorded_by INT NULL"),
            ("case_record", "ai_summary", "ALTER TABLE case_record ADD COLUMN ai_summary TEXT NULL"),
            ("case_record", "ai_key_points", "ALTER TABLE case_record ADD COLUMN ai_key_points TEXT NULL"),
            ("case_record", "ai_category", "ALTER TABLE case_record ADD COLUMN ai_category VARCHAR(150) NULL"),
            ("case_record", "ai_category_confidence", "ALTER TABLE case_record ADD COLUMN ai_category_confidence DECIMAL(5,4) NULL"),
            ("case_record", "ai_risk_level", "ALTER TABLE case_record ADD COLUMN ai_risk_level VARCHAR(20) NULL"),
            ("case_record", "ai_risk_score", "ALTER TABLE case_record ADD COLUMN ai_risk_score INT NULL"),
            ("case_record", "ai_risk_reasons", "ALTER TABLE case_record ADD COLUMN ai_risk_reasons TEXT NULL"),
            ("case_record", "ai_entities", "ALTER TABLE case_record ADD COLUMN ai_entities TEXT NULL"),
            ("case_record", "ai_recommended_next_action", "ALTER TABLE case_record ADD COLUMN ai_recommended_next_action TEXT NULL"),
            ("case_record", "ai_model", "ALTER TABLE case_record ADD COLUMN ai_model VARCHAR(100) NULL"),
            ("case_record", "ai_processed_at", "ALTER TABLE case_record ADD COLUMN ai_processed_at DATETIME NULL"),

            ("backup_run", "backup_type", "ALTER TABLE backup_run ADD COLUMN backup_type ENUM('FULL','INCREMENTAL','DIFFERENTIAL') NOT NULL DEFAULT 'FULL'"),
            ("backup_run", "base_started_at", "ALTER TABLE backup_run ADD COLUMN base_started_at DATETIME NULL"),
            ("backup_run", "base_backup_run_id", "ALTER TABLE backup_run ADD COLUMN base_backup_run_id INT NULL")
        };

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            tables.Add(column.Table);
        }

        foreach (var table in tables)
        {
            if (!TableExists(conn, table))
            {
                throw new InvalidOperationException($"Missing required table '{table}'.");
            }
        }

        foreach (var column in columns)
        {
            if (ColumnExists(conn, column.Table, column.Column))
            {
                continue;
            }

            using var cmd = new MySqlCommand(column.Sql, conn);
            cmd.ExecuteNonQuery();
        }
    }

    private static void EnsureAppCompatTables(MySqlConnection conn)
    {
        if (!TableExists(conn, "document_payment"))
        {
            if (!TableExists(conn, "document_request") || !TableExists(conn, "user_account"))
            {
                throw new InvalidOperationException("Missing required tables for document payments.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE document_payment (
                    payment_id INT AUTO_INCREMENT PRIMARY KEY,
                    doc_request_id INT NOT NULL,
                    amount DECIMAL(10,2),
                    or_no VARCHAR(100),
                    payment_method ENUM('Cash','GCash','Bank'),
                    paid_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    received_by_user_id INT,
                    FOREIGN KEY (doc_request_id) REFERENCES document_request(doc_request_id) ON DELETE CASCADE,
                    FOREIGN KEY (received_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "case_timeline"))
        {
            if (!TableExists(conn, "case_record") || !TableExists(conn, "user_account"))
            {
                throw new InvalidOperationException("Missing required tables for case timeline.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE case_timeline (
                    timeline_id INT AUTO_INCREMENT PRIMARY KEY,
                    case_id INT NOT NULL,
                    event_type VARCHAR(50) NOT NULL,
                    event_title VARCHAR(150) NOT NULL,
                    event_details TEXT NULL,
                    from_status VARCHAR(30) NULL,
                    to_status VARCHAR(30) NULL,
                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    created_by_user_id INT NULL,
                    INDEX idx_case_timeline_case (case_id),
                    INDEX idx_case_timeline_created_at (created_at),
                    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "case_hearing"))
        {
            if (!TableExists(conn, "case_record") || !TableExists(conn, "user_account"))
            {
                throw new InvalidOperationException("Missing required tables for case hearings.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE case_hearing (
                    hearing_id INT AUTO_INCREMENT PRIMARY KEY,
                    case_id INT NOT NULL,
                    schedule_at DATETIME,
                    venue VARCHAR(150),
                    status ENUM('SCHEDULED','DONE','RESET','CANCELLED') DEFAULT 'SCHEDULED',
                    minutes TEXT,
                    result TEXT,
                    created_by_user_id INT NULL,
                    INDEX idx_case_hearing_case (case_id),
                    INDEX idx_case_hearing_status (status),
                    INDEX idx_case_hearing_schedule (schedule_at),
                    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "role_permission"))
        {
            if (!TableExists(conn, "role"))
            {
                throw new InvalidOperationException("Missing required role table for permissions.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE role_permission (
                    role_id INT NOT NULL,
                    permission_key VARCHAR(100) NOT NULL,
                    is_allowed TINYINT(1) NOT NULL DEFAULT 0,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (role_id, permission_key),
                    FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "record_attachment"))
        {
            if (!TableExists(conn, "user_account"))
            {
                throw new InvalidOperationException("Missing required user_account table for attachments.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE record_attachment (
                    attachment_id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    entity_type ENUM('RESIDENT','CASE','CERTIFICATE') NOT NULL,
                    entity_id INT NOT NULL,
                    file_name VARCHAR(255) NOT NULL,
                    file_ext VARCHAR(20) NULL,
                    mime_type VARCHAR(120) NULL,
                    file_size_bytes BIGINT NOT NULL DEFAULT 0,
                    file_hash CHAR(64) NULL,
                    file_blob LONGBLOB NOT NULL,
                    notes VARCHAR(255) NULL,
                    uploaded_by_user_id INT NULL,
                    uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    INDEX idx_attachment_entity (entity_type, entity_id, uploaded_at),
                    INDEX idx_attachment_hash (file_hash),
                    FOREIGN KEY (uploaded_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "outbound_notification"))
        {
            if (!TableExists(conn, "user_account"))
            {
                throw new InvalidOperationException("Missing required user_account table for notifications.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE outbound_notification (
                    notification_id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    dedupe_key VARCHAR(160) NULL,
                    channel ENUM('SMS','EMAIL') NOT NULL,
                    recipient VARCHAR(200) NOT NULL,
                    subject VARCHAR(180) NULL,
                    message TEXT NOT NULL,
                    status ENUM('PENDING','SENT','FAILED','SKIPPED') NOT NULL DEFAULT 'PENDING',
                    source_module VARCHAR(40) NULL,
                    source_record_id INT NULL,
                    template_key VARCHAR(80) NULL,
                    scheduled_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    sent_at DATETIME NULL,
                    attempts INT NOT NULL DEFAULT 0,
                    last_error VARCHAR(500) NULL,
                    created_by_user_id INT NULL,
                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    UNIQUE KEY ux_outbound_notification_dedupe (dedupe_key),
                    INDEX idx_outbound_notification_status (status, scheduled_at),
                    INDEX idx_outbound_notification_source (source_module, source_record_id),
                    INDEX idx_outbound_notification_channel (channel, status),
                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "outbound_notification_attempt"))
        {
            if (!TableExists(conn, "outbound_notification"))
            {
                throw new InvalidOperationException("Missing required outbound_notification table.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE outbound_notification_attempt (
                    attempt_id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    notification_id BIGINT NOT NULL,
                    attempt_no INT NOT NULL,
                    attempted_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    success TINYINT(1) NOT NULL DEFAULT 0,
                    response_code VARCHAR(64) NULL,
                    response_message VARCHAR(500) NULL,
                    INDEX idx_notification_attempt_notification (notification_id, attempted_at),
                    FOREIGN KEY (notification_id) REFERENCES outbound_notification(notification_id) ON DELETE CASCADE
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "resident_transfer_history"))
        {
            if (!TableExists(conn, "resident") || !TableExists(conn, "user_account"))
            {
                throw new InvalidOperationException("Missing required resident/user tables for transfer history.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE resident_transfer_history (
                    transfer_id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    resident_id INT NOT NULL,
                    old_purok_id INT NULL,
                    old_household_id INT NULL,
                    old_address VARCHAR(255) NULL,
                    new_purok_id INT NULL,
                    new_household_id INT NULL,
                    new_address VARCHAR(255) NULL,
                    transfer_reason VARCHAR(255) NULL,
                    transferred_by_user_id INT NULL,
                    transferred_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    INDEX idx_transfer_history_resident (resident_id, transferred_at),
                    INDEX idx_transfer_history_old_location (old_purok_id, old_household_id),
                    INDEX idx_transfer_history_new_location (new_purok_id, new_household_id),
                    FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE CASCADE,
                    FOREIGN KEY (old_purok_id) REFERENCES purok_sitio(purok_id) ON DELETE SET NULL,
                    FOREIGN KEY (new_purok_id) REFERENCES purok_sitio(purok_id) ON DELETE SET NULL,
                    FOREIGN KEY (old_household_id) REFERENCES household(household_id) ON DELETE SET NULL,
                    FOREIGN KEY (new_household_id) REFERENCES household(household_id) ON DELETE SET NULL,
                    FOREIGN KEY (transferred_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
                )", conn);
            cmd.ExecuteNonQuery();
        }

        if (!TableExists(conn, "backup_run"))
        {
            if (!TableExists(conn, "user_account"))
            {
                throw new InvalidOperationException("Missing required user_account table for backups.");
            }

            using var cmd = new MySqlCommand(@"
                CREATE TABLE backup_run (
                    backup_run_id INT AUTO_INCREMENT PRIMARY KEY,
                    started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ended_at DATETIME NULL,
                    status ENUM('RUNNING','SUCCESS','FAILED') NOT NULL DEFAULT 'RUNNING',
                    backup_type ENUM('FULL','INCREMENTAL','DIFFERENTIAL') NOT NULL DEFAULT 'FULL',
                    base_started_at DATETIME NULL,
                    base_backup_run_id INT NULL,
                    file_path VARCHAR(500) NULL,
                    file_size_bytes BIGINT NULL,
                    error_message TEXT NULL,
                    created_by_user_id INT NULL,
                    INDEX idx_backup_run_started_at (started_at),
                    INDEX idx_backup_run_status (status),
                    INDEX idx_backup_run_type_started_at (backup_type, started_at),
                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
                )", conn);
            cmd.ExecuteNonQuery();
        }
    }

    private static void EnsureAppCompatIndexes(MySqlConnection conn)
    {
        // Best-effort indexes; older DBs may already have them or may have conflicting definitions.
        TryExecuteIgnore(conn,
            "CREATE UNIQUE INDEX ux_document_request_verification_token ON document_request(verification_token)");
        TryExecuteIgnore(conn,
            "CREATE INDEX idx_document_request_expires_at ON document_request(expires_at)");
        TryExecuteIgnore(conn,
            "CREATE INDEX idx_document_request_renewed_from ON document_request(renewed_from_request_id)");
        TryExecuteIgnore(conn,
            "CREATE INDEX idx_case_record_date_status ON case_record(date_filed, status, complainant_id)");
        TryExecuteIgnore(conn,
            "CREATE INDEX idx_purok_coordinates ON purok_sitio(latitude, longitude)");
        TryExecuteIgnore(conn,
            "CREATE INDEX idx_backup_run_type_started_at ON backup_run(backup_type, started_at)");

        // Login query: WHERE username=@u AND password_hash=@p AND is_active=1
        TryExecuteIgnore(conn,
            "CREATE UNIQUE INDEX ux_user_account_username ON user_account(username)");

        // Resident list query: WHERE is_deleted=N ORDER BY last_name, first_name
        TryExecuteIgnore(conn,
            "CREATE INDEX idx_resident_search ON resident(is_deleted, last_name, first_name)");

        // Fix role table bloat: deduplicate then add unique constraint so
        // ON DUPLICATE KEY UPDATE in SchemaBootstrap works correctly.
        EnsureRoleNameUnique(conn);
    }

    private static void EnsureRoleNameUnique(MySqlConnection conn)
    {
        // 1. Keep only the lowest role_id per name; re-point user_role rows then delete dupes.
        try
        {
            using var dedupCmd = new MySqlCommand(@"
                UPDATE user_role ur
                JOIN (
                    SELECT name, MIN(role_id) AS keep_id
                    FROM role
                    GROUP BY name
                    HAVING COUNT(*) > 1
                ) keeper ON ur.role_id IN (
                    SELECT role_id FROM role
                    WHERE name = keeper.name AND role_id != keeper.keep_id
                )
                SET ur.role_id = keeper.keep_id
                WHERE 1=1;", conn);
            dedupCmd.ExecuteNonQuery();

            using var deleteCmd = new MySqlCommand(@"
                DELETE r FROM role r
                INNER JOIN (
                    SELECT name, MIN(role_id) AS keep_id
                    FROM role GROUP BY name
                ) keep ON r.name = keep.name AND r.role_id != keep.keep_id;", conn);
            deleteCmd.ExecuteNonQuery();
        }
        catch
        {
            // Non-fatal; if FKs prevent deletion it's fine — the index creation below will still succeed
            // if only one row per name remains, or silently fail if dupes are left.
        }

        // 2. Add unique index (silently ignored if already exists or dupes remain).
        TryExecuteIgnore(conn, "CREATE UNIQUE INDEX ux_role_name ON role(name)");
    }

    private static void TryExecuteIgnore(MySqlConnection conn, string sql)
    {
        try
        {
            using var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Ignore.
        }
    }

    private static bool ColumnExists(MySqlConnection conn, string table, string column)
    {
        using var cmd = new MySqlCommand(
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table
                AND COLUMN_NAME = @column", conn);
        cmd.Parameters.AddWithValue("@table", table);
        cmd.Parameters.AddWithValue("@column", column);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static bool TableExists(MySqlConnection conn, string table)
    {
        using var cmd = new MySqlCommand(
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table", conn);
        cmd.Parameters.AddWithValue("@table", table);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
