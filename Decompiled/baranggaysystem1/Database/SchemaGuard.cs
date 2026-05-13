using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class SchemaGuard
{
	private static readonly object ReadySync = new object();

	private static string? _lastReadyFingerprint;

	public static void EnsureDatabaseReady(string? knownWorkingConnectionString = null, bool force = false)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		string text = (string.IsNullOrWhiteSpace(knownWorkingConnectionString) ? DBConnection.GetCurrentConnectionString() : knownWorkingConnectionString);
		string text2 = BuildReadyFingerprint(text);
		lock (ReadySync)
		{
			if (!force && string.Equals(_lastReadyFingerprint, text2, StringComparison.Ordinal))
			{
				return;
			}
			MySqlConnection val = new MySqlConnection(text);
			try
			{
				((DbConnection)(object)val).Open();
				MigrationRunner.ApplyPendingMigrations(val);
				SchemaBootstrap.EnsureCoreDefaults(val);
				EnsureAppCompatColumns(val);
				EnsureAppCompatTables(val);
				EnsureAppCompatIndexes(val);
				SchemaBootstrap.EnsureCoreDefaults(val);
				_lastReadyFingerprint = text2;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public static StartupHealthReport RunStartupHealthChecks()
	{
		StartupHealthReport startupHealthReport = new StartupHealthReport();
		MySqlConnection conn;
		try
		{
			conn = DBConnection.GetConnection();
			((DbConnection)(object)conn).Open();
			startupHealthReport.Add("DB connectivity", StartupHealthLevel.Ok, "Connected successfully.");
		}
		catch (Exception ex)
		{
			startupHealthReport.Add("DB connectivity", StartupHealthLevel.Critical, ex.Message);
			return startupHealthReport;
		}
		MySqlConnection val = conn;
		try
		{
			try
			{
				if (!MigrationRunner.HasMigrationFiles())
				{
					startupHealthReport.Add("Migration files", StartupHealthLevel.Warning, "Migration SQL files were not found in output/project path.");
				}
				else
				{
					startupHealthReport.Add("Migration files", StartupHealthLevel.Ok, "Migration SQL files found.");
				}
			}
			catch (Exception ex2)
			{
				startupHealthReport.Add("Migration files", StartupHealthLevel.Warning, ex2.Message);
			}
			try
			{
				List<string> list = new string[20]
				{
					"barangay", "purok_sitio", "household", "resident", "role", "user_account", "document_type", "document_request", "case_record", "record_attachment",
					"outbound_notification", "resident_transfer_history", "role_permission", "backup_run", "expense_entry", "inventory_item", "asset_record", "procurement_request", "resident_classification", "schema_migrations"
				}.Where((string t) => !TableExists(conn, t)).ToList();
				if (list.Count == 0)
				{
					startupHealthReport.Add("Required tables", StartupHealthLevel.Ok, "All required tables are present.");
				}
				else
				{
					startupHealthReport.Add("Required tables", StartupHealthLevel.Critical, "Missing: " + FormatList(list));
				}
			}
			catch (Exception ex3)
			{
				startupHealthReport.Add("Required tables", StartupHealthLevel.Critical, ex3.Message);
			}
			try
			{
				(string, string)[] obj = new(string, string)[33]
				{
					("resident", "is_deleted"),
					("resident", "photo"),
					("resident", "is_head_of_family"),
					("resident", "is_solo_parent"),
					("resident", "is_youth"),
					("resident", "is_indigent"),
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
					("projects", "record_type"),
					("projects", "attendance_count"),
					("projects", "outcome_status"),
					("expense_entry", "expense_title"),
					("inventory_item", "quantity_on_hand"),
					("asset_record", "lifecycle_status"),
					("procurement_request", "request_title"),
					("procurement_request", "workflow_status"),
					("resident_classification", "classification_type"),
					("resident_classification", "status")
				};
				List<string> list2 = new List<string>();
				(string, string)[] array = obj;
				for (int num = 0; num < array.Length; num++)
				{
					var (text, text2) = array[num];
					if (!TableExists(conn, text))
					{
						list2.Add(text + "." + text2 + " (table missing)");
					}
					else if (!ColumnExists(conn, text, text2))
					{
						list2.Add(text + "." + text2);
					}
				}
				if (list2.Count == 0)
				{
					startupHealthReport.Add("Required columns", StartupHealthLevel.Ok, "All required columns are present.");
				}
				else
				{
					startupHealthReport.Add("Required columns", StartupHealthLevel.Critical, "Missing: " + FormatList(list2));
				}
			}
			catch (Exception ex4)
			{
				startupHealthReport.Add("Required columns", StartupHealthLevel.Critical, ex4.Message);
			}
			try
			{
				IReadOnlyList<string> pendingAutoMigrationNames = MigrationRunner.GetPendingAutoMigrationNames(conn);
				if (pendingAutoMigrationNames.Count == 0)
				{
					startupHealthReport.Add("Pending auto migrations", StartupHealthLevel.Ok, "No pending auto migrations.");
				}
				else
				{
					startupHealthReport.Add("Pending auto migrations", StartupHealthLevel.Warning, "Pending: " + FormatList(pendingAutoMigrationNames));
				}
			}
			catch (Exception ex5)
			{
				startupHealthReport.Add("Pending auto migrations", StartupHealthLevel.Warning, ex5.Message);
			}
			try
			{
				IReadOnlyList<string> pendingManualMigrationNames = MigrationRunner.GetPendingManualMigrationNames(conn);
				if (pendingManualMigrationNames.Count == 0)
				{
					startupHealthReport.Add("Pending manual migrations", StartupHealthLevel.Ok, "No pending manual migrations.");
				}
				else
				{
					startupHealthReport.Add("Pending manual migrations", StartupHealthLevel.Warning, "Needs manual run: " + FormatList(pendingManualMigrationNames));
				}
			}
			catch (Exception ex6)
			{
				startupHealthReport.Add("Pending manual migrations", StartupHealthLevel.Warning, ex6.Message);
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		return startupHealthReport;
	}

	private static string FormatList(IReadOnlyList<string> values, int max = 6)
	{
		if (values.Count <= max)
		{
			return string.Join(", ", values);
		}
		int value = values.Count - max;
		return string.Join(", ", values.Take(max)) + $", +{value} more";
	}

	private static void EnsureAppCompatColumns(MySqlConnection conn)
	{
		//IL_081b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0822: Expected O, but got Unknown
		(string, string, string)[] array = new(string, string, string)[68]
		{
			("resident", "photo", "ALTER TABLE resident ADD COLUMN photo LONGBLOB NULL"),
			("resident", "is_deleted", "ALTER TABLE resident ADD COLUMN is_deleted TINYINT(1) NOT NULL DEFAULT 0"),
			("resident", "is_head_of_family", "ALTER TABLE resident ADD COLUMN is_head_of_family TINYINT(1) NOT NULL DEFAULT 0"),
			("resident", "deleted_at", "ALTER TABLE resident ADD COLUMN deleted_at DATETIME NULL"),
			("resident", "deleted_by_user_id", "ALTER TABLE resident ADD COLUMN deleted_by_user_id INT NULL"),
			("resident", "delete_reason", "ALTER TABLE resident ADD COLUMN delete_reason VARCHAR(255) NULL"),
			("resident", "is_solo_parent", "ALTER TABLE resident ADD COLUMN is_solo_parent TINYINT(1) NOT NULL DEFAULT 0"),
			("resident", "is_youth", "ALTER TABLE resident ADD COLUMN is_youth TINYINT(1) NOT NULL DEFAULT 0"),
			("resident", "is_indigent", "ALTER TABLE resident ADD COLUMN is_indigent TINYINT(1) NOT NULL DEFAULT 0"),
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
			("backup_run", "base_backup_run_id", "ALTER TABLE backup_run ADD COLUMN base_backup_run_id INT NULL"),
			("projects", "record_type", "ALTER TABLE projects ADD COLUMN record_type VARCHAR(20) NOT NULL DEFAULT 'Project'"),
			("projects", "attendance_target", "ALTER TABLE projects ADD COLUMN attendance_target INT NOT NULL DEFAULT 0"),
			("projects", "attendance_count", "ALTER TABLE projects ADD COLUMN attendance_count INT NOT NULL DEFAULT 0"),
			("projects", "last_activity_date", "ALTER TABLE projects ADD COLUMN last_activity_date DATE NULL"),
			("projects", "outcome_status", "ALTER TABLE projects ADD COLUMN outcome_status VARCHAR(30) NOT NULL DEFAULT 'Pending'"),
			("projects", "outcome_summary", "ALTER TABLE projects ADD COLUMN outcome_summary TEXT NULL")
		};
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		(string, string, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, string, string) tuple = array2[i];
			hashSet.Add(tuple.Item1);
		}
		foreach (string item in hashSet)
		{
			if (!TableExists(conn, item))
			{
				throw new InvalidOperationException("Missing required table '" + item + "'.");
			}
		}
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, string, string) tuple2 = array2[i];
			if (!ColumnExists(conn, tuple2.Item1, tuple2.Item2))
			{
				MySqlCommand val = new MySqlCommand(tuple2.Item3, conn);
				try
				{
					((DbCommand)(object)val).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
		}
	}

	private static void EnsureAppCompatTables(MySqlConnection conn)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Expected O, but got Unknown
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Expected O, but got Unknown
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Expected O, but got Unknown
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Expected O, but got Unknown
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Expected O, but got Unknown
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Expected O, but got Unknown
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Expected O, but got Unknown
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Expected O, but got Unknown
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Expected O, but got Unknown
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a1: Expected O, but got Unknown
		//IL_04ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Expected O, but got Unknown
		if (!TableExists(conn, "document_payment"))
		{
			if (!TableExists(conn, "document_request") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for document payments.");
			}
			MySqlCommand val = new MySqlCommand("\r\n                CREATE TABLE document_payment (\r\n                    payment_id INT AUTO_INCREMENT PRIMARY KEY,\r\n                    doc_request_id INT NOT NULL,\r\n                    amount DECIMAL(10,2),\r\n                    or_no VARCHAR(100),\r\n                    payment_method ENUM('Cash','GCash','Bank'),\r\n                    paid_at DATETIME DEFAULT CURRENT_TIMESTAMP,\r\n                    received_by_user_id INT,\r\n                    FOREIGN KEY (doc_request_id) REFERENCES document_request(doc_request_id) ON DELETE CASCADE,\r\n                    FOREIGN KEY (received_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\r\n                )", conn);
			try
			{
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		if (!TableExists(conn, "case_timeline"))
		{
			if (!TableExists(conn, "case_record") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for case timeline.");
			}
			MySqlCommand val2 = new MySqlCommand("\r\n                CREATE TABLE case_timeline (\r\n                    timeline_id INT AUTO_INCREMENT PRIMARY KEY,\r\n                    case_id INT NOT NULL,\r\n                    event_type VARCHAR(50) NOT NULL,\r\n                    event_title VARCHAR(150) NOT NULL,\r\n                    event_details TEXT NULL,\r\n                    from_status VARCHAR(30) NULL,\r\n                    to_status VARCHAR(30) NULL,\r\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    created_by_user_id INT NULL,\r\n                    INDEX idx_case_timeline_case (case_id),\r\n                    INDEX idx_case_timeline_created_at (created_at),\r\n                    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,\r\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\r\n                )", conn);
			try
			{
				((DbCommand)(object)val2).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		if (!TableExists(conn, "case_hearing"))
		{
			if (!TableExists(conn, "case_record") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for case hearings.");
			}
			MySqlCommand val3 = new MySqlCommand("\r\n                CREATE TABLE case_hearing (\r\n                    hearing_id INT AUTO_INCREMENT PRIMARY KEY,\r\n                    case_id INT NOT NULL,\r\n                    schedule_at DATETIME,\r\n                    venue VARCHAR(150),\r\n                    status ENUM('SCHEDULED','DONE','RESET','CANCELLED') DEFAULT 'SCHEDULED',\r\n                    minutes TEXT,\r\n                    result TEXT,\r\n                    created_by_user_id INT NULL,\r\n                    INDEX idx_case_hearing_case (case_id),\r\n                    INDEX idx_case_hearing_status (status),\r\n                    INDEX idx_case_hearing_schedule (schedule_at),\r\n                    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,\r\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\r\n                )", conn);
			try
			{
				((DbCommand)(object)val3).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
		}
		if (!TableExists(conn, "role_permission"))
		{
			if (!TableExists(conn, "role"))
			{
				throw new InvalidOperationException("Missing required role table for permissions.");
			}
			MySqlCommand val4 = new MySqlCommand("\r\n                CREATE TABLE role_permission (\r\n                    role_id INT NOT NULL,\r\n                    permission_key VARCHAR(100) NOT NULL,\r\n                    is_allowed TINYINT(1) NOT NULL DEFAULT 0,\r\n                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\r\n                    PRIMARY KEY (role_id, permission_key),\r\n                    FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE\r\n                )", conn);
			try
			{
				((DbCommand)(object)val4).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val4)?.Dispose();
			}
		}
		if (!TableExists(conn, "record_attachment"))
		{
			if (!TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required user_account table for attachments.");
			}
			MySqlCommand val5 = new MySqlCommand("\r\n                CREATE TABLE record_attachment (\r\n                    attachment_id BIGINT AUTO_INCREMENT PRIMARY KEY,\r\n                    entity_type ENUM('RESIDENT','CASE','CERTIFICATE') NOT NULL,\r\n                    entity_id INT NOT NULL,\r\n                    file_name VARCHAR(255) NOT NULL,\r\n                    file_ext VARCHAR(20) NULL,\r\n                    mime_type VARCHAR(120) NULL,\r\n                    file_size_bytes BIGINT NOT NULL DEFAULT 0,\r\n                    file_hash CHAR(64) NULL,\r\n                    file_blob LONGBLOB NOT NULL,\r\n                    notes VARCHAR(255) NULL,\r\n                    uploaded_by_user_id INT NULL,\r\n                    uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    INDEX idx_attachment_entity (entity_type, entity_id, uploaded_at),\r\n                    INDEX idx_attachment_hash (file_hash),\r\n                    FOREIGN KEY (uploaded_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\r\n                )", conn);
			try
			{
				((DbCommand)(object)val5).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val5)?.Dispose();
			}
		}
		if (!TableExists(conn, "outbound_notification"))
		{
			if (!TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required user_account table for notifications.");
			}
			MySqlCommand val6 = new MySqlCommand("\r\n                CREATE TABLE outbound_notification (\r\n                    notification_id BIGINT AUTO_INCREMENT PRIMARY KEY,\r\n                    dedupe_key VARCHAR(160) NULL,\r\n                    channel ENUM('SMS','EMAIL') NOT NULL,\r\n                    recipient VARCHAR(200) NOT NULL,\r\n                    subject VARCHAR(180) NULL,\r\n                    message TEXT NOT NULL,\r\n                    status ENUM('PENDING','SENT','FAILED','SKIPPED') NOT NULL DEFAULT 'PENDING',\r\n                    source_module VARCHAR(40) NULL,\r\n                    source_record_id INT NULL,\r\n                    template_key VARCHAR(80) NULL,\r\n                    scheduled_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    sent_at DATETIME NULL,\r\n                    attempts INT NOT NULL DEFAULT 0,\r\n                    last_error VARCHAR(500) NULL,\r\n                    created_by_user_id INT NULL,\r\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\r\n                    UNIQUE KEY ux_outbound_notification_dedupe (dedupe_key),\r\n                    INDEX idx_outbound_notification_status (status, scheduled_at),\r\n                    INDEX idx_outbound_notification_source (source_module, source_record_id),\r\n                    INDEX idx_outbound_notification_channel (channel, status),\r\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\r\n                )", conn);
			try
			{
				((DbCommand)(object)val6).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val6)?.Dispose();
			}
		}
		if (!TableExists(conn, "outbound_notification_attempt"))
		{
			if (!TableExists(conn, "outbound_notification"))
			{
				throw new InvalidOperationException("Missing required outbound_notification table.");
			}
			MySqlCommand val7 = new MySqlCommand("\r\n                CREATE TABLE outbound_notification_attempt (\r\n                    attempt_id BIGINT AUTO_INCREMENT PRIMARY KEY,\r\n                    notification_id BIGINT NOT NULL,\r\n                    attempt_no INT NOT NULL,\r\n                    attempted_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    success TINYINT(1) NOT NULL DEFAULT 0,\r\n                    response_code VARCHAR(64) NULL,\r\n                    response_message VARCHAR(500) NULL,\r\n                    INDEX idx_notification_attempt_notification (notification_id, attempted_at),\r\n                    FOREIGN KEY (notification_id) REFERENCES outbound_notification(notification_id) ON DELETE CASCADE\r\n                )", conn);
			try
			{
				((DbCommand)(object)val7).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val7)?.Dispose();
			}
		}
		if (!TableExists(conn, "resident_transfer_history"))
		{
			if (!TableExists(conn, "resident") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required resident/user tables for transfer history.");
			}
			MySqlCommand val8 = new MySqlCommand("\r\n                CREATE TABLE resident_transfer_history (\r\n                    transfer_id BIGINT AUTO_INCREMENT PRIMARY KEY,\r\n                    resident_id INT NOT NULL,\r\n                    old_purok_id INT NULL,\r\n                    old_household_id INT NULL,\r\n                    old_address VARCHAR(255) NULL,\r\n                    new_purok_id INT NULL,\r\n                    new_household_id INT NULL,\r\n                    new_address VARCHAR(255) NULL,\r\n                    transfer_reason VARCHAR(255) NULL,\r\n                    transferred_by_user_id INT NULL,\r\n                    transferred_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    INDEX idx_transfer_history_resident (resident_id, transferred_at),\r\n                    INDEX idx_transfer_history_old_location (old_purok_id, old_household_id),\r\n                    INDEX idx_transfer_history_new_location (new_purok_id, new_household_id),\r\n                    FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE CASCADE,\r\n                    FOREIGN KEY (old_purok_id) REFERENCES purok_sitio(purok_id) ON DELETE SET NULL,\r\n                    FOREIGN KEY (new_purok_id) REFERENCES purok_sitio(purok_id) ON DELETE SET NULL,\r\n                    FOREIGN KEY (old_household_id) REFERENCES household(household_id) ON DELETE SET NULL,\r\n                    FOREIGN KEY (new_household_id) REFERENCES household(household_id) ON DELETE SET NULL,\r\n                    FOREIGN KEY (transferred_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\r\n                )", conn);
			try
			{
				((DbCommand)(object)val8).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val8)?.Dispose();
			}
		}
		if (!TableExists(conn, "backup_run"))
		{
			if (!TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required user_account table for backups.");
			}
			MySqlCommand val9 = new MySqlCommand("\r\n                CREATE TABLE backup_run (\r\n                    backup_run_id INT AUTO_INCREMENT PRIMARY KEY,\r\n                    started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    ended_at DATETIME NULL,\r\n                    status ENUM('RUNNING','SUCCESS','FAILED') NOT NULL DEFAULT 'RUNNING',\r\n                    backup_type ENUM('FULL','INCREMENTAL','DIFFERENTIAL') NOT NULL DEFAULT 'FULL',\r\n                    base_started_at DATETIME NULL,\r\n                    base_backup_run_id INT NULL,\r\n                    file_path VARCHAR(500) NULL,\r\n                    file_size_bytes BIGINT NULL,\r\n                    error_message TEXT NULL,\r\n                    created_by_user_id INT NULL,\r\n                    INDEX idx_backup_run_started_at (started_at),\r\n                    INDEX idx_backup_run_status (status),\r\n                    INDEX idx_backup_run_type_started_at (backup_type, started_at),\r\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\r\n                )", conn);
			try
			{
				((DbCommand)(object)val9).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val9)?.Dispose();
			}
		}
		if (!TableExists(conn, "ayuda_program"))
		{
			if (!TableExists(conn, "barangay") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for ayuda program management.");
			}
			MySqlCommand val10 = new MySqlCommand("\n                CREATE TABLE ayuda_program (\n                    program_id INT AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL,\n                    program_name VARCHAR(150) NOT NULL,\n                    category VARCHAR(80) NOT NULL,\n                    allocated_budget DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',\n                    start_date DATE NULL,\n                    end_date DATE NULL,\n                    notes TEXT NULL,\n                    created_by_user_id INT NULL,\n                    updated_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\n                    INDEX idx_ayuda_program_barangay_status (barangay_id, status),\n                    INDEX idx_ayuda_program_category (category),\n                    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,\n                    FOREIGN KEY (updated_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\n                )", conn);
			try
			{
				((DbCommand)(object)val10).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val10)?.Dispose();
			}
		}
		if (!TableExists(conn, "ayuda_release"))
		{
			if (!TableExists(conn, "ayuda_program") || !TableExists(conn, "resident") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for ayuda releases.");
			}
			MySqlCommand val11 = new MySqlCommand("\n                CREATE TABLE ayuda_release (\n                    release_id INT AUTO_INCREMENT PRIMARY KEY,\n                    program_id INT NOT NULL,\n                    resident_id INT NOT NULL,\n                    reference_no VARCHAR(50) NOT NULL,\n                    amount DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    released_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    release_status VARCHAR(20) NOT NULL DEFAULT 'RELEASED',\n                    notes TEXT NULL,\n                    created_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    UNIQUE KEY ux_ayuda_release_reference (reference_no),\n                    INDEX idx_ayuda_release_program (program_id, released_at),\n                    INDEX idx_ayuda_release_resident (resident_id, released_at),\n                    INDEX idx_ayuda_release_status (release_status, released_at),\n                    FOREIGN KEY (program_id) REFERENCES ayuda_program(program_id) ON DELETE CASCADE,\n                    FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE RESTRICT,\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\n                )", conn);
			try
			{
				((DbCommand)(object)val11).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val11)?.Dispose();
			}
		}
		if (!TableExists(conn, "expense_entry"))
		{
			if (!TableExists(conn, "barangay") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for expense tracking.");
			}
			MySqlCommand val12 = new MySqlCommand("\n                CREATE TABLE expense_entry (\n                    expense_id INT AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL,\n                    expense_date DATE NOT NULL,\n                    expense_category VARCHAR(80) NOT NULL,\n                    expense_title VARCHAR(150) NOT NULL,\n                    payee_name VARCHAR(150) NULL,\n                    amount DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    payment_method VARCHAR(40) NOT NULL DEFAULT 'Cash',\n                    status VARCHAR(20) NOT NULL DEFAULT 'POSTED',\n                    reference_no VARCHAR(60) NULL,\n                    notes TEXT NULL,\n                    created_by_user_id INT NULL,\n                    updated_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\n                    INDEX idx_expense_entry_barangay_date (barangay_id, expense_date),\n                    INDEX idx_expense_entry_category_status (expense_category, status),\n                    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,\n                    FOREIGN KEY (updated_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\n                )", conn);
			try
			{
				((DbCommand)(object)val12).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val12)?.Dispose();
			}
		}
		if (!TableExists(conn, "inventory_item"))
		{
			if (!TableExists(conn, "barangay") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for inventory tracking.");
			}
			MySqlCommand val13 = new MySqlCommand("\n                CREATE TABLE inventory_item (\n                    item_id INT AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL,\n                    item_name VARCHAR(150) NOT NULL,\n                    category VARCHAR(80) NOT NULL,\n                    unit VARCHAR(40) NOT NULL DEFAULT 'pcs',\n                    quantity_on_hand DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    reorder_level DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    unit_cost DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    location VARCHAR(150) NULL,\n                    item_status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',\n                    last_restocked_at DATE NULL,\n                    notes TEXT NULL,\n                    created_by_user_id INT NULL,\n                    updated_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\n                    INDEX idx_inventory_item_barangay_status (barangay_id, item_status),\n                    INDEX idx_inventory_item_category (category),\n                    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,\n                    FOREIGN KEY (updated_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\n                )", conn);
			try
			{
				((DbCommand)(object)val13).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val13)?.Dispose();
			}
		}
		if (!TableExists(conn, "asset_record"))
		{
			if (!TableExists(conn, "barangay") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for asset tracking.");
			}
			MySqlCommand val14 = new MySqlCommand("\n                CREATE TABLE asset_record (\n                    asset_id INT AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL,\n                    asset_name VARCHAR(150) NOT NULL,\n                    asset_category VARCHAR(80) NOT NULL,\n                    asset_tag VARCHAR(80) NULL,\n                    acquisition_date DATE NULL,\n                    acquisition_cost DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    assigned_location VARCHAR(150) NULL,\n                    custodian_name VARCHAR(150) NULL,\n                    condition_status VARCHAR(20) NOT NULL DEFAULT 'GOOD',\n                    lifecycle_status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',\n                    notes TEXT NULL,\n                    created_by_user_id INT NULL,\n                    updated_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\n                    INDEX idx_asset_record_barangay_lifecycle (barangay_id, lifecycle_status),\n                    INDEX idx_asset_record_category_condition (asset_category, condition_status),\n                    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,\n                    FOREIGN KEY (updated_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\n                )", conn);
			try
			{
				((DbCommand)(object)val14).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val14)?.Dispose();
			}
		}
		if (!TableExists(conn, "procurement_request"))
		{
			if (!TableExists(conn, "barangay") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for procurement tracking.");
			}
			MySqlCommand val15 = new MySqlCommand("\n                CREATE TABLE procurement_request (\n                    procurement_id INT AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL,\n                    request_type VARCHAR(40) NOT NULL DEFAULT 'PROCUREMENT',\n                    request_date DATE NOT NULL,\n                    needed_by_date DATE NULL,\n                    request_title VARCHAR(150) NOT NULL,\n                    procurement_category VARCHAR(80) NOT NULL,\n                    vendor_name VARCHAR(150) NULL,\n                    requested_by_name VARCHAR(120) NULL,\n                    total_amount DECIMAL(12,2) NOT NULL DEFAULT 0.00,\n                    workflow_status VARCHAR(30) NOT NULL DEFAULT 'DRAFT',\n                    purchase_order_no VARCHAR(60) NULL,\n                    approved_by_name VARCHAR(120) NULL,\n                    approved_at DATETIME NULL,\n                    item_summary TEXT NULL,\n                    approval_notes TEXT NULL,\n                    notes TEXT NULL,\n                    created_by_user_id INT NULL,\n                    updated_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\n                    INDEX idx_procurement_request_barangay_status (barangay_id, workflow_status),\n                    INDEX idx_procurement_request_date (request_date, needed_by_date),\n                    INDEX idx_procurement_request_category_type (procurement_category, request_type),\n                    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,\n                    FOREIGN KEY (updated_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\n                )", conn);
			try
			{
				((DbCommand)(object)val15).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val15)?.Dispose();
			}
		}
		if (!TableExists(conn, "resident_classification"))
		{
			if (!TableExists(conn, "barangay") || !TableExists(conn, "user_account"))
			{
				throw new InvalidOperationException("Missing required tables for resident classifications.");
			}
			MySqlCommand val16 = new MySqlCommand("\n                CREATE TABLE resident_classification (\n                    classification_id INT AUTO_INCREMENT PRIMARY KEY,\n                    barangay_id INT NOT NULL,\n                    classification_type VARCHAR(20) NOT NULL DEFAULT 'TAG',\n                    classification_key VARCHAR(60) NULL,\n                    name VARCHAR(100) NOT NULL,\n                    description VARCHAR(255) NULL,\n                    color_hex VARCHAR(20) NULL,\n                    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',\n                    is_system TINYINT(1) NOT NULL DEFAULT 0,\n                    sort_order INT NOT NULL DEFAULT 0,\n                    created_by_user_id INT NULL,\n                    updated_by_user_id INT NULL,\n                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\n                    INDEX idx_resident_classification_type_status (barangay_id, classification_type, status),\n                    INDEX idx_resident_classification_name (barangay_id, classification_type, name),\n                    UNIQUE KEY ux_resident_classification_key (barangay_id, classification_type, classification_key),\n                    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,\n                    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,\n                    FOREIGN KEY (updated_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL\n                )", conn);
			try
			{
				((DbCommand)(object)val16).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val16)?.Dispose();
			}
		}
		EnsureResidentClassificationDefaults(conn);
	}

	private static void EnsureAppCompatIndexes(MySqlConnection conn)
	{
		TryExecuteIgnore(conn, "CREATE UNIQUE INDEX ux_document_request_verification_token ON document_request(verification_token)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_document_request_expires_at ON document_request(expires_at)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_document_request_renewed_from ON document_request(renewed_from_request_id)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_case_record_date_status ON case_record(date_filed, status, complainant_id)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_purok_coordinates ON purok_sitio(latitude, longitude)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_backup_run_type_started_at ON backup_run(backup_type, started_at)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_procurement_request_barangay_status ON procurement_request(barangay_id, workflow_status)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_procurement_request_date ON procurement_request(request_date, needed_by_date)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_procurement_request_category_type ON procurement_request(procurement_category, request_type)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_resident_classification_type_status ON resident_classification(barangay_id, classification_type, status)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_resident_classification_name ON resident_classification(barangay_id, classification_type, name)");
		EnsureDocumentTypeCodeUnique(conn);
		TryExecuteIgnore(conn, "CREATE UNIQUE INDEX ux_user_account_username ON user_account(username)");
		TryExecuteIgnore(conn, "CREATE INDEX idx_resident_search ON resident(is_deleted, last_name, first_name)");
		EnsureRoleNameUnique(conn);
	}

	private static void EnsureRoleNameUnique(MySqlConnection conn)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		try
		{
			MySqlCommand val = new MySqlCommand("\r\n                UPDATE user_role ur\r\n                JOIN (\r\n                    SELECT name, MIN(role_id) AS keep_id\r\n                    FROM role\r\n                    GROUP BY name\r\n                    HAVING COUNT(*) > 1\r\n                ) keeper ON ur.role_id IN (\r\n                    SELECT role_id FROM role\r\n                    WHERE name = keeper.name AND role_id != keeper.keep_id\r\n                )\r\n                SET ur.role_id = keeper.keep_id\r\n                WHERE 1=1;", conn);
			try
			{
				((DbCommand)(object)val).ExecuteNonQuery();
				MySqlCommand val2 = new MySqlCommand("\r\n                DELETE r FROM role r\r\n                INNER JOIN (\r\n                    SELECT name, MIN(role_id) AS keep_id\r\n                    FROM role GROUP BY name\r\n                ) keep ON r.name = keep.name AND r.role_id != keep.keep_id;", conn);
				try
				{
					((DbCommand)(object)val2).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
		}
		TryExecuteIgnore(conn, "CREATE UNIQUE INDEX ux_role_name ON role(name)");
	}

	private static void EnsureDocumentTypeCodeUnique(MySqlConnection conn)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		if (!TableExists(conn, "document_type"))
		{
			return;
		}
		try
		{
			TryExecuteIgnore(conn, "UPDATE document_type SET code = NULL WHERE code IS NOT NULL AND TRIM(code) = ''");
			List<(int, string, string)> list = new List<(int, string, string)>();
			MySqlCommand val = new MySqlCommand("SELECT doc_type_id,\n                         COALESCE(name, '') AS name,\n                         COALESCE(code, '') AS code\n                  FROM document_type\n                  ORDER BY doc_type_id ASC", conn);
			try
			{
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						list.Add((Convert.ToInt32(((DbDataReader)(object)val2)["doc_type_id"]), Convert.ToString(((DbDataReader)(object)val2)["name"]) ?? string.Empty, Convert.ToString(((DbDataReader)(object)val2)["code"]) ?? string.Empty));
					}
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			foreach (IGrouping<string, (int, string, string)> item2 in from @group in list.GroupBy<(int, string, string), string>(GetDocumentTypeIdentityKey, StringComparer.Ordinal)
				where !string.IsNullOrWhiteSpace(@group.Key) && @group.Count() > 1
				select @group)
			{
				int item = item2.First().Item1;
				foreach (var item3 in item2.Skip(1))
				{
					RepointDocumentRequestDocumentType(conn, item3.Item1, item);
					MergeDocumentNumberSequence(conn, item3.Item1, item);
					MySqlCommand val3 = new MySqlCommand("DELETE FROM document_type WHERE doc_type_id = @docTypeId", conn);
					try
					{
						val3.Parameters.AddWithValue("@docTypeId", (object)item3.Item1);
						((DbCommand)(object)val3).ExecuteNonQuery();
					}
					finally
					{
						((IDisposable)val3)?.Dispose();
					}
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("SchemaGuard: document type cleanup skipped.", ex);
		}
		TryExecuteIgnore(conn, "CREATE UNIQUE INDEX ux_document_type_code ON document_type(code)");
	}

	private static string GetDocumentTypeIdentityKey((int Id, string Name, string Code) record)
	{
		string text = record.Code.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return "CODE:" + text.ToUpperInvariant();
		}
		string text2 = record.Name.Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return "NAME:" + text2.ToUpperInvariant();
		}
		return string.Empty;
	}

	private static void RepointDocumentRequestDocumentType(MySqlConnection conn, int duplicateId, int keepId)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		if (!TableExists(conn, "document_request"))
		{
			return;
		}
		MySqlCommand val = new MySqlCommand("UPDATE document_request\n              SET doc_type_id = @keepId\n              WHERE doc_type_id = @duplicateId", conn);
		try
		{
			val.Parameters.AddWithValue("@keepId", (object)keepId);
			val.Parameters.AddWithValue("@duplicateId", (object)duplicateId);
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void MergeDocumentNumberSequence(MySqlConnection conn, int duplicateId, int keepId)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		if (!TableExists(conn, "document_number_sequence"))
		{
			return;
		}
		MySqlCommand val = new MySqlCommand("INSERT INTO document_number_sequence (doc_type_id, year, last_no)\n              SELECT @keepId, year, last_no\n              FROM document_number_sequence\n              WHERE doc_type_id = @duplicateId\n              ON DUPLICATE KEY UPDATE\n                  last_no = GREATEST(last_no, VALUES(last_no))", conn);
		try
		{
			val.Parameters.AddWithValue("@keepId", (object)keepId);
			val.Parameters.AddWithValue("@duplicateId", (object)duplicateId);
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		MySqlCommand val2 = new MySqlCommand("DELETE FROM document_number_sequence WHERE doc_type_id = @duplicateId", conn);
		try
		{
			val2.Parameters.AddWithValue("@duplicateId", (object)duplicateId);
			((DbCommand)(object)val2).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	private static void EnsureResidentClassificationDefaults(MySqlConnection conn)
	{
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		(string, string, string, string, int)[] array = new(string, string, string, string, int)[7]
		{
			("PWD", "PWD", "Residents marked as persons with disability.", "#2563EB", 10),
			("SENIOR", "Senior Citizen", "Residents marked as senior citizens.", "#7C3AED", 20),
			("FOUR_PS", "4Ps Beneficiary", "Residents marked as 4Ps beneficiaries.", "#16A34A", 30),
			("VOTER", "Registered Voter", "Residents marked as registered voters.", "#0891B2", 40),
			("SOLO_PARENT", "Solo Parent", "Residents marked as solo parents.", "#DB2777", 50),
			("YOUTH", "Youth", "Residents marked for the youth registry.", "#EA580C", 60),
			("INDIGENT", "Indigent", "Residents marked for indigent assistance tracking.", "#CA8A04", 70)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, string, string, int) tuple = array[i];
			MySqlCommand val = new MySqlCommand("\n                INSERT INTO resident_classification\n                    (barangay_id, classification_type, classification_key, name, description, color_hex, status, is_system, sort_order, created_at, updated_at)\n                SELECT b.barangay_id, 'CATEGORY', @key, @name, @description, @color, 'ACTIVE', 1, @sortOrder, NOW(), NOW()\n                FROM barangay b\n                WHERE NOT EXISTS (\n                    SELECT 1\n                    FROM resident_classification rc\n                    WHERE rc.barangay_id = b.barangay_id\n                      AND rc.classification_type = 'CATEGORY'\n                      AND rc.classification_key = @key\n                )", conn);
			try
			{
				val.Parameters.AddWithValue("@key", (object)tuple.Item1);
				val.Parameters.AddWithValue("@name", (object)tuple.Item2);
				val.Parameters.AddWithValue("@description", (object)tuple.Item3);
				val.Parameters.AddWithValue("@color", (object)tuple.Item4);
				val.Parameters.AddWithValue("@sortOrder", (object)tuple.Item5);
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	private static void TryExecuteIgnore(MySqlConnection conn, string sql)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		try
		{
			MySqlCommand val = new MySqlCommand(sql, conn);
			try
			{
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
		}
	}

	private static bool ColumnExists(MySqlConnection conn, string table, string column)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\r\n              FROM INFORMATION_SCHEMA.COLUMNS\r\n              WHERE TABLE_SCHEMA = DATABASE()\r\n                AND TABLE_NAME = @table\r\n                AND COLUMN_NAME = @column", conn);
		try
		{
			val.Parameters.AddWithValue("@table", (object)table);
			val.Parameters.AddWithValue("@column", (object)column);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar()) > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool TableExists(MySqlConnection conn, string table)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n              FROM INFORMATION_SCHEMA.TABLES\n              WHERE TABLE_SCHEMA = DATABASE()\r\n                AND TABLE_NAME = @table", conn);
		try
		{
			val.Parameters.AddWithValue("@table", (object)table);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar()) > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static string BuildReadyFingerprint(string connectionString)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MySqlConnectionStringBuilder val = new MySqlConnectionStringBuilder(connectionString, false);
			return string.Join("|", ((MySqlBaseConnectionStringBuilder)val).Server?.Trim().ToLowerInvariant() ?? string.Empty, ((MySqlBaseConnectionStringBuilder)val).Port.ToString(), ((MySqlBaseConnectionStringBuilder)val).Database?.Trim().ToLowerInvariant() ?? string.Empty, ((MySqlBaseConnectionStringBuilder)val).UserID?.Trim().ToLowerInvariant() ?? string.Empty, ((object)((MySqlBaseConnectionStringBuilder)val).SslMode/*cast due to constrained. prefix*/).ToString());
		}
		catch
		{
			return connectionString.Trim();
		}
	}
}
