using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using baranggaysystem1.helper;

namespace baranggaysystem1.Database;

internal static class MigrationRunner
{
    private const string ManualTag = "@manual";

    public static void ApplyPendingMigrations(MySqlConnection conn)
    {
        EnsureMigrationsTable(conn);

        var files = GetOrderedMigrationFiles();
        if (files.Count == 0)
        {
            // Dev builds should copy migrations to output, but if not present, do not crash the app.
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

    private static void EnsureMigrationsTable(MySqlConnection conn)
    {
        using var cmd = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS schema_migrations (
                migration_name VARCHAR(255) NOT NULL PRIMARY KEY,
                applied_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )", conn);
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

    private static HashSet<string> LoadAppliedMigrations(MySqlConnection conn)
    {
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new MySqlCommand("SELECT migration_name FROM schema_migrations", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string? name = reader["migration_name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                applied.Add(name);
            }
        }
        return applied;
    }

    private static void MarkApplied(MySqlConnection conn, string migrationName)
    {
        using var cmd = new MySqlCommand(
            "INSERT INTO schema_migrations (migration_name) VALUES (@name)",
            conn);
        cmd.Parameters.AddWithValue("@name", migrationName);
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

    private static void ApplySqlScript(MySqlConnection conn, string migrationName, string sql)
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
            using var cmd = new MySqlCommand(stmt, conn);
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
