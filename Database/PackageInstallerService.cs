using System;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal sealed class PackageInstallRequest
{
    public DatabaseConnectionProfile ConnectionProfile { get; set; } = DatabaseConnectionProfile.CreateDefault();
    public string SuperAdminUsername { get; set; } = string.Empty;
    public string SuperAdminPassword { get; set; } = string.Empty;
    public string UserUsername { get; set; } = string.Empty;
    public string UserPassword { get; set; } = string.Empty;
}

internal sealed class ConnectionTestResult
{
    private ConnectionTestResult(bool success, bool databaseMissing, string message)
    {
        Success = success;
        DatabaseMissing = databaseMissing;
        Message = message;
    }

    public bool Success { get; }
    public bool DatabaseMissing { get; }
    public string Message { get; }

    public static ConnectionTestResult Pass(string message, bool databaseMissing = false)
    {
        return new ConnectionTestResult(true, databaseMissing, message);
    }

    public static ConnectionTestResult Fail(string message)
    {
        return new ConnectionTestResult(false, false, message);
    }
}

internal static class PackageInstallerService
{
    public static bool NeedsInstaller()
    {
        try
        {
            if (!DBConnection.TryOpenCurrent(out _))
            {
                return true;
            }

            using var conn = DBConnection.GetConnection();
            conn.Open();

            if (!TableExists(conn, "user_account"))
            {
                return true;
            }

            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM user_account", conn);
            int totalUsers = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            return totalUsers == 0;
        }
        catch
        {
            return true;
        }
    }

