using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;
using Microsoft.Data.Sqlite;

namespace baranggaysystem1.Database;

internal static class OfflineSyncStatus
{
    public const string Pending = "pending";
    public const string Synced = "synced";
}

internal sealed record DbParameterValue(string Name, object? Value);

internal sealed record SyncQueueItem(
    long QueueId,
    string TableName,
    string Operation,
    string SqlText,
    IReadOnlyList<DbParameterValue> Parameters,
    string DedupeKey);

internal static class OfflineTrackedTableCatalog
{
    public static readonly string[] All =
    {
        "role",
        "user_account",
        "user_role",
        "announcements",
        "household",
        "resident",
        "case_record",
        "document_request"
    };

    public static bool HasSyncStatus(string? tableName)
        => !string.IsNullOrWhiteSpace(tableName)
           && All.Contains(tableName, StringComparer.OrdinalIgnoreCase);
}

internal static class DbParameterMapper
{
    public static IReadOnlyList<DbParameterValue> Capture(Action<MySqlCommand>? configure)
    {
        if (configure == null)
        {
            return Array.Empty<DbParameterValue>();
        }

        using var command = new MySqlCommand();
        configure(command);
        return command.Parameters
            .Cast<MySqlParameter>()
            .Select(p => new DbParameterValue(p.ParameterName, NormalizeValue(p.Value)))
            .ToArray();
    }

    // NOTE: the offline database may be either MySQL or SQLite; use generic DbCommand
    public static void Apply(DbCommand command, IEnumerable<DbParameterValue>? parameters)
    {
        if (parameters == null)
        {
            return;
        }

        foreach (DbParameterValue parameter in parameters)
        {
            var p = command.CreateParameter();
            p.ParameterName = parameter.Name;
            p.Value = NormalizeValue(parameter.Value) ?? DBNull.Value;
            command.Parameters.Add(p);
        }
    }

    public static string Serialize(IEnumerable<DbParameterValue>? parameters)
    {
        return JsonSerializer.Serialize(parameters ?? Array.Empty<DbParameterValue>());
    }

    public static IReadOnlyList<DbParameterValue> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<DbParameterValue>();
        }

        List<DbParameterValue>? values = JsonSerializer.Deserialize<List<DbParameterValue>>(json);
        if (values == null)
        {
            return Array.Empty<DbParameterValue>();
        }

        return values
            .Select(v => new DbParameterValue(v.Name, NormalizeValue(v.Value)))
            .ToArray();
    }


    private static object? NormalizeValue(object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt32(out int intValue) => intValue,
                JsonValueKind.Number when element.TryGetInt64(out long longValue) => longValue,
                JsonValueKind.Number when element.TryGetDecimal(out decimal decimalValue) => decimalValue,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => element.ToString()
            };
        }

        return value;
    }
}

internal sealed class RemoteDatabaseService
{
    private const uint ConnectivityCheckTimeoutSeconds = 2;

    public bool CanConnect()
    {
        // Use a short timeout (2 seconds) for connectivity checks to avoid blocking
        return DBConnection.TryOpenCurrentWithTimeout(ConnectivityCheckTimeoutSeconds, out _);
    }

    public DataTable LoadTable(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(sql, conn);
        DbParameterMapper.Apply(cmd, parameters);
        using var adapter = new MySqlDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public int ExecuteNonQuery(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(sql, conn);
        DbParameterMapper.Apply(cmd, parameters);
        return cmd.ExecuteNonQuery();
    }

    public T? ExecuteScalar<T>(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(sql, conn);
        DbParameterMapper.Apply(cmd, parameters);
        object? result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            return default;
        }

        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }
}

internal sealed class LocalDatabaseService
{
    private static readonly Regex InsertColumnsRegex = new(@"^\s*INSERT\s+INTO\s+[`\[]?(?<table>[A-Za-z0-9_]+)[`\]]?\s*\((?<columns>.*?)\)\s*VALUES\s*\((?<values>.*?)\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex UpdateSetRegex = new(@"^\s*UPDATE\s+[`\[]?(?<table>[A-Za-z0-9_]+)[`\]]?\s+SET\s+(?<set>.*?)(\s+WHERE\s+.*)?$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TableNameRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    // This property is kept for compatibility; it returns the underlying connection string.
    public string DatabasePath => ConnectionString;

    // when no environment variable is provided we default to SQLite for portability
    private const string DefaultSqliteFile = "offline.db";

