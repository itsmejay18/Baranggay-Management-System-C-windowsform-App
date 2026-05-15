using System;
using System.Data.Common;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

/// <summary>
/// Automatically creates the MySQL database and bootstraps the schema
/// if the database does not exist when connecting.
/// </summary>
internal static class DatabaseAutoCreator
{
    /// <summary>
    /// Checks if the target database exists on the server. If not, creates it
    /// and runs schema bootstrap so the system is immediately usable.
    /// Returns true if the database was created (or already existed and is ready).
    /// </summary>
    public static bool EnsureDatabaseExists(DatabaseConnectionProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.Database))
            return false;

        string serverConnStr = BuildServerOnlyConnectionString(profile);
        string databaseName = profile.Database.Trim();

        try
        {
            // Step 1: Connect to the server (no database selected)
            using var serverConn = new MySqlConnection(serverConnStr);
            ((DbConnection)(object)serverConn).Open();

            // Step 2: Check if database exists
            if (!DatabaseExists(serverConn, databaseName))
            {
                // Step 3: Create the database
                CreateDatabase(serverConn, databaseName);
                AppLogger.LogInfo($"[DatabaseAutoCreator] Created database '{databaseName}'.");
            }

            serverConn.Close();

            // Step 4: Connect to the new database and run full schema setup
            string fullConnStr = DbConnectionSettingsStore.BuildConnectionString(profile);
            using var dbConn = new MySqlConnection(fullConnStr);
            ((DbConnection)(object)dbConn).Open();

            // Run migrations and create all tables
            MigrationRunner.ApplyPendingMigrations(dbConn);
            SchemaBootstrap.EnsureCoreDefaults(dbConn);
            SchemaGuard.EnsureDatabaseReady(fullConnStr, force: true);

            AppLogger.LogInfo($"[DatabaseAutoCreator] Database '{databaseName}' is ready with full schema.");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"[DatabaseAutoCreator] Could not auto-create database '{databaseName}'.", ex);
            return false;
        }
    }

    /// <summary>
    /// Attempts to ensure the database exists. If the database already exists,
    /// just validates the schema is up to date.
    /// </summary>
    public static bool TryEnsureReady(string connectionString, DatabaseConnectionProfile profile)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || profile == null)
            return false;

        try
        {
            // First try connecting directly - if it works, database exists
            using var conn = new MySqlConnection(connectionString);
            ((DbConnection)(object)conn).Open();
            conn.Close();
            return true;
        }
        catch (MySqlException ex) when (IsDatabaseNotFoundError(ex))
        {
            // Database doesn't exist - create it
            AppLogger.LogInfo($"[DatabaseAutoCreator] Database not found, attempting auto-creation...");
            return EnsureDatabaseExists(profile);
        }
        catch
        {
            // Other connection errors (server unreachable, auth failed, etc.)
            return false;
        }
    }

    private static bool DatabaseExists(MySqlConnection serverConn, string databaseName)
    {
        using var cmd = new MySqlCommand(
            "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @db LIMIT 1",
            serverConn);
        cmd.Parameters.AddWithValue("@db", (object)databaseName);
        object result = ((DbCommand)(object)cmd).ExecuteScalar();
        return result != null && result != DBNull.Value;
    }

    private static void CreateDatabase(MySqlConnection serverConn, string databaseName)
    {
        string safeName = databaseName.Replace("`", "``", StringComparison.Ordinal);
        using var cmd = new MySqlCommand(
            $"CREATE DATABASE IF NOT EXISTS `{safeName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci",
            serverConn);
        ((DbCommand)(object)cmd).ExecuteNonQuery();
    }

    private static string BuildServerOnlyConnectionString(DatabaseConnectionProfile profile)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = profile.Server,
            Port = profile.Port == 0 ? 3306u : profile.Port,
            UserID = profile.Username,
            Password = profile.Password,
            SslMode = profile.UseSsl ? MySqlSslMode.Preferred : MySqlSslMode.Disabled,
            AllowPublicKeyRetrieval = true,
            AllowUserVariables = true,
            ConnectionTimeout = 5
        };
        // Do NOT set Database - connect to server only
        return ((DbConnectionStringBuilder)(object)builder).ConnectionString;
    }

    private static bool IsDatabaseNotFoundError(MySqlException ex)
    {
        // MySQL error 1049 = Unknown database
        if (ex.Number == 1049)
            return true;

        // Also check the message for safety
        string msg = ex.Message ?? string.Empty;
        return msg.Contains("Unknown database", StringComparison.OrdinalIgnoreCase);
    }
}
