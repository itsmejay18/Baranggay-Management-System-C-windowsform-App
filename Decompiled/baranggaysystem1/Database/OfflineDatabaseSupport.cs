using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class OfflineDatabaseSupport
{
	private const string AppFolder = "BarangaySystem";

	private const string DbFileName = "barangay_system.db";

	private const string RelativeDatabaseDirectory = "Database";

	private const string RelativeSqliteDirectory = "sqlite";

	private const string OfflineAdminPasswordEnv = "BARANGAY_OFFLINE_ADMIN_PASSWORD";

	private const string OfflineAdminUsernameEnv = "BARANGAY_OFFLINE_ADMIN_USERNAME";

	private const string BootstrapAdminPasswordEnv = "BARANGAY_BOOTSTRAP_ADMIN_PASSWORD";

	private const string BootstrapAdminUsernameEnv = "BARANGAY_BOOTSTRAP_ADMIN_USERNAME";

	private const string PsgcAreaTableName = "ph_psgc_area";

	private static readonly string[] SqliteDemoSeedMigrationNames = new string[2] { "20260309_seed_30_records_30_transactions_reports.sql", "20260428_ph_public_reference_seed.sql" };

	private static string? _dbPath;

	private static bool _bootstrapped;

	private static readonly object _lock = new object();

	public static AppConnectionMode CurrentMode { get; private set; } = AppConnectionMode.Online;

	public static bool IsOffline => CurrentMode == AppConnectionMode.Offline;

	public static bool IsAvailable
	{
		get
		{
			lock (_lock)
			{
				return _bootstrapped;
			}
		}
	}

	public static void ActivateOfflineMode()
	{
		CurrentMode = AppConnectionMode.Offline;
		AppLogger.LogInfo("[Offline] Switched to offline mode.");
	}

	public static void ActivateOnlineMode()
	{
		CurrentMode = AppConnectionMode.Online;
		AppLogger.LogInfo("[Offline] Switched to online mode.");
	}

	public static string GetDatabasePath()
	{
		lock (_lock)
		{
			return _dbPath ?? ResolveDbPath();
		}
	}

	public static bool TryAuthenticateOffline(string username, string password, out int userId, out int barangayId, out string role)
	{
		userId = 0;
		barangayId = 1;
		role = string.Empty;
		try
		{
			if (!EnsureInitialised())
			{
				return false;
			}
			SqliteConnection connection = GetConnection();
			try
			{
				SqliteCommand val = connection.CreateCommand();
				try
				{
					((DbCommand)(object)val).CommandText = "\n                    SELECT ua.user_id,\n                           IFNULL(ua.barangay_id, 1) AS barangay_id,\n                           COALESCE(r.name, 'Staff') AS role,\n                           ua.password_hash\n                    FROM user_account ua\n                    LEFT JOIN user_role ur ON ur.user_id = ua.user_id\n                    LEFT JOIN role r ON r.role_id = ur.role_id\n                    WHERE LOWER(ua.username) = LOWER($username)\n                      AND IFNULL(ua.is_active, 1) = 1\n                    ORDER BY CASE\n                        WHEN r.name = 'Super Admin' THEN 2\n                        WHEN r.name = 'Admin' THEN 1\n                        ELSE 0\n                    END DESC\n                    LIMIT 1;";
					val.Parameters.AddWithValue("$username", (object)username);
					SqliteDataReader val2 = val.ExecuteReader();
					try
					{
						if (!((DbDataReader)(object)val2).Read())
						{
							return false;
						}
						string storedHash = ((((DbDataReader)(object)val2)["password_hash"] == DBNull.Value) ? string.Empty : (Convert.ToString(((DbDataReader)(object)val2)["password_hash"]) ?? string.Empty));
						string upgradedHash;
						PasswordHelper.VerificationResult verificationResult = PasswordHelper.VerifyPassword(password, storedHash, out upgradedHash);
						if (verificationResult == PasswordHelper.VerificationResult.Failed)
						{
							return false;
						}
						userId = Convert.ToInt32(((DbDataReader)(object)val2)["user_id"]);
						barangayId = ((((DbDataReader)(object)val2)["barangay_id"] == DBNull.Value) ? 1 : Convert.ToInt32(((DbDataReader)(object)val2)["barangay_id"]));
						role = Convert.ToString(((DbDataReader)(object)val2)["role"]) ?? "Staff";
						((DbDataReader)(object)val2).Close();
						if (verificationResult == PasswordHelper.VerificationResult.SuccessRehashNeeded && !string.IsNullOrWhiteSpace(upgradedHash))
						{
							TryUpgradeOfflinePasswordHash(connection, userId, upgradedHash);
						}
						return true;
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
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("[Offline] SQLite login failed.", ex);
			return false;
		}
	}

	public static bool EnsureInitialised()
	{
		lock (_lock)
		{
			if (_bootstrapped)
			{
				return true;
			}
			try
			{
				_dbPath = ResolveDbPath();
				Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
				bool flag = !File.Exists(_dbPath);
				SqliteConnection val = OpenConnectionInternal(_dbPath);
				try
				{
					((DbConnection)(object)val).Open();
					OfflineSqlCompat.RegisterFunctions(val);
					// Always run bootstrap - it uses CREATE TABLE IF NOT EXISTS so it's idempotent.
					// This repairs older databases that are missing tables.
					RunBootstrapSql(val);
					EnsureCompatibilitySchema(val);
					EnsureDemoSeedData(val);
					EnsureTemporaryAdminAccount(val);
					_bootstrapped = true;
					AppLogger.LogInfo("[Offline] SQLite database ready at: " + _dbPath);
					return true;
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogError("[Offline] Failed to initialise SQLite database.", ex);
				return false;
			}
		}
	}

	public static SqliteConnection GetConnection()
	{
		string dbPath;
		lock (_lock)
		{
			if (!_bootstrapped || _dbPath == null)
			{
				throw new InvalidOperationException("Offline database is not initialised. Call EnsureInitialised() first.");
			}
			dbPath = _dbPath;
		}
		SqliteConnection val = OpenConnectionInternal(dbPath);
		((DbConnection)(object)val).Open();
		OfflineSqlCompat.RegisterFunctions(val);
		SqliteCommand val2 = val.CreateCommand();
		try
		{
			((DbCommand)(object)val2).CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
			((DbCommand)(object)val2).ExecuteNonQuery();
			return val;
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	private static string ResolveDbPath()
	{
		string text = Path.Combine(AppContext.BaseDirectory, "Database", "sqlite", "barangay_system.db");
		if (TryEnsureWritableDirectory(Path.GetDirectoryName(text)))
		{
			return text;
		}
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BarangaySystem", "barangay_system.db");
	}

	private static bool TryEnsureWritableDirectory(string? directoryPath)
	{
		if (string.IsNullOrWhiteSpace(directoryPath))
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(directoryPath);
			string path = Path.Combine(directoryPath, ".write-test.tmp");
			File.WriteAllText(path, "ok");
			File.Delete(path);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static SqliteConnection OpenConnectionInternal(string path)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		return new SqliteConnection(((object)new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = (SqliteOpenMode)0
		}).ToString());
	}

	private static void RunBootstrapSql(SqliteConnection conn)
	{
		RunSqlScript(conn, ResolveSqliteScript("offline_bootstrap.sql"), "[Offline] Bootstrap SQL applied successfully.");
	}

	private static void EnsureTemporaryAdminAccount(SqliteConnection conn)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("BARANGAY_OFFLINE_ADMIN_PASSWORD");
		if (string.IsNullOrWhiteSpace(environmentVariable))
		{
			environmentVariable = Environment.GetEnvironmentVariable("BARANGAY_BOOTSTRAP_ADMIN_PASSWORD");
		}
		if (string.IsNullOrWhiteSpace(environmentVariable))
		{
			return;
		}
		string text = Environment.GetEnvironmentVariable("BARANGAY_OFFLINE_ADMIN_USERNAME");
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Environment.GetEnvironmentVariable("BARANGAY_BOOTSTRAP_ADMIN_USERNAME");
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "admin";
		}
		string text2 = PasswordHelper.HashPassword(environmentVariable);
		SqliteCommand val = conn.CreateCommand();
		long num;
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT role_id FROM role WHERE name = 'Super Admin' LIMIT 1;";
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			if (obj == null || obj == DBNull.Value)
			{
				SqliteCommand val2 = conn.CreateCommand();
				try
				{
					((DbCommand)(object)val2).CommandText = "INSERT INTO role(role_id, name, description, sync_status) VALUES (1, 'Super Admin', 'Primary system owner', 'synced');";
					((DbCommand)(object)val2).ExecuteNonQuery();
					num = 1L;
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			else
			{
				num = Convert.ToInt64(obj);
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		SqliteCommand val3 = conn.CreateCommand();
		long num2;
		try
		{
			((DbCommand)(object)val3).CommandText = "SELECT user_id FROM user_account WHERE LOWER(username) = LOWER($username) LIMIT 1;";
			val3.Parameters.AddWithValue("$username", (object)text);
			object obj2 = ((DbCommand)(object)val3).ExecuteScalar();
			if (obj2 == null || obj2 == DBNull.Value)
			{
				SqliteCommand val4 = conn.CreateCommand();
				try
				{
					((DbCommand)(object)val4).CommandText = "SELECT IFNULL(MAX(user_id), 0) + 1 FROM user_account;";
					num2 = Convert.ToInt64(((DbCommand)(object)val4).ExecuteScalar() ?? ((object)1L));
					SqliteCommand val5 = conn.CreateCommand();
					try
					{
						((DbCommand)(object)val5).CommandText = "\n                        INSERT INTO user_account\n                            (user_id, barangay_id, username, password_hash, full_name, is_active, created_at, updated_at, sync_status)\n                        VALUES\n                            ($userId, 1, $username, $passwordHash, 'Bootstrap Admin', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'synced');";
						val5.Parameters.AddWithValue("$userId", (object)num2);
						val5.Parameters.AddWithValue("$username", (object)text);
						val5.Parameters.AddWithValue("$passwordHash", (object)text2);
						((DbCommand)(object)val5).ExecuteNonQuery();
					}
					finally
					{
						((IDisposable)val5)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val4)?.Dispose();
				}
			}
			else
			{
				num2 = Convert.ToInt64(obj2);
				SqliteCommand val6 = conn.CreateCommand();
				try
				{
					((DbCommand)(object)val6).CommandText = "\n                        UPDATE user_account\n                        SET is_active = 1,\n                            updated_at = CURRENT_TIMESTAMP\n                        WHERE user_id = $userId;";
					val6.Parameters.AddWithValue("$userId", (object)num2);
					((DbCommand)(object)val6).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val6)?.Dispose();
				}
			}
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
		SqliteCommand val7 = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val7).CommandText = "SELECT COUNT(*) FROM user_role WHERE user_id = $userId AND role_id = $roleId;";
			val7.Parameters.AddWithValue("$userId", (object)num2);
			val7.Parameters.AddWithValue("$roleId", (object)num);
			if (Convert.ToInt64(((DbCommand)(object)val7).ExecuteScalar() ?? ((object)0L)) == 0L)
			{
				SqliteCommand val8 = conn.CreateCommand();
				long num3;
				try
				{
					((DbCommand)(object)val8).CommandText = "SELECT IFNULL(MAX(user_role_id), 0) + 1 FROM user_role;";
					num3 = Convert.ToInt64(((DbCommand)(object)val8).ExecuteScalar() ?? ((object)1L));
				}
				finally
				{
					((IDisposable)val8)?.Dispose();
				}
				SqliteCommand val9 = conn.CreateCommand();
				try
				{
					((DbCommand)(object)val9).CommandText = "INSERT INTO user_role(user_role_id, user_id, role_id, sync_status) VALUES ($id, $userId, $roleId, 'synced');";
					val9.Parameters.AddWithValue("$id", (object)num3);
					val9.Parameters.AddWithValue("$userId", (object)num2);
					val9.Parameters.AddWithValue("$roleId", (object)num);
					((DbCommand)(object)val9).ExecuteNonQuery();
					return;
				}
				finally
				{
					((IDisposable)val9)?.Dispose();
				}
			}
		}
		finally
		{
			((IDisposable)val7)?.Dispose();
		}
	}

	private static void TryUpgradeOfflinePasswordHash(SqliteConnection conn, int userId, string upgradedHash)
	{
		try
		{
			SqliteCommand val = conn.CreateCommand();
			try
			{
				((DbCommand)(object)val).CommandText = "\n                    UPDATE user_account\n                    SET password_hash = $hash,\n                        updated_at = CURRENT_TIMESTAMP,\n                        sync_status = 'dirty'\n                    WHERE user_id = $userId;";
				val.Parameters.AddWithValue("$hash", (object)upgradedHash);
				val.Parameters.AddWithValue("$userId", (object)userId);
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

	private static void EnsureCompatibilitySchema(SqliteConnection conn)
	{
		// Missing core tables not in the original bootstrap
		CreateTableIfMissing(conn, "document_request", "\n                CREATE TABLE document_request (\n                    doc_request_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL DEFAULT 1,\n                    resident_id INTEGER,\n                    document_type_id INTEGER,\n                    document_no TEXT,\n                    status TEXT NOT NULL DEFAULT 'SUBMITTED',\n                    purpose TEXT,\n                    fee REAL NOT NULL DEFAULT 0,\n                    or_number TEXT,\n                    business_name TEXT,\n                    business_nature TEXT,\n                    verification_token TEXT,\n                    verification_token_created_at TEXT,\n                    expires_at TEXT,\n                    renewed_from_request_id INTEGER,\n                    renewal_notified_at TEXT,\n                    release_notified_at TEXT,\n                    print_count INTEGER NOT NULL DEFAULT 0,\n                    last_printed_at TEXT,\n                    remarks TEXT,\n                    requested_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    approved_at TEXT,\n                    released_at TEXT,\n                    cancelled_at TEXT,\n                    created_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "document_payment", "\n                CREATE TABLE document_payment (\n                    payment_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    doc_request_id INTEGER NOT NULL,\n                    amount REAL,\n                    or_no TEXT,\n                    payment_method TEXT,\n                    paid_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    received_by_user_id INTEGER,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "announcements", "\n                CREATE TABLE announcements (\n                    announcement_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL DEFAULT 1,\n                    title TEXT NOT NULL,\n                    body TEXT,\n                    priority TEXT NOT NULL DEFAULT 'Normal',\n                    status TEXT NOT NULL DEFAULT 'Published',\n                    is_pinned INTEGER NOT NULL DEFAULT 0,\n                    created_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "outbound_notification", "\n                CREATE TABLE outbound_notification (\n                    notification_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    dedupe_key TEXT,\n                    channel TEXT NOT NULL,\n                    recipient TEXT NOT NULL,\n                    subject TEXT,\n                    message TEXT NOT NULL,\n                    status TEXT NOT NULL DEFAULT 'PENDING',\n                    source_module TEXT,\n                    source_record_id INTEGER,\n                    template_key TEXT,\n                    scheduled_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sent_at TEXT,\n                    attempts INTEGER NOT NULL DEFAULT 0,\n                    last_error TEXT,\n                    created_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    UNIQUE (dedupe_key)\n                );");
		CreateTableIfMissing(conn, "outbound_notification_attempt", "\n                CREATE TABLE outbound_notification_attempt (\n                    attempt_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    notification_id INTEGER NOT NULL,\n                    attempt_no INTEGER NOT NULL,\n                    attempted_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    success INTEGER NOT NULL DEFAULT 0,\n                    response_code TEXT,\n                    response_message TEXT\n                );");
		CreateTableIfMissing(conn, "case_hearing", "\n                CREATE TABLE case_hearing (\n                    hearing_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    case_id INTEGER NOT NULL,\n                    schedule_at TEXT,\n                    venue TEXT,\n                    status TEXT NOT NULL DEFAULT 'SCHEDULED',\n                    minutes TEXT,\n                    result TEXT,\n                    created_by_user_id INTEGER,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "case_timeline", "\n                CREATE TABLE case_timeline (\n                    timeline_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    case_id INTEGER NOT NULL,\n                    event_type TEXT NOT NULL,\n                    event_title TEXT NOT NULL,\n                    event_details TEXT,\n                    from_status TEXT,\n                    to_status TEXT,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    created_by_user_id INTEGER,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "record_attachment", "\n                CREATE TABLE record_attachment (\n                    attachment_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    entity_type TEXT NOT NULL,\n                    entity_id INTEGER NOT NULL,\n                    file_name TEXT NOT NULL,\n                    file_ext TEXT,\n                    mime_type TEXT,\n                    file_size_bytes INTEGER NOT NULL DEFAULT 0,\n                    file_hash TEXT,\n                    file_blob BLOB NOT NULL,\n                    notes TEXT,\n                    uploaded_by_user_id INTEGER,\n                    uploaded_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "backup_run", "\n                CREATE TABLE backup_run (\n                    backup_run_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    started_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    ended_at TEXT,\n                    status TEXT NOT NULL DEFAULT 'RUNNING',\n                    backup_type TEXT NOT NULL DEFAULT 'FULL',\n                    base_started_at TEXT,\n                    base_backup_run_id INTEGER,\n                    file_path TEXT,\n                    file_size_bytes INTEGER,\n                    error_message TEXT,\n                    created_by_user_id INTEGER,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "emergency_contact", "\n                CREATE TABLE emergency_contact (\n                    contact_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL DEFAULT 1,\n                    category TEXT NOT NULL DEFAULT 'OTHER',\n                    agency_name TEXT NOT NULL,\n                    contact_person TEXT,\n                    phone_primary TEXT NOT NULL,\n                    phone_secondary TEXT,\n                    email TEXT,\n                    address TEXT,\n                    notes TEXT,\n                    is_priority INTEGER NOT NULL DEFAULT 0,\n                    is_active INTEGER NOT NULL DEFAULT 1,\n                    created_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced'\n                );");
		CreateTableIfMissing(conn, "expense_entry", "\n                CREATE TABLE expense_entry (\n                    expense_id INTEGER NOT NULL,\n                    barangay_id INTEGER NOT NULL,\n                    expense_date TEXT NOT NULL,\n                    expense_category TEXT NOT NULL,\n                    expense_title TEXT NOT NULL,\n                    payee_name TEXT,\n                    amount REAL NOT NULL DEFAULT 0.00,\n                    payment_method TEXT NOT NULL DEFAULT 'Cash',\n                    status TEXT NOT NULL DEFAULT 'POSTED',\n                    reference_no TEXT,\n                    notes TEXT,\n                    created_by_user_id INTEGER,\n                    updated_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    PRIMARY KEY (expense_id),\n                    CONSTRAINT fk_expense_entry_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_expense_entry_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,\n                    CONSTRAINT fk_expense_entry_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		CreateTableIfMissing(conn, "inventory_item", "\n                CREATE TABLE inventory_item (\n                    item_id INTEGER NOT NULL,\n                    barangay_id INTEGER NOT NULL,\n                    item_name TEXT NOT NULL,\n                    category TEXT NOT NULL,\n                    unit TEXT NOT NULL DEFAULT 'pcs',\n                    quantity_on_hand REAL NOT NULL DEFAULT 0.00,\n                    reorder_level REAL NOT NULL DEFAULT 0.00,\n                    unit_cost REAL NOT NULL DEFAULT 0.00,\n                    location TEXT,\n                    item_status TEXT NOT NULL DEFAULT 'ACTIVE',\n                    last_restocked_at TEXT,\n                    notes TEXT,\n                    created_by_user_id INTEGER,\n                    updated_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    PRIMARY KEY (item_id),\n                    CONSTRAINT fk_inventory_item_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_inventory_item_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,\n                    CONSTRAINT fk_inventory_item_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		CreateTableIfMissing(conn, "asset_record", "\n                CREATE TABLE asset_record (\n                    asset_id INTEGER NOT NULL,\n                    barangay_id INTEGER NOT NULL,\n                    asset_name TEXT NOT NULL,\n                    asset_category TEXT NOT NULL,\n                    asset_tag TEXT,\n                    acquisition_date TEXT,\n                    acquisition_cost REAL NOT NULL DEFAULT 0.00,\n                    assigned_location TEXT,\n                    custodian_name TEXT,\n                    condition_status TEXT NOT NULL DEFAULT 'GOOD',\n                    lifecycle_status TEXT NOT NULL DEFAULT 'ACTIVE',\n                    notes TEXT,\n                    created_by_user_id INTEGER,\n                    updated_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    PRIMARY KEY (asset_id),\n                    CONSTRAINT fk_asset_record_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_asset_record_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,\n                    CONSTRAINT fk_asset_record_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		CreateTableIfMissing(conn, "procurement_request", "\n                CREATE TABLE procurement_request (\n                    procurement_id INTEGER NOT NULL,\n                    barangay_id INTEGER NOT NULL,\n                    request_type TEXT NOT NULL DEFAULT 'PROCUREMENT',\n                    request_date TEXT NOT NULL,\n                    needed_by_date TEXT,\n                    request_title TEXT NOT NULL,\n                    procurement_category TEXT NOT NULL,\n                    vendor_name TEXT,\n                    requested_by_name TEXT,\n                    total_amount REAL NOT NULL DEFAULT 0.00,\n                    workflow_status TEXT NOT NULL DEFAULT 'DRAFT',\n                    purchase_order_no TEXT,\n                    approved_by_name TEXT,\n                    approved_at TEXT,\n                    item_summary TEXT,\n                    approval_notes TEXT,\n                    notes TEXT,\n                    created_by_user_id INTEGER,\n                    updated_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    PRIMARY KEY (procurement_id),\n                    CONSTRAINT fk_procurement_request_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_procurement_request_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,\n                    CONSTRAINT fk_procurement_request_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		CreateTableIfMissing(conn, "resident_classification", "\n                CREATE TABLE resident_classification (\n                    classification_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL,\n                    classification_type TEXT NOT NULL DEFAULT 'TAG',\n                    classification_key TEXT,\n                    name TEXT NOT NULL,\n                    description TEXT,\n                    color_hex TEXT,\n                    status TEXT NOT NULL DEFAULT 'ACTIVE',\n                    is_system INTEGER NOT NULL DEFAULT 0,\n                    sort_order INTEGER NOT NULL DEFAULT 0,\n                    created_by_user_id INTEGER,\n                    updated_by_user_id INTEGER,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    CONSTRAINT fk_resident_classification_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_resident_classification_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,\n                    CONSTRAINT fk_resident_classification_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		CreateTableIfMissing(conn, "ayuda_program", "\n                CREATE TABLE ayuda_program (\n                    program_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL,\n                    program_name TEXT NOT NULL,\n                    category TEXT NOT NULL,\n                    allocated_budget REAL NOT NULL DEFAULT 0.00,\n                    status TEXT NOT NULL DEFAULT 'ACTIVE',\n                    start_date TEXT NULL,\n                    end_date TEXT NULL,\n                    notes TEXT NULL,\n                    created_by_user_id INTEGER NULL,\n                    updated_by_user_id INTEGER NULL,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    CONSTRAINT fk_ayuda_program_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_ayuda_program_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,\n                    CONSTRAINT fk_ayuda_program_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		CreateTableIfMissing(conn, "ayuda_release_batch", "\n                CREATE TABLE ayuda_release_batch (\n                    batch_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    barangay_id INTEGER NOT NULL,\n                    program_id INTEGER NOT NULL,\n                    batch_reference TEXT NOT NULL,\n                    release_date TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    total_amount REAL NOT NULL DEFAULT 0.00,\n                    beneficiary_count INTEGER NOT NULL DEFAULT 0,\n                    notes TEXT NULL,\n                    report_file_path TEXT NULL,\n                    created_by_user_id INTEGER NULL,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    CONSTRAINT fk_ayuda_release_batch_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_ayuda_release_batch_program FOREIGN KEY (program_id) REFERENCES ayuda_program (program_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_ayuda_release_batch_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		CreateTableIfMissing(conn, "ayuda_release", "\n                CREATE TABLE ayuda_release (\n                    release_id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    program_id INTEGER NOT NULL,\n                    batch_id INTEGER NULL,\n                    resident_id INTEGER NOT NULL,\n                    batch_reference TEXT NULL,\n                    reference_no TEXT NOT NULL,\n                    amount REAL NOT NULL DEFAULT 0.00,\n                    released_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    release_status TEXT NOT NULL DEFAULT 'RELEASED',\n                    notes TEXT NULL,\n                    created_by_user_id INTEGER NULL,\n                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'synced',\n                    CONSTRAINT fk_ayuda_release_program FOREIGN KEY (program_id) REFERENCES ayuda_program (program_id) ON DELETE CASCADE,\n                    CONSTRAINT fk_ayuda_release_batch FOREIGN KEY (batch_id) REFERENCES ayuda_release_batch (batch_id) ON DELETE SET NULL,\n                    CONSTRAINT fk_ayuda_release_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE RESTRICT,\n                    CONSTRAINT fk_ayuda_release_created_by FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL\n                );");
		AddColumnIfMissing(conn, "purok_sitio", "is_active", "INTEGER NOT NULL DEFAULT 1");
		AddColumnIfMissing(conn, "purok_sitio", "latitude", "REAL NULL");
		AddColumnIfMissing(conn, "purok_sitio", "longitude", "REAL NULL");
		AddColumnIfMissing(conn, "household", "address_note", "TEXT NULL");
		AddColumnIfMissing(conn, "household", "latitude", "REAL NULL");
		AddColumnIfMissing(conn, "household", "longitude", "REAL NULL");
		AddColumnIfMissing(conn, "resident", "is_head_of_family", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "is_solo_parent", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "is_youth", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "is_indigent", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "photo_url", "TEXT NULL");
		AddColumnIfMissing(conn, "resident", "suffix", "TEXT NULL");
		AddColumnIfMissing(conn, "resident", "civil_status", "TEXT NULL");
		AddColumnIfMissing(conn, "resident", "is_pwd", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "is_4ps_beneficiary", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "is_registered_voter", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "education_level", "TEXT NULL");
		AddColumnIfMissing(conn, "resident", "occupation", "TEXT NULL");
		AddColumnIfMissing(conn, "resident", "deleted_at", "TEXT NULL");
		AddColumnIfMissing(conn, "resident", "deleted_by_user_id", "INTEGER NULL");
		AddColumnIfMissing(conn, "resident", "delete_reason", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "case_no", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "date_filed", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "incident_date", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "incident_time", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "incident_location", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "incident_type", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "summary", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "handled_by_user_id", "INTEGER NULL");
		AddColumnIfMissing(conn, "case_record", "respondent_resident_id", "INTEGER NULL");
		AddColumnIfMissing(conn, "case_record", "respondent_name", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "witness_names", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "action_taken", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "resolution_details", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "incident_details", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "action_at", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "recorded_by", "INTEGER NULL");
		AddColumnIfMissing(conn, "case_record", "complainant_id", "INTEGER NULL");
		AddColumnIfMissing(conn, "case_record", "referral_destination", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "closure_notes", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "closed_at", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "closed_by_user_id", "INTEGER NULL");
		AddColumnIfMissing(conn, "case_record", "ai_summary", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_key_points", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_category", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_category_confidence", "REAL NULL");
		AddColumnIfMissing(conn, "case_record", "ai_risk_level", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_risk_score", "INTEGER NULL");
		AddColumnIfMissing(conn, "case_record", "ai_risk_reasons", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_entities", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_recommended_next_action", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_model", "TEXT NULL");
		AddColumnIfMissing(conn, "case_record", "ai_processed_at", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "photo_url", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "first_name", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "middle_name", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "last_name", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "position", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "department", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "last_project", "TEXT NULL");
		AddColumnIfMissing(conn, "user_account", "must_change_password", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "audit_trail", "action_at", "TEXT NULL");
		AddColumnIfMissing(conn, "projects", "name", "TEXT NULL");
		AddColumnIfMissing(conn, "projects", "record_type", "TEXT NOT NULL DEFAULT 'Project'");
		AddColumnIfMissing(conn, "projects", "attendance_target", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "projects", "attendance_count", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "projects", "last_activity_date", "TEXT NULL");
		AddColumnIfMissing(conn, "projects", "outcome_status", "TEXT NOT NULL DEFAULT 'Pending'");
		AddColumnIfMissing(conn, "projects", "outcome_summary", "TEXT NULL");
		AddColumnIfMissing(conn, "ayuda_program", "sync_status", "TEXT NOT NULL DEFAULT 'synced'");
		AddColumnIfMissing(conn, "ayuda_release_batch", "report_file_path", "TEXT NULL");
		AddColumnIfMissing(conn, "ayuda_release_batch", "sync_status", "TEXT NOT NULL DEFAULT 'synced'");
		AddColumnIfMissing(conn, "ayuda_release", "batch_id", "INTEGER NULL");
		AddColumnIfMissing(conn, "ayuda_release", "batch_reference", "TEXT NULL");
		AddColumnIfMissing(conn, "ayuda_release", "sync_status", "TEXT NOT NULL DEFAULT 'synced'");
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "\n                UPDATE purok_sitio SET is_active = 1 WHERE is_active IS NULL;\n                UPDATE resident SET is_head_of_family = 0 WHERE is_head_of_family IS NULL;\n                UPDATE resident SET is_solo_parent = 0 WHERE is_solo_parent IS NULL;\n                UPDATE resident SET is_youth = 0 WHERE is_youth IS NULL;\n                UPDATE resident SET is_indigent = 0 WHERE is_indigent IS NULL;\n                UPDATE projects SET record_type = 'Project' WHERE record_type IS NULL OR TRIM(record_type) = '';\n                UPDATE projects SET attendance_target = 0 WHERE attendance_target IS NULL;\n                UPDATE projects SET attendance_count = 0 WHERE attendance_count IS NULL;\n                UPDATE projects SET outcome_status = 'Pending' WHERE outcome_status IS NULL OR TRIM(outcome_status) = '';\n                UPDATE projects SET outcome_summary = '' WHERE outcome_summary IS NULL;\n                UPDATE projects SET name = COALESCE(name, title) WHERE name IS NULL OR TRIM(name) = '';\n                UPDATE audit_trail SET action_at = COALESCE(action_at, created_at) WHERE action_at IS NULL;\n                UPDATE ayuda_release SET batch_reference = reference_no WHERE batch_reference IS NULL OR TRIM(batch_reference) = '';\n                UPDATE ayuda_release_batch SET report_file_path = '' WHERE report_file_path IS NULL;";
			((DbCommand)(object)val).ExecuteNonQuery();
			CreateIndexIfMissing(conn, "idx_expense_entry_barangay_date", "CREATE INDEX idx_expense_entry_barangay_date ON expense_entry (barangay_id, expense_date);");
			CreateIndexIfMissing(conn, "idx_expense_entry_category_status", "CREATE INDEX idx_expense_entry_category_status ON expense_entry (expense_category, status);");
			CreateIndexIfMissing(conn, "idx_inventory_item_barangay_status", "CREATE INDEX idx_inventory_item_barangay_status ON inventory_item (barangay_id, item_status);");
			CreateIndexIfMissing(conn, "idx_inventory_item_category", "CREATE INDEX idx_inventory_item_category ON inventory_item (category);");
			CreateIndexIfMissing(conn, "idx_asset_record_barangay_lifecycle", "CREATE INDEX idx_asset_record_barangay_lifecycle ON asset_record (barangay_id, lifecycle_status);");
			CreateIndexIfMissing(conn, "idx_asset_record_category_condition", "CREATE INDEX idx_asset_record_category_condition ON asset_record (asset_category, condition_status);");
			CreateIndexIfMissing(conn, "idx_procurement_request_barangay_status", "CREATE INDEX idx_procurement_request_barangay_status ON procurement_request (barangay_id, workflow_status);");
			CreateIndexIfMissing(conn, "idx_procurement_request_date", "CREATE INDEX idx_procurement_request_date ON procurement_request (request_date, needed_by_date);");
			CreateIndexIfMissing(conn, "idx_procurement_request_category_type", "CREATE INDEX idx_procurement_request_category_type ON procurement_request (procurement_category, request_type);");
			CreateIndexIfMissing(conn, "idx_resident_classification_type_status", "CREATE INDEX idx_resident_classification_type_status ON resident_classification (barangay_id, classification_type, status);");
			CreateIndexIfMissing(conn, "idx_resident_classification_name", "CREATE INDEX idx_resident_classification_name ON resident_classification (barangay_id, classification_type, name);");
			CreateIndexIfMissing(conn, "ux_resident_classification_key", "CREATE UNIQUE INDEX ux_resident_classification_key ON resident_classification (barangay_id, classification_type, classification_key);");
			CreateIndexIfMissing(conn, "idx_ayuda_program_barangay_status", "CREATE INDEX idx_ayuda_program_barangay_status ON ayuda_program (barangay_id, status);");
			CreateIndexIfMissing(conn, "idx_ayuda_release_batch_barangay_date", "CREATE INDEX idx_ayuda_release_batch_barangay_date ON ayuda_release_batch (barangay_id, release_date);");
			CreateIndexIfMissing(conn, "ux_ayuda_release_batch_reference", "CREATE UNIQUE INDEX ux_ayuda_release_batch_reference ON ayuda_release_batch (batch_reference);");
			CreateIndexIfMissing(conn, "idx_ayuda_release_program", "CREATE INDEX idx_ayuda_release_program ON ayuda_release (program_id, released_at);");
			CreateIndexIfMissing(conn, "idx_ayuda_release_batch", "CREATE INDEX idx_ayuda_release_batch ON ayuda_release (batch_id, released_at);");
			CreateIndexIfMissing(conn, "idx_ayuda_release_batch_reference", "CREATE INDEX idx_ayuda_release_batch_reference ON ayuda_release (batch_reference);");
			CreateIndexIfMissing(conn, "ux_ayuda_release_reference", "CREATE UNIQUE INDEX ux_ayuda_release_reference ON ayuda_release (reference_no);");
			EnsureResidentClassificationDefaults(conn);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void CreateTableIfMissing(SqliteConnection conn, string tableName, string createSql)
	{
		if (TableExists(conn, tableName))
		{
			return;
		}
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = createSql;
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void AddColumnIfMissing(SqliteConnection conn, string tableName, string columnName, string definition)
	{
		// Skip if table doesn't exist yet (avoids crashing on partially-initialized databases)
		if (!TableExists(conn, tableName))
		{
			return;
		}
		if (ColumnExists(conn, tableName, columnName))
		{
			return;
		}
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void CreateIndexIfMissing(SqliteConnection conn, string indexName, string createSql)
	{
		if (IndexExists(conn, indexName))
		{
			return;
		}
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = createSql;
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void EnsureResidentClassificationDefaults(SqliteConnection conn)
	{
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
			SqliteCommand val = conn.CreateCommand();
			try
			{
				((DbCommand)(object)val).CommandText = "\n                    INSERT INTO resident_classification\n                        (barangay_id, classification_type, classification_key, name, description, color_hex, status, is_system, sort_order, created_at, updated_at, sync_status)\n                    SELECT b.barangay_id, 'CATEGORY', $key, $name, $description, $color, 'ACTIVE', 1, $sortOrder, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'synced'\n                    FROM barangay b\n                    WHERE NOT EXISTS (\n                        SELECT 1\n                        FROM resident_classification rc\n                        WHERE rc.barangay_id = b.barangay_id\n                          AND rc.classification_type = 'CATEGORY'\n                          AND rc.classification_key = $key\n                    );";
				val.Parameters.AddWithValue("$key", (object)tuple.Item1);
				val.Parameters.AddWithValue("$name", (object)tuple.Item2);
				val.Parameters.AddWithValue("$description", (object)tuple.Item3);
				val.Parameters.AddWithValue("$color", (object)tuple.Item4);
				val.Parameters.AddWithValue("$sortOrder", (object)tuple.Item5);
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
	{
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "PRAGMA table_info(" + tableName + ");";
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					if (string.Equals((((DbDataReader)(object)val2)["name"] == DBNull.Value) ? string.Empty : (Convert.ToString(((DbDataReader)(object)val2)["name"]) ?? string.Empty), columnName, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				return false;
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

	private static bool TableExists(SqliteConnection conn, string tableName)
	{
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
			val.Parameters.AddWithValue("$name", (object)tableName);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool IndexExists(SqliteConnection conn, string indexName)
	{
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name;";
			val.Parameters.AddWithValue("$name", (object)indexName);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool MigrationExists(SqliteConnection conn, string migrationName)
	{
		if (!TableExists(conn, "schema_migrations"))
		{
			return false;
		}
		SqliteCommand val = conn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT COUNT(*) FROM schema_migrations WHERE migration_name = $name;";
			val.Parameters.AddWithValue("$name", (object)migrationName);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void EnsureDemoSeedData(SqliteConnection conn)
	{
		bool flag = true;
		string[] sqliteDemoSeedMigrationNames = SqliteDemoSeedMigrationNames;
		foreach (string migrationName in sqliteDemoSeedMigrationNames)
		{
			if (!MigrationExists(conn, migrationName))
			{
				flag = false;
				break;
			}
		}
		if (!flag || !TableExists(conn, "ph_psgc_area"))
		{
			RunSqlScript(conn, ResolveSqliteScript("seed_demo_data.sql"), "[Offline] SQLite demo/reference seed applied successfully.");
		}
	}

	private static string ResolveSqliteScript(string fileName)
	{
		string text = Path.Combine(AppContext.BaseDirectory, "Database", "sqlite", fileName);
		if (File.Exists(text))
		{
			return File.ReadAllText(text);
		}
		string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "sqlite", fileName);
		if (File.Exists(path))
		{
			return File.ReadAllText(path);
		}
		throw new FileNotFoundException(fileName + " not found. Ensure it is set to CopyToOutputDirectory in the project.", text);
	}

	private static void RunSqlScript(SqliteConnection conn, string sql, string successLogMessage)
	{
		SqliteTransaction val = conn.BeginTransaction();
		try
		{
			foreach (string item in SplitStatements(sql))
			{
				SqliteCommand val2 = conn.CreateCommand();
				try
				{
					val2.Transaction = val;
					((DbCommand)(object)val2).CommandText = item;
					((DbCommand)(object)val2).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			((DbTransaction)(object)val).Commit();
			AppLogger.LogInfo(successLogMessage);
		}
		catch
		{
			((DbTransaction)(object)val).Rollback();
			throw;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static IEnumerable<string> SplitStatements(string sql)
	{
		StringBuilder sb = new StringBuilder();
		string[] array = sql.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].TrimEnd('\r');
			string text2 = text.TrimStart();
			if (text2.StartsWith("--") || text2.Length == 0)
			{
				if (sb.Length > 0)
				{
					sb.AppendLine(text);
				}
				continue;
			}
			sb.AppendLine(text);
			if (text2.EndsWith(';'))
			{
				string text3 = sb.ToString().Trim();
				sb.Clear();
				if (text3.Length > 1)
				{
					yield return text3;
				}
			}
		}
		string text4 = sb.ToString().Trim();
		if (text4.Length > 1 && !text4.TrimStart().StartsWith("--"))
		{
			yield return text4;
		}
	}
}