    private static bool UseSqlite
    {
        get
        {
            string? env = Environment.GetEnvironmentVariable("BARANGAY_OFFLINE_CONNECTION");
            if (string.IsNullOrWhiteSpace(env))
            {
                return true; // default engine
            }

            if (IsLikelySqlite(env))
            {
                return true;
            }

            AppLogger.LogWarning("BARANGAY_OFFLINE_CONNECTION is set but not recognized as SQLite. Defaulting to SQLite offline database for portability.");
            return true;
        }
    }

    private static string GetSqliteDatabasePath()
    {
        string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string offlineFolder = Path.Combine(baseFolder, "BarangaySystem", "Offline");
        Directory.CreateDirectory(offlineFolder);
        return Path.Combine(offlineFolder, DefaultSqliteFile);
    }

    private static bool IsLikelySqlite(string cs)
        => cs.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase) >= 0
           || cs.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
           || cs.IndexOf("sqlite", StringComparison.OrdinalIgnoreCase) >= 0;

    // MySQL defaults (used when offline engine is MySQL)
    private const string DefaultServer = "localhost";
    private const uint DefaultPort = 3306;
    private const string DefaultDatabase = "barangay_offline";
    private const string DefaultUser = "root";
    private const string DefaultPassword = "123456";

    private string ConnectionString
    {
        get
        {
            // allow override via environment variable for debugging/testing
            string? env = Environment.GetEnvironmentVariable("BARANGAY_OFFLINE_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env) && IsLikelySqlite(env))
            {
                return env;
            }

            if (!string.IsNullOrWhiteSpace(env) && !IsLikelySqlite(env))
            {
                AppLogger.LogWarning("BARANGAY_OFFLINE_CONNECTION is set but not recognized as SQLite; ignoring and using default local SQLite.");
            }

            if (UseSqlite)
            {
                string path = GetSqliteDatabasePath();
                return $"Data Source={path};Mode=ReadWriteCreate;Cache=Shared";
            }

            var def = new MySqlConnectionStringBuilder
            {
                Server = DefaultServer,
                Port = DefaultPort,
                Database = DefaultDatabase,
                UserID = DefaultUser,
                Password = DefaultPassword,
                SslMode = MySqlSslMode.Disabled,
                AllowUserVariables = true,
                AllowPublicKeyRetrieval = true,
                Pooling = true,
                MinimumPoolSize = 1,
                MaximumPoolSize = 60,
                ConnectionTimeout = 5
            };

            return def.ConnectionString;
        }
    }

    public void EnsureDatabaseInitialized()
    {
        try
        {
            if (UseSqlite)
            {
                string sqlitePath = GetSqliteDatabasePath();
                Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath) ?? AppContext.BaseDirectory);
                if (!File.Exists(sqlitePath))
                {
                    using var f = File.Create(sqlitePath);
                    AppLogger.LogInfo($"Created new offline SQLite database at: {sqlitePath}");
                }
                else
                {
                    AppLogger.LogInfo($"Using existing offline SQLite database at: {sqlitePath}");
                }

                using var conn = new SqliteConnection(ConnectionString);
                conn.Open();
                
                // SQLite needs each statement executed separately
                string bootstrapScript = LoadBootstrapScript();
                AppLogger.LogInfo($"Loaded bootstrap script length: {bootstrapScript.Length}");
                AppLogger.LogInfo("Bootstrap script sample (first 400 chars): " + bootstrapScript.Substring(0, Math.Min(400, bootstrapScript.Length)).Replace("\r\n", " "));
                
                // Debug: Log the script to find AUTO_INCREMENT
                if (bootstrapScript.Contains("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.LogWarning("WARNING: Bootstrap script contains AUTO_INCREMENT! This will cause SQLite errors.");
                    AppLogger.LogWarning($"First 500 chars of script: {bootstrapScript.Substring(0, Math.Min(500, bootstrapScript.Length))}");
                }
                
                string[] statements = bootstrapScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                AppLogger.LogInfo($"Bootstrap script split into {statements.Length} statements.");
                
                foreach (string statement in statements)
                {
                    string trimmedStatement = statement.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedStatement))
                        continue;
                    
                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = trimmedStatement;
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"Failed to execute statement: {trimmedStatement.Substring(0, Math.Min(100, trimmedStatement.Length))}...");
                        AppLogger.LogError($"Error: {ex.Message}");
                        throw;
                    }
                }
                
                AppLogger.LogInfo("Offline database schema bootstrap completed.");
                
                MigrationRunner.ApplyPendingMigrations(conn);
                AppLogger.LogInfo("Offline database migrations applied successfully.");
                
                // Seed default admin user if user_account is empty
                TrySeedDefaultAdminUser(conn);
            }
            else
            {
                // ensure the local MySQL database exists and then apply the schema
                using var conn = new MySqlConnection(ConnectionString);
                conn.Open();

                // create the database if it is missing, then switch to it
                using (var createCmd = conn.CreateCommand())
                {
                    createCmd.CommandText =
                        "CREATE DATABASE IF NOT EXISTS barangay_offline;" +
                        "USE barangay_offline;" +
                        LoadBootstrapScript();
                    createCmd.ExecuteNonQuery();
                }
                
                AppLogger.LogInfo("Offline database schema bootstrap completed (MySQL).");
                MigrationRunner.ApplyPendingMigrations(conn);
                AppLogger.LogInfo("Offline database migrations applied successfully (MySQL).");
                TrySeedDefaultAdminUser(conn);
            }
        }
        catch (Exception ex)
        {
            // log warning and continue; offline cache won't work but app can still run
            string engine = UseSqlite ? "SQLite" : "MySQL";
            AppLogger.LogError($"Failed to initialize offline {engine} database. Offline mode will be disabled.", ex);
        }
    }

    private void TrySeedDefaultAdminUser(DbConnection conn)
    {
        try
        {
            // Check if user_account table has any users
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM user_account";
            object? result = checkCmd.ExecuteScalar();
            int userCount = result != null ? Convert.ToInt32(result) : 0;

            if (userCount > 0)
            {
                AppLogger.LogInfo($"Offline user_account already has {userCount} users; skipping default admin seed.");
                return;
            }

            // Insert default admin user for offline mode
            string defaultAdminPassword = PasswordHelper.HashPassword("admin");
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            
            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO user_account 
                (barangay_id, username, password_hash, first_name, last_name, full_name, is_active, created_at, sync_status)
                VALUES 
                (1, 'admin', @password, 'System', 'Admin', 'System Admin', 1, @created_at, 'synced')";
            
            var paramPassword = insertCmd.CreateParameter();
            paramPassword.ParameterName = "@password";
            paramPassword.Value = defaultAdminPassword;
            insertCmd.Parameters.Add(paramPassword);

            var paramCreatedAt = insertCmd.CreateParameter();
            paramCreatedAt.ParameterName = "@created_at";
            paramCreatedAt.Value = now;
            insertCmd.Parameters.Add(paramCreatedAt);

            insertCmd.ExecuteNonQuery();

            // Insert admin role for the default user
            using var roleCmd = conn.CreateCommand();
            roleCmd.CommandText = @"
                INSERT INTO role (role_id, name, description, sync_status)
                VALUES (1, 'Admin', 'Administrator', 'synced')";
            roleCmd.ExecuteNonQuery();

            // Link admin role to the default user
            using var linkCmd = conn.CreateCommand();
            linkCmd.CommandText = @"
                INSERT INTO user_role (user_id, role_id, sync_status)
                VALUES (1, 1, 'synced')";
            linkCmd.ExecuteNonQuery();

            AppLogger.LogInfo("Seeded default admin user (username: admin, password: admin) for offline-first login.");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to seed default admin user for offline database.", ex);
        }
    }

    private DbConnection GetLocalConnection()
    {
        return UseSqlite
            ? (DbConnection)new SqliteConnection(ConnectionString)
            : new MySqlConnection(ConnectionString);
    }

    public DataTable LoadTable(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        using var conn = GetLocalConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SqlDialectTranslator.ToLocalSql(sql);
        DbParameterMapper.Apply(cmd, parameters);
        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public int ExecuteNonQuery(
        string sql,
        IEnumerable<DbParameterValue>? parameters,
        DatabaseOperationInfo operationInfo,
        bool queueForSync,
        string? syncStatus)
    {
        using var conn = GetLocalConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        string localSql = BuildLocalMutationSql(sql, operationInfo, syncStatus);
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = SqlDialectTranslator.ToLocalSql(localSql);
        DbParameterMapper.Apply(cmd, parameters);

        if (!string.IsNullOrWhiteSpace(syncStatus)
            && OfflineTrackedTableCatalog.HasSyncStatus(operationInfo.TableName))
        {
            bool has = false;
            foreach (DbParameter p in cmd.Parameters)
            {
                if (string.Equals(p.ParameterName, "@sync_status", StringComparison.OrdinalIgnoreCase))
                {
                    has = true;
                    break;
                }
            }

            if (!has)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@sync_status";
                p.Value = syncStatus;
                cmd.Parameters.Add(p);
            }
        }

        int affected = cmd.ExecuteNonQuery();

        if (queueForSync && operationInfo.IsWrite && !string.IsNullOrWhiteSpace(operationInfo.TableName))
        {
            InsertSyncQueue(conn, tx, operationInfo, sql, parameters);
        }

        tx.Commit();
        return affected;
    }

    public T? ExecuteScalar<T>(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        using var conn = GetLocalConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SqlDialectTranslator.ToLocalSql(sql);
        DbParameterMapper.Apply(cmd, parameters);
        object? result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            return default;
        }

        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<SyncQueueItem> LoadPendingQueue()
    {
        using var conn = GetLocalConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT queue_id, table_name, operation, sql_text, parameter_json, dedupe_key
            FROM sync_queue
            ORDER BY queue_id";

        using var reader = cmd.ExecuteReader();
        var items = new List<SyncQueueItem>();
        while (reader.Read())
        {
            items.Add(new SyncQueueItem(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DbParameterMapper.Deserialize(reader.IsDBNull(4) ? null : reader.GetString(4)),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5)));
        }

        return items;
    }

    public void DeletePendingQueue(IReadOnlyCollection<long> queueIds)
    {
        if (queueIds.Count == 0)
        {
            return;
        }

        using var conn = GetLocalConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"DELETE FROM sync_queue WHERE queue_id IN ({string.Join(", ", queueIds.Select((_, index) => $"@p{index}"))})";

        int parameterIndex = 0;
        foreach (long queueId in queueIds)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@p{parameterIndex}";
            p.Value = queueId;
            cmd.Parameters.Add(p);
            parameterIndex++;
        }

        cmd.ExecuteNonQuery();
        tx.Commit();
    }

    public void RecordSyncFailure(long queueId, string error)
    {
        using var conn = GetLocalConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE sync_queue
            SET retry_count = retry_count + 1,
                last_error = @error
            WHERE queue_id = @queueId";
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@error";
        p1.Value = error;
        cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@queueId";
        p2.Value = queueId;
        cmd.Parameters.Add(p2);
        cmd.ExecuteNonQuery();
    }

    public void ReplaceTrackedTable(string tableName, DataTable snapshot)
    {
        if (!TableNameRegex.IsMatch(tableName))
        {
            throw new InvalidOperationException($"Invalid table name '{tableName}'.");
        }

        using var conn = GetLocalConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        EnsureColumns(conn, tx, tableName, snapshot);

        using (var delete = conn.CreateCommand())
        {
            delete.Transaction = tx;
            delete.CommandText = $"DELETE FROM {tableName}";
            delete.ExecuteNonQuery();
        }

        if (snapshot.Columns.Count == 0)
        {
            tx.Commit();
            return;
        }

        var columnNames = snapshot.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        bool includeSyncStatus = OfflineTrackedTableCatalog.HasSyncStatus(tableName) && !columnNames.Contains("sync_status", StringComparer.OrdinalIgnoreCase);
        string insertColumns = string.Join(", ", columnNames);
        string insertValues = string.Join(", ", columnNames.Select(name => "@" + name));
        if (includeSyncStatus)
        {
            insertColumns += ", sync_status";
            insertValues += ", @sync_status";
        }

        foreach (DataRow row in snapshot.Rows)
        {
            using var insert = conn.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = $"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})";

            foreach (DataColumn column in snapshot.Columns)
            {
                var p = insert.CreateParameter();
                p.ParameterName = "@" + column.ColumnName;
                p.Value = row[column] == DBNull.Value ? DBNull.Value : row[column];
                insert.Parameters.Add(p);
            }

            if (includeSyncStatus)
            {
                var p = insert.CreateParameter();
                p.ParameterName = "@sync_status";
                p.Value = OfflineSyncStatus.Synced;
                insert.Parameters.Add(p);
            }

            insert.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private static string LoadBootstrapScript()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Database", "sqlite", "offline_bootstrap.sql"); // kept under sqlite folder for compatibility
        string script;
        if (File.Exists(path))
        {
            script = File.ReadAllText(path);
        }
        else
        {
            script = @"
CREATE TABLE IF NOT EXISTS role (
    role_id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);
CREATE TABLE IF NOT EXISTS user_account (
    user_id INTEGER PRIMARY KEY,
    barangay_id INTEGER,
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    first_name TEXT,
    middle_name TEXT,
    last_name TEXT,
    full_name TEXT,
    email TEXT,
    contact_no TEXT,
    position TEXT,
    department TEXT,
    last_project TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    photo_url TEXT,
    last_login_at TEXT,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);
CREATE TABLE IF NOT EXISTS user_role (
    user_id INTEGER NOT NULL,
    role_id INTEGER NOT NULL,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (user_id, role_id)
);
CREATE TABLE IF NOT EXISTS announcements (
    announcement_id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    body TEXT,
    priority TEXT,
    status TEXT,
    is_pinned INTEGER NOT NULL DEFAULT 0,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);
CREATE TABLE IF NOT EXISTS household (
    household_id INTEGER PRIMARY KEY,
    barangay_id INTEGER,
    purok_id INTEGER,
    household_no TEXT,
    address TEXT,
    head_resident_id INTEGER,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);
CREATE TABLE IF NOT EXISTS resident (
    resident_id INTEGER PRIMARY KEY,
    household_id INTEGER,
    barangay_id INTEGER,
    purok_id INTEGER,
    first_name TEXT,
    middle_name TEXT,
    last_name TEXT,
    birth_date TEXT,
    sex TEXT,
    civil_status TEXT,
    contact_no TEXT,
    status TEXT,
    photo BLOB,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);
CREATE TABLE IF NOT EXISTS case_record (
    case_id INTEGER PRIMARY KEY,
    complainant_id INTEGER,
    respondent_resident_id INTEGER,
    respondent_name TEXT,
    incident_type TEXT,
    incident_date TEXT,
    incident_time TEXT,
    status TEXT,
    incident_details TEXT,
    resolution_details TEXT,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);
CREATE TABLE IF NOT EXISTS document_request (
    doc_request_id INTEGER PRIMARY KEY,
    resident_id INTEGER,
    doc_type_id INTEGER,
    document_no TEXT,
    status TEXT,
    fee REAL,
    or_number TEXT,
    verification_token TEXT,
    expires_at TEXT,
    requested_at TEXT,
    approved_at TEXT,
    released_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);
CREATE TABLE IF NOT EXISTS sync_queue (
    queue_id INTEGER PRIMARY KEY AUTOINCREMENT,
    table_name TEXT NOT NULL,
    operation TEXT NOT NULL,
    sql_text TEXT NOT NULL,
    parameter_json TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dedupe_key TEXT NOT NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    UNIQUE (dedupe_key)
);
";
        }

        if (!UseSqlite)
        {
            // adjust minor syntax differences for MySQL
            script = script.Replace("AUTOINCREMENT", "AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase)
                           .Replace("INTEGER PRIMARY KEY", "BIGINT PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
        }

        return script;
    }

    private static string BuildLocalMutationSql(string sql, DatabaseOperationInfo info, string? syncStatus)
    {
        if (string.IsNullOrWhiteSpace(syncStatus) || !OfflineTrackedTableCatalog.HasSyncStatus(info.TableName))
        {
            return sql;
        }

        if (info.Kind == DatabaseOperationKind.Insert)
        {
            Match match = InsertColumnsRegex.Match(sql);
            if (match.Success && match.Groups["columns"].Value.IndexOf("sync_status", StringComparison.OrdinalIgnoreCase) < 0)
            {
                string columns = match.Groups["columns"].Value.Trim();
                string values = match.Groups["values"].Value.Trim();
                return sql.Replace(match.Value,
                    $"INSERT INTO {match.Groups["table"].Value} ({columns}, sync_status) VALUES ({values}, @sync_status)",
                    StringComparison.Ordinal);
            }
        }

        if (info.Kind == DatabaseOperationKind.Update)
        {
            Match match = UpdateSetRegex.Match(sql);
            if (match.Success && match.Groups["set"].Value.IndexOf("sync_status", StringComparison.OrdinalIgnoreCase) < 0)
            {
                string setClause = match.Groups["set"].Value.Trim();
                int whereIndex = sql.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
                if (whereIndex >= 0)
                {
                    string prefix = sql[..whereIndex];
                    string suffix = sql[whereIndex..];
                    return prefix + ", sync_status = @sync_status" + suffix;
                }

                return sql + ", sync_status = @sync_status";
            }
        }

        return sql;
    }

    private void InsertSyncQueue(
        DbConnection conn,
        DbTransaction tx,
        DatabaseOperationInfo operationInfo,
        string sql,
        IEnumerable<DbParameterValue>? parameters)
    {
        string serializedParameters = DbParameterMapper.Serialize(parameters);
        string dedupeKey = ComputeDedupeKey(operationInfo, sql, serializedParameters);

        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        if (UseSqlite)
        {
            cmd.CommandText = @"
            INSERT OR IGNORE INTO sync_queue (table_name, operation, sql_text, parameter_json, dedupe_key)
            VALUES (@tableName, @operation, @sql, @parameterJson, @dedupeKey)";
        }
        else
        {
            cmd.CommandText = @"
            INSERT IGNORE INTO sync_queue (table_name, operation, sql_text, parameter_json, dedupe_key)
            VALUES (@tableName, @operation, @sql, @parameterJson, @dedupeKey)";
        }

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@tableName";
        p1.Value = operationInfo.TableName ?? string.Empty;
        cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@operation";
        p2.Value = operationInfo.Kind.ToString().ToUpperInvariant();
        cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter();
        p3.ParameterName = "@sql";
        p3.Value = sql;
        cmd.Parameters.Add(p3);
        var p4 = cmd.CreateParameter();
        p4.ParameterName = "@parameterJson";
        p4.Value = serializedParameters;
        cmd.Parameters.Add(p4);
        var p5 = cmd.CreateParameter();
        p5.ParameterName = "@dedupeKey";
        p5.Value = dedupeKey;
        cmd.Parameters.Add(p5);

        cmd.ExecuteNonQuery();
    }

    private static string ComputeDedupeKey(DatabaseOperationInfo operationInfo, string sql, string serializedParameters)
    {
        string raw = $"{operationInfo.Kind}|{operationInfo.TableName}|{sql}|{serializedParameters}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private void EnsureColumns(DbConnection conn, DbTransaction tx, string tableName, DataTable snapshot)
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var check = conn.CreateCommand())
        {
            check.Transaction = tx;
            if (UseSqlite)
            {
                check.CommandText = $"PRAGMA table_info({tableName})";
                using var reader = check.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(1))
                    {
                        existingColumns.Add(reader.GetString(1));
                    }
                }
            }
            else
            {
                check.CommandText = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table";
                var p = check.CreateParameter();
                p.ParameterName = "@table";
                p.Value = tableName;
                check.Parameters.Add(p);
                using var reader = check.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        existingColumns.Add(reader.GetString(0));
                    }
                }
            }
        }

        foreach (DataColumn column in snapshot.Columns)
        {
            if (existingColumns.Contains(column.ColumnName))
            {
                continue;
            }

            using var alter = conn.CreateCommand();
            alter.Transaction = tx;
            alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {column.ColumnName} {MapMySqlType(column.DataType)}";
            alter.ExecuteNonQuery();
        }
    }

    private static string MapMySqlType(Type type)
    {
        if (UseSqlite)
        {
            // SQLite uses dynamic typing; use affinity names that make sense
            return type == typeof(byte[]) ? "BLOB"
                : type == typeof(short) || type == typeof(int) || type == typeof(long) ? "INTEGER"
                : type == typeof(bool) ? "INTEGER"
                : type == typeof(float) || type == typeof(double) ? "REAL"
                : type == typeof(decimal) ? "NUMERIC"
                : "TEXT";
        }

        // normal MySQL mapping
        return type == typeof(byte[]) ? "BLOB"
            : type == typeof(short) || type == typeof(int) ? "INT"
            : type == typeof(long) ? "BIGINT"
            : type == typeof(bool) ? "TINYINT(1)"
            : type == typeof(float) || type == typeof(double) ? "DOUBLE"
            : type == typeof(decimal) ? "DECIMAL(18,2)"
            : "TEXT";
    }
}

internal static class SqlDialectTranslator
{
    public static string ToLocalSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        string normalized = sql.Replace("`", string.Empty, StringComparison.Ordinal)
            .Replace("NOW()", "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase)
            .Replace("CURDATE()", "DATE('now')", StringComparison.OrdinalIgnoreCase)
            .Replace("UTC_TIMESTAMP()", "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase);

        return normalized;
    }
}