    public static ConnectionTestResult TestConnection(DatabaseConnectionProfile profile)
    {
        try
        {
            string dbConnection = DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: true);
            using var conn = new MySqlConnection(dbConnection);
            conn.Open();
            return ConnectionTestResult.Pass("Connection successful. Database is reachable.");
        }
        catch (MySqlException ex) when (ex.Number == 1049)
        {
            try
            {
                string serverConnection = DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: false);
                using var serverConn = new MySqlConnection(serverConnection);
                serverConn.Open();
                return ConnectionTestResult.Pass(
                    "Server connection successful. Database does not exist yet and will be created during install.",
                    databaseMissing: true);
            }
            catch (Exception nested)
            {
                return ConnectionTestResult.Fail($"Server connection failed: {nested.Message}");
            }
        }
        catch (Exception ex)
        {
            return ConnectionTestResult.Fail($"Connection failed: {ex.Message}");
        }
    }

    public static void Install(PackageInstallRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string superAdminUsername = request.SuperAdminUsername?.Trim() ?? string.Empty;
        string superAdminPassword = request.SuperAdminPassword ?? string.Empty;
        string userUsername = request.UserUsername?.Trim() ?? string.Empty;
        string userPassword = request.UserPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(superAdminUsername))
        {
            throw new InvalidOperationException("Super Admin username is required.");
        }

        if (string.IsNullOrWhiteSpace(superAdminPassword))
        {
            throw new InvalidOperationException("Super Admin password is required.");
        }

        if (string.IsNullOrWhiteSpace(userUsername))
        {
            throw new InvalidOperationException("User username is required.");
        }

        if (string.IsNullOrWhiteSpace(userPassword))
        {
            throw new InvalidOperationException("User password is required.");
        }

        if (string.Equals(superAdminUsername, userUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Super Admin and User usernames must be different.");
        }

        DatabaseConnectionProfile profile = request.ConnectionProfile ?? DatabaseConnectionProfile.CreateDefault();
        string serverConnection = DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: false);
        string databaseConnection = DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: true);

        using (var serverConn = new MySqlConnection(serverConnection))
        {
            serverConn.Open();
            EnsureDatabaseExists(serverConn, profile.Database);
        }

        DbConnectionSettingsStore.Save(profile);
        DBConnection.SetRuntimeConnectionString(databaseConnection);

        // Apply migrations and compatibility schema before account seeding.
        SchemaGuard.EnsureDatabaseReady();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        SchemaBootstrap.EnsureCoreDefaults(conn);

        using var tx = conn.BeginTransaction();
        int superAdminRoleId = EnsureRole(conn, tx, "Super Admin", "Primary system owner.");
        int staffRoleId = EnsureRole(conn, tx, "Staff", "Staff account.");

        UpsertUserWithRole(
            conn,
            tx,
            superAdminUsername,
            superAdminPassword,
            "Super Admin",
            superAdminRoleId);

        UpsertUserWithRole(
            conn,
            tx,
            userUsername,
            userPassword,
            "Default User",
            staffRoleId);

        tx.Commit();
    }

    private static void EnsureDatabaseExists(MySqlConnection serverConn, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Database name is required.");
        }

        string escaped = databaseName.Trim().Replace("`", "``", StringComparison.Ordinal);
        using var cmd = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{escaped}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", serverConn);
        cmd.ExecuteNonQuery();
    }

    private static int EnsureRole(MySqlConnection conn, MySqlTransaction tx, string roleName, string description)
    {
        using var find = new MySqlCommand("SELECT role_id FROM role WHERE name = @name LIMIT 1", conn, tx);
        find.Parameters.AddWithValue("@name", roleName);
        object? existing = find.ExecuteScalar();
        if (existing != null && existing != DBNull.Value)
        {
            return Convert.ToInt32(existing);
        }

        using var insert = new MySqlCommand("INSERT INTO role (name, description) VALUES (@name, @description)", conn, tx);
        insert.Parameters.AddWithValue("@name", roleName);
        insert.Parameters.AddWithValue("@description", description);
        insert.ExecuteNonQuery();
        return (int)insert.LastInsertedId;
    }

    private static void UpsertUserWithRole(
        MySqlConnection conn,
        MySqlTransaction tx,
        string username,
        string password,
        string fullName,
        int roleId)
    {
        int? userId = FindUserId(conn, tx, username);
        string passwordHash = PasswordHelper.HashPassword(password);

        if (userId == null)
        {
            using var insert = new MySqlCommand(
                @"INSERT INTO user_account
                    (barangay_id, username, password_hash, full_name, is_active, created_at, updated_at)
                  VALUES
                    (@barangayId, @username, @passwordHash, @fullName, 1, NOW(), NOW())",
                conn,
                tx);
            insert.Parameters.AddWithValue("@barangayId", SchemaDefaults.DefaultBarangayId);
            insert.Parameters.AddWithValue("@username", username);
            insert.Parameters.AddWithValue("@passwordHash", passwordHash);
            insert.Parameters.AddWithValue("@fullName", fullName);
            insert.ExecuteNonQuery();
            userId = (int)insert.LastInsertedId;
        }
        else
        {
            using var update = new MySqlCommand(
                @"UPDATE user_account
                  SET password_hash = @passwordHash,
                      full_name = CASE WHEN IFNULL(full_name, '') = '' THEN @fullName ELSE full_name END,
                      is_active = 1,
                      updated_at = NOW()
                  WHERE user_id = @userId",
                conn,
                tx);
            update.Parameters.AddWithValue("@passwordHash", passwordHash);
            update.Parameters.AddWithValue("@fullName", fullName);
            update.Parameters.AddWithValue("@userId", userId.Value);
            update.ExecuteNonQuery();
        }

        using (var clearRole = new MySqlCommand("DELETE FROM user_role WHERE user_id = @userId", conn, tx))
        {
            clearRole.Parameters.AddWithValue("@userId", userId.Value);
            clearRole.ExecuteNonQuery();
        }

        using (var setRole = new MySqlCommand("INSERT INTO user_role (user_id, role_id) VALUES (@userId, @roleId)", conn, tx))
        {
            setRole.Parameters.AddWithValue("@userId", userId.Value);
            setRole.Parameters.AddWithValue("@roleId", roleId);
            setRole.ExecuteNonQuery();
        }
    }

    private static int? FindUserId(MySqlConnection conn, MySqlTransaction tx, string username)
    {
        using var cmd = new MySqlCommand("SELECT user_id FROM user_account WHERE username = @username LIMIT 1", conn, tx);
        cmd.Parameters.AddWithValue("@username", username);
        object? value = cmd.ExecuteScalar();
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }

    private static bool TableExists(MySqlConnection conn, string table)
    {
        using var cmd = new MySqlCommand(
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table",
            conn);
        cmd.Parameters.AddWithValue("@table", table);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
