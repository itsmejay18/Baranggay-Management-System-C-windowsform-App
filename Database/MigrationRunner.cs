using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Microsoft.Data.Sqlite;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class MigrationRunner
{
    private const string ManualTag = "@manual";

    public static void ApplyPendingMigrations(MySqlConnection conn)
    {
        EnsureMigrationsTable(conn);
        ApplyPendingMigrationsGeneric(conn);
    }

    public static void ApplyPendingMigrations(SqliteConnection conn)
    {
        EnsureMigrationsTable(conn);
        ApplyPendingMigrationsGeneric(conn);
    }

    private static void ApplyPendingMigrationsGeneric(DbConnection conn)
    {
        var files = GetOrderedMigrationFiles();
        if (files.Count == 0)
        {
            AppLogger.LogWarning("Migrations directory not found; skipping migration runner.");
            return;
        }

        var applied = LoadAppliedMigrations(conn);
        foreach (string filePath in files)
        {
            string name = Path.GetFileName(filePath);
            if (applied.Contains(name))
            {
                continue;
            }

            string sql = File.ReadAllText(filePath);
            if (IsManualMigration(sql))
            {
                continue;
            }

            ApplySqlScript(conn, name, sql);
            MarkApplied(conn, name);
            applied.Add(name);
        }
    }

    public static IReadOnlyList<string> GetPendingAutoMigrationNames(MySqlConnection conn)
    {
        return GetPendingMigrationNames(conn, includeManual: false, onlyManual: false);
    }

    public static IReadOnlyList<string> GetPendingManualMigrationNames(MySqlConnection conn)
    {
        return GetPendingMigrationNames(conn, includeManual: true, onlyManual: true);
    }

    public static string? TryGetCurrentSchemaVersion()
    {
        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();
            EnsureMigrationsTable(conn);

            using var cmd = new MySqlCommand(
                @"SELECT migration_name
                  FROM schema_migrations
                  ORDER BY applied_at DESC, migration_name DESC
                  LIMIT 1", conn);
            object? value = cmd.ExecuteScalar();
            string? name = value?.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return Path.GetFileNameWithoutExtension(name);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to read current schema version.", ex);
            return null;
        }
    }

    public static bool HasMigrationFiles()
    {
        return GetOrderedMigrationFiles().Count > 0;
    }

    private static void EnsureMigrationsTable(DbConnection conn)
    {
        using var cmd = conn.CreateCommand();
        if (conn is SqliteConnection)
        {
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS schema_migrations (
                migration_name TEXT NOT NULL PRIMARY KEY,
                applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );";
        }
        else
        {
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS schema_migrations (
                migration_name VARCHAR(255) NOT NULL PRIMARY KEY,
                applied_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );";
        }

        cmd.ExecuteNonQuery();
    }

    private static IReadOnlyList<string> GetPendingMigrationNames(MySqlConnection conn, bool includeManual, bool onlyManual)
    {
        EnsureMigrationsTable(conn);
        var files = GetOrderedMigrationFiles();
        if (files.Count == 0)
        {
            return Array.Empty<string>();
        }

        var applied = LoadAppliedMigrations(conn);
        var pending = new List<string>();
        foreach (string filePath in files)
        {
            string name = Path.GetFileName(filePath);
            if (applied.Contains(name))
            {
                continue;
            }

            string sql = File.ReadAllText(filePath);
            bool manual = IsManualMigration(sql);
            if (onlyManual)
            {
                if (!manual)
                {
                    continue;
                }
            }
            else if (!includeManual && manual)
            {
                continue;
            }

            pending.Add(name);
        }

        return pending;
    }

    private static (int DateKey, int Priority, string Name) GetSortKey(string filePath)
    {
        string name = Path.GetFileName(filePath);
        int dateKey = 0;
        if (name.Length >= 8 && int.TryParse(name.Substring(0, 8), out int parsed))
        {
            dateKey = parsed;
        }

        // Ensure baseline schema comes before patches and indexes for the same date prefix.
        string lower = name.ToLowerInvariant();
        int priority;
        if (lower.Contains("new_schema"))
        {
            priority = 0;
        }
        else if (lower.Contains("patch"))
        {
            priority = 1;
        }
        else if (lower.Contains("role_permission"))
        {
            priority = 2;
        }
        else if (lower.Contains("backup_run"))
        {
            priority = 3;
        }
        else if (lower.Contains("add_indexes"))
        {
            priority = 9;
        }
        else
        {
            priority = 5;
        }

        return (dateKey, priority, name);
    }

    private static HashSet<string> LoadAppliedMigrations(DbConnection conn)
    {
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT migration_name FROM schema_migrations";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string? name = reader[0]?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                applied.Add(name);
            }
        }
        return applied;
    }

    private static void MarkApplied(DbConnection conn, string migrationName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO schema_migrations (migration_name) VALUES (@name)";
        var param = cmd.CreateParameter();
        param.ParameterName = "@name";
        param.Value = migrationName;
        cmd.Parameters.Add(param);
        cmd.ExecuteNonQuery();
    }

    private static bool IsManualMigration(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return false;
        }

        // Keep it simple: if the script contains the manual tag anywhere, we don't auto-run it.
        return sql.IndexOf(ManualTag, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void ApplySqlScript(DbConnection conn, string migrationName, string sql)
    {
        var statements = SplitStatements(sql).ToList();
        if (statements.Count == 0)
        {
            return;
        }

        AppLogger.LogInfo($"Applying migration {migrationName} ({statements.Count} statements)...");

        for (int i = 0; i < statements.Count; i++)
        {
            string stmt = statements[i];
                if (conn is SqliteConnection)
            {
                stmt = ConvertToSqliteSql(stmt);
            }

            string stmtTrimmed = stmt.Trim();
            if (string.IsNullOrEmpty(stmtTrimmed)
                || stmtTrimmed.StartsWith("--", StringComparison.Ordinal)
                || stmtTrimmed.StartsWith("/*", StringComparison.Ordinal)
                || stmtTrimmed.Equals("-- skipped for sqlite", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = stmt;
            cmd.CommandTimeout = 60;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                string singleLine = stmt
                    .Replace("\r", " ", StringComparison.Ordinal)
                    .Replace("\n", " ", StringComparison.Ordinal)
                    .Trim();
                if (singleLine.Length > 240)
                {
                    singleLine = singleLine[..240] + "...";
                }

                AppLogger.LogError(
                    $"Migration {migrationName} failed at statement {i + 1}/{statements.Count}: {singleLine}",
                    ex);
                throw;
            }
        }
    }

    private static string ConvertToSqliteSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        string normalized = sql.Replace("`", "", StringComparison.Ordinal)
            .Replace("AUTO_INCREMENT", "AUTOINCREMENT", StringComparison.OrdinalIgnoreCase)
            .Replace("ENGINE=InnoDB", "", StringComparison.OrdinalIgnoreCase)
            .Replace("ENGINE=MyISAM", "", StringComparison.OrdinalIgnoreCase)
            .Replace("UNSIGNED", "", StringComparison.OrdinalIgnoreCase)
            .Replace("INT(11)", "INTEGER", StringComparison.OrdinalIgnoreCase)
            .Replace("DATETIME", "TEXT", StringComparison.OrdinalIgnoreCase)
            .Replace("NOW()", "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase)
            .Replace("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP", "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase)
            .Replace("TRUE", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("FALSE", "0", StringComparison.OrdinalIgnoreCase)
            .Replace("boolean", "INTEGER", StringComparison.OrdinalIgnoreCase)
            .Replace("tinyint(1)", "INTEGER", StringComparison.OrdinalIgnoreCase)
            .Replace("text CHARACTER SET utf8mb4", "TEXT", StringComparison.OrdinalIgnoreCase)
            .Replace("CHARACTER SET utf8mb4", "", StringComparison.OrdinalIgnoreCase);

        // DROP DATABASE/USE and MySQL session statements are no-ops for sqlite
        string trimmed = normalized.TrimStart();
        if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("SET FOREIGN_KEY_CHECKS", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("SET @", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("SET SESSION", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("PREPARE ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("EXECUTE ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("DEALLOCATE ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("CALL ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("SET NAMES", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("SET CHARACTER SET", StringComparison.OrdinalIgnoreCase))
        {
            return "-- skipped for sqlite";
        }

        // Convert MySQL ENUM to sqlite compatible type.
        normalized = Regex.Replace(normalized, "\\bENUM\\s*\\([^)]*\\)", "TEXT", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, "\\bLONGBLOB\\b", "BLOB", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, "\\bTINYINT\\s*\\(1\\)", "INTEGER", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, "\\bINT\\s+AUTOINCREMENT\\s+PRIMARY\\s+KEY\\b", "INTEGER PRIMARY KEY AUTOINCREMENT", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, "\\bINT\\s+AUTOINCREMENT\\b", "INTEGER", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, "AUTO_INCREMENT", "AUTOINCREMENT", RegexOptions.IgnoreCase);

        // Remove inline MySQL index definitions inside CREATE TABLE statements.
        normalized = Regex.Replace(normalized, "(?im)^\\s*INDEX\\s+[A-Za-z0-9_]+\\s*\\([^)]*\\)\\s*,?\\s*$", string.Empty);
        normalized = Regex.Replace(normalized, "(?m),\\s*\\)\\s*$", ")");

        // Convert MySQL insert upsert syntax to sqlite-friendly version by dropping duplicate-key update clauses.
        normalized = Regex.Replace(normalized, "ON DUPLICATE KEY UPDATE.*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // SQLite only supports autoincrement on an INTEGER PRIMARY KEY
        normalized = Regex.Replace(normalized, "INTEGER PRIMARY KEY AUTOINCREMENT", "INTEGER PRIMARY KEY AUTOINCREMENT", RegexOptions.IgnoreCase); // no-op

        return normalized;
    }

    private static IEnumerable<string> SplitStatements(string sql)
    {
        if (sql == null)
        {
            yield break;
        }

        // Strip BOM if present.
        if (sql.Length > 0 && sql[0] == '\uFEFF')
        {
            sql = sql[1..];
        }

        string delimiter = ";";
        var sb = new StringBuilder();
        using var reader = new StringReader(sql);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
            {
                string next = trimmed.Substring("DELIMITER".Length).Trim();
                delimiter = string.IsNullOrWhiteSpace(next) ? ";" : next;
                continue;
            }

            sb.AppendLine(line);

            while (true)
            {
                int idx = FindDelimiterIndex(sb, delimiter);
                if (idx < 0)
                {
                    break;
                }

                string statement = sb.ToString(0, idx);
                sb.Remove(0, idx + delimiter.Length);

                statement = statement.Trim();
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    yield return statement;
                }
            }
        }

        string tail = sb.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(tail))
        {
            yield return tail;
        }
    }

    private static int FindDelimiterIndex(StringBuilder sb, string delimiter)
    {
        if (sb.Length == 0)
        {
            return -1;
        }

        delimiter ??= ";";
        if (delimiter.Length == 0)
        {
            delimiter = ";";
        }

        bool inSingle = false;
        bool inDouble = false;
        bool inBacktick = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i <= sb.Length - delimiter.Length; i++)
        {
            char c = sb[i];
            char n = i + 1 < sb.Length ? sb[i + 1] : '\0';

            if (inLineComment)
            {
                if (c == '\n')
                {
                    inLineComment = false;
                }
                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && n == '/')
                {
                    inBlockComment = false;
                    i++;
                }
                continue;
            }

            if (inSingle)
            {
                if (c == '\\')
                {
                    i++;
                    continue;
                }
                if (c == '\'')
                {
                    inSingle = false;
                }
                continue;
            }

            if (inDouble)
            {
                if (c == '\\')
                {
                    i++;
                    continue;
                }
                if (c == '\"')
                {
                    inDouble = false;
                }
                continue;
            }

            if (inBacktick)
            {
                if (c == '`')
                {
                    inBacktick = false;
                }
                continue;
            }

            // Start comments.
            if (c == '#' )
            {
                inLineComment = true;
                continue;
            }

            if (c == '-' && n == '-')
            {
                char after = i + 2 < sb.Length ? sb[i + 2] : '\0';
                if (after == '\0' || char.IsWhiteSpace(after))
                {
                    inLineComment = true;
                    i++;
                    continue;
                }
            }

            if (c == '/' && n == '*')
            {
                inBlockComment = true;
                i++;
                continue;
            }

            // Start strings.
            if (c == '\'')
            {
                inSingle = true;
                continue;
            }

            if (c == '\"')
            {
                inDouble = true;
                continue;
            }

            if (c == '`')
            {
                inBacktick = true;
                continue;
            }

            bool matches = true;
            for (int k = 0; k < delimiter.Length; k++)
            {
                if (sb[i + k] != delimiter[k])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return i;
            }
        }

        return -1;
    }

    private static string? TryGetMigrationsDirectory()
    {
        // 1) Typical deployed path (copied to output).
        string candidate = Path.Combine(AppContext.BaseDirectory, "Database", "migrations");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        // 2) Dev path (project root).
        string? root = TryGetProjectRoot();
        if (!string.IsNullOrWhiteSpace(root))
        {
            candidate = Path.Combine(root, "Database", "migrations");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static List<string> GetOrderedMigrationFiles()
    {
        string? migrationsDir = TryGetMigrationsDirectory();
        if (string.IsNullOrWhiteSpace(migrationsDir) || !Directory.Exists(migrationsDir))
        {
            return new List<string>();
        }

        return Directory.EnumerateFiles(migrationsDir, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(GetSortKey)
            .ToList();
    }

    private static string? TryGetProjectRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && current != null; i++)
        {
            if (File.Exists(Path.Combine(current.FullName, "baranggaysystem1.csproj")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
