using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Reflection;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database
{
    internal enum AppConnectionMode
    {
        /// <summary>MySQL is reachable; full read/write against the remote database.</summary>
        Online,
        /// <summary>MySQL is unreachable; using the local SQLite read-only cache.</summary>
        Offline,
    }

    /// <summary>
    /// Manages the local SQLite offline database and the current connection mode.
    /// The database file lives in %AppData%\BarangaySystem\offline.db and is bootstrapped
    /// from Database/sqlite/offline_bootstrap.sql on first use.
    /// </summary>
    internal static class OfflineDatabaseSupport
    {
        private const string AppFolder = "BarangaySystem";
        private const string DbFileName = "offline.db";
        private const string OfflineAdminPasswordEnv = "BARANGAY_OFFLINE_ADMIN_PASSWORD";
        private const string OfflineAdminUsernameEnv = "BARANGAY_OFFLINE_ADMIN_USERNAME";
        private const string BootstrapAdminPasswordEnv = "BARANGAY_BOOTSTRAP_ADMIN_PASSWORD";
        private const string BootstrapAdminUsernameEnv = "BARANGAY_BOOTSTRAP_ADMIN_USERNAME";

        private static string? _dbPath;
        private static bool _bootstrapped;
        private static readonly object _lock = new();

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Current connection mode. Defaults to Online; set to Offline when MySQL is unreachable.</summary>
        public static AppConnectionMode CurrentMode { get; private set; } = AppConnectionMode.Online;

        /// <summary>True when the system is running against the local SQLite database.</summary>
        public static bool IsOffline => CurrentMode == AppConnectionMode.Offline;

        /// <summary>Switches the app into offline mode. Should be called from Program.cs when MySQL setup fails.</summary>
        public static void ActivateOfflineMode()
        {
            CurrentMode = AppConnectionMode.Offline;
            AppLogger.LogInfo("[Offline] Switched to offline mode.");
        }

        /// <summary>Switches the app back to online mode (e.g. after a connection is restored).</summary>
        public static void ActivateOnlineMode()
        {
            CurrentMode = AppConnectionMode.Online;
            AppLogger.LogInfo("[Offline] Switched to online mode.");
        }

        /// <summary>Returns true when the offline database has been initialised.</summary>
        public static bool IsAvailable
        {
            get
            {
                lock (_lock) { return _bootstrapped; }
            }
        }

        public static bool TryAuthenticateOffline(
            string username,
            string password,
            out int userId,
            out int barangayId,
            out string role)
        {
            userId = 0;
            barangayId = SchemaDefaults.DefaultBarangayId;
            role = string.Empty;

            try
            {
                if (!EnsureInitialised())
                {
                    return false;
                }

                using var conn = GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT ua.user_id,
                           IFNULL(ua.barangay_id, 1) AS barangay_id,
                           COALESCE(r.name, 'Staff') AS role,
                           ua.password_hash
                    FROM user_account ua
                    LEFT JOIN user_role ur ON ur.user_id = ua.user_id
                    LEFT JOIN role r ON r.role_id = ur.role_id
                    WHERE LOWER(ua.username) = LOWER($username)
                      AND IFNULL(ua.is_active, 1) = 1
                    ORDER BY CASE
                        WHEN r.name = 'Super Admin' THEN 2
                        WHEN r.name = 'Admin' THEN 1
                        ELSE 0
                    END DESC
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("$username", username);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return false;
                }

                string storedHash = reader["password_hash"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(reader["password_hash"]) ?? string.Empty;

                var verification = PasswordHelper.VerifyPassword(password, storedHash, out string? upgradedHash);
                if (verification == PasswordHelper.VerificationResult.Failed)
                {
                    return false;
                }

                userId = Convert.ToInt32(reader["user_id"]);
                barangayId = reader["barangay_id"] == DBNull.Value
                    ? SchemaDefaults.DefaultBarangayId
                    : Convert.ToInt32(reader["barangay_id"]);
                role = Convert.ToString(reader["role"]) ?? "Staff";

                reader.Close();

                if (verification == PasswordHelper.VerificationResult.SuccessRehashNeeded
                    && !string.IsNullOrWhiteSpace(upgradedHash))
                {
                    TryUpgradeOfflinePasswordHash(conn, userId, upgradedHash);
                }

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("[Offline] SQLite login failed.", ex);
                return false;
            }
        }

        /// <summary>
        /// Ensures the offline database file exists and is bootstrapped.
        /// Safe to call multiple times; subsequent calls are no-ops.
        /// </summary>
        /// <returns>True on success, false if bootstrap failed.</returns>
        public static bool EnsureInitialised()
        {
            lock (_lock)
            {
                if (_bootstrapped)
                    return true;

                try
                {
                    _dbPath = ResolveDbPath();
                    Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

                    bool isNewDb = !File.Exists(_dbPath);

                    using var conn = OpenConnectionInternal(_dbPath);
                    conn.Open();

                    if (isNewDb)
                    {
                        RunBootstrapSql(conn);
                    }

                    EnsureTemporaryAdminAccount(conn);

                    _bootstrapped = true;
                    AppLogger.LogInfo($"[Offline] SQLite database ready at: {_dbPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"[Offline] Failed to initialise SQLite database.", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// Opens and returns a new SQLite connection to the offline database.
        /// Caller is responsible for disposing it.
        /// Throws <see cref="InvalidOperationException"/> if the database is not initialised yet.
        /// </summary>
        public static SqliteConnection GetConnection()
        {
            string path;
            lock (_lock)
            {
                if (!_bootstrapped || _dbPath == null)
                    throw new InvalidOperationException(
                        "Offline database is not initialised. Call EnsureInitialised() first.");
                path = _dbPath;
            }

            var conn = OpenConnectionInternal(path);
            conn.Open();
            OfflineSqlCompat.RegisterFunctions(conn);

            // Enable WAL for better concurrent read/write performance.
            using var walCmd = conn.CreateCommand();
            walCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            walCmd.ExecuteNonQuery();

            return conn;
        }

        // -------------------------------------------------------------------------
        // Internal helpers
        // -------------------------------------------------------------------------

        private static string ResolveDbPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppFolder, DbFileName);
        }

        private static SqliteConnection OpenConnectionInternal(string path)
        {
            // Mode=ReadWriteCreate creates the file if it doesn't exist.
            string cs = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
            }.ToString();

            return new SqliteConnection(cs);
        }

        private static void RunBootstrapSql(SqliteConnection conn)
        {
            string sql = ResolveBootstrapSql();

            // SQLite doesn't support multi-statement Execute; split on ';' delimiters.
            // We use a transaction for speed (thousands of INSERTs otherwise each auto-commit).
            using var tx = conn.BeginTransaction();
            try
            {
                foreach (string statement in SplitStatements(sql))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = statement;
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
                AppLogger.LogInfo("[Offline] Bootstrap SQL applied successfully.");
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static void EnsureTemporaryAdminAccount(SqliteConnection conn)
        {
            string? password = Environment.GetEnvironmentVariable(OfflineAdminPasswordEnv);
            if (string.IsNullOrWhiteSpace(password))
            {
                password = Environment.GetEnvironmentVariable(BootstrapAdminPasswordEnv);
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            string username = Environment.GetEnvironmentVariable(OfflineAdminUsernameEnv);
            if (string.IsNullOrWhiteSpace(username))
            {
                username = Environment.GetEnvironmentVariable(BootstrapAdminUsernameEnv);
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                username = "admin";
            }

            string hash = PasswordHelper.HashPassword(password);

            long superAdminRoleId;
            using (var roleCmd = conn.CreateCommand())
            {
                roleCmd.CommandText = "SELECT role_id FROM role WHERE name = 'Super Admin' LIMIT 1;";
                object? roleObj = roleCmd.ExecuteScalar();
                if (roleObj == null || roleObj == DBNull.Value)
                {
                    using var roleInsert = conn.CreateCommand();
                    roleInsert.CommandText =
                        "INSERT INTO role(role_id, name, description, sync_status) VALUES (1, 'Super Admin', 'Primary system owner', 'synced');";
                    roleInsert.ExecuteNonQuery();
                    superAdminRoleId = 1;
                }
                else
                {
                    superAdminRoleId = Convert.ToInt64(roleObj);
                }
            }

            long adminUserId;
            using (var userFind = conn.CreateCommand())
            {
                userFind.CommandText = "SELECT user_id FROM user_account WHERE LOWER(username) = LOWER($username) LIMIT 1;";
                userFind.Parameters.AddWithValue("$username", username);
                object? existingUser = userFind.ExecuteScalar();
                if (existingUser == null || existingUser == DBNull.Value)
                {
                    using var maxUserCmd = conn.CreateCommand();
                    maxUserCmd.CommandText = "SELECT IFNULL(MAX(user_id), 0) + 1 FROM user_account;";
                    adminUserId = Convert.ToInt64(maxUserCmd.ExecuteScalar() ?? 1L);

                    using var userInsert = conn.CreateCommand();
                    userInsert.CommandText = @"
                        INSERT INTO user_account
                            (user_id, barangay_id, username, password_hash, full_name, is_active, created_at, updated_at, sync_status)
                        VALUES
                            ($userId, 1, $username, $passwordHash, 'Bootstrap Admin', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'synced');";
                    userInsert.Parameters.AddWithValue("$userId", adminUserId);
                    userInsert.Parameters.AddWithValue("$username", username);
                    userInsert.Parameters.AddWithValue("$passwordHash", hash);
                    userInsert.ExecuteNonQuery();
                }
                else
                {
                    adminUserId = Convert.ToInt64(existingUser);
                    using var userUpdate = conn.CreateCommand();
                    userUpdate.CommandText = @"
                        UPDATE user_account
                        SET is_active = 1,
                            updated_at = CURRENT_TIMESTAMP
                        WHERE user_id = $userId;";
                    userUpdate.Parameters.AddWithValue("$userId", adminUserId);
                    userUpdate.ExecuteNonQuery();
                }
            }

            using (var existsRole = conn.CreateCommand())
            {
                existsRole.CommandText = "SELECT COUNT(*) FROM user_role WHERE user_id = $userId AND role_id = $roleId;";
                existsRole.Parameters.AddWithValue("$userId", adminUserId);
                existsRole.Parameters.AddWithValue("$roleId", superAdminRoleId);
                long existingRoleCount = Convert.ToInt64(existsRole.ExecuteScalar() ?? 0L);
                if (existingRoleCount == 0)
                {
                    long userRoleId;
                    using (var maxUserRoleCmd = conn.CreateCommand())
                    {
                        maxUserRoleCmd.CommandText = "SELECT IFNULL(MAX(user_role_id), 0) + 1 FROM user_role;";
                        userRoleId = Convert.ToInt64(maxUserRoleCmd.ExecuteScalar() ?? 1L);
                    }

                    using var addUserRole = conn.CreateCommand();
                    addUserRole.CommandText =
                        "INSERT INTO user_role(user_role_id, user_id, role_id, sync_status) VALUES ($id, $userId, $roleId, 'synced');";
                    addUserRole.Parameters.AddWithValue("$id", userRoleId);
                    addUserRole.Parameters.AddWithValue("$userId", adminUserId);
                    addUserRole.Parameters.AddWithValue("$roleId", superAdminRoleId);
                    addUserRole.ExecuteNonQuery();
                }
            }
        }

        private static void TryUpgradeOfflinePasswordHash(SqliteConnection conn, int userId, string upgradedHash)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE user_account
                    SET password_hash = $hash,
                        updated_at = CURRENT_TIMESTAMP,
                        sync_status = 'dirty'
                    WHERE user_id = $userId;";
                cmd.Parameters.AddWithValue("$hash", upgradedHash);
                cmd.Parameters.AddWithValue("$userId", userId);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Ignore hash upgrade failures (login already succeeded).
            }
        }

        private static string ResolveBootstrapSql()
        {
            // 1. Try next to the executing assembly (CopyToOutputDirectory set in csproj).
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
            string candidate = Path.Combine(exeDir, "Database", "sqlite", "offline_bootstrap.sql");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);

            // 2. Project source tree (dev/debug fallback).
            string devCandidate = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Database", "sqlite", "offline_bootstrap.sql");
            if (File.Exists(devCandidate))
                return File.ReadAllText(devCandidate);

            throw new FileNotFoundException(
                "offline_bootstrap.sql not found. Ensure it is set to CopyToOutputDirectory in the project.",
                candidate);
        }

        /// <summary>
        /// Splits a SQL script into individual statements by semicolons,
        /// skipping blank lines and comment-only lines.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<string> SplitStatements(string sql)
        {
            // Simple split: statements are separated by ';' at end of non-comment lines.
            // This is sufficient for the bootstrap SQL we generate (no stored procedures).
            var sb = new System.Text.StringBuilder();
            foreach (string raw in sql.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                string trimmed = line.TrimStart();

                // Skip pure comment lines and empty lines while building a statement.
                if (trimmed.StartsWith("--") || trimmed.Length == 0)
                {
                    if (sb.Length > 0)
                        sb.AppendLine(line);
                    continue;
                }

                sb.AppendLine(line);

                if (trimmed.EndsWith(';'))
                {
                    string stmt = sb.ToString().Trim();
                    sb.Clear();
                    if (stmt.Length > 1)
                        yield return stmt;
                }
            }

            // Flush any trailing statement without a terminating semicolon.
            string tail = sb.ToString().Trim();
            if (tail.Length > 1 && !tail.TrimStart().StartsWith("--"))
                yield return tail;
        }
    }
}
