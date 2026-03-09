using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal enum BackupRunState
{
    Unknown = 0,
    Running = 1,
    Success = 2,
    Failed = 3
}

internal enum BackupMode
{
    Full = 0,
    Incremental = 1,
    Differential = 2
}

internal sealed record BackupRunInfo(
    long? BackupRunId,
    DateTime StartedAt,
    DateTime? EndedAt,
    BackupRunState State,
    string? FilePath,
    long? FileSizeBytes,
    string? ErrorMessage,
    BackupMode Mode = BackupMode.Full,
    DateTime? BaselineStartedAt = null);

internal sealed record BackupBaseline(
    long? BackupRunId,
    DateTime StartedAt);

internal static class BackupService
{
    private const string EnvMySqlDumpPath = "BARANGAY_MYSQLDUMP_PATH";
    private static readonly string[] TemporalColumnCandidates =
    {
        "updated_at",
        "modified_at",
        "changed_at",
        "created_at",
        "requested_at",
        "released_at",
        "approved_at",
        "date_registered",
        "date_filed",
        "started_at",
        "transferred_at",
        "uploaded_at",
        "action_at",
        "applied_at"
    };

    internal static string GetBackupDirectory()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BarangaySystem",
            "backups");
        Directory.CreateDirectory(dir);
        return dir;
    }

    internal static void OpenBackupFolder()
    {
        string dir = GetBackupDirectory();
        Process.Start(new ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true
        });
    }

    internal static BackupRunInfo? TryGetLatestRun()
    {
        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                @"SELECT backup_run_id,
                         started_at,
                         ended_at,
                         status,
                         backup_type,
                         base_started_at,
                         file_path,
                         file_size_bytes,
                         error_message
                  FROM backup_run
                  ORDER BY started_at DESC
                  LIMIT 1", conn);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            long? id = reader["backup_run_id"] != DBNull.Value ? Convert.ToInt64(reader["backup_run_id"]) : null;
            DateTime startedAt = reader["started_at"] != DBNull.Value
                ? Convert.ToDateTime(reader["started_at"])
                : DateTime.MinValue;
            DateTime? endedAt = reader["ended_at"] != DBNull.Value ? Convert.ToDateTime(reader["ended_at"]) : null;
            string status = reader["status"]?.ToString() ?? string.Empty;
            string backupType = reader["backup_type"]?.ToString() ?? string.Empty;
            DateTime? baseStartedAt = reader["base_started_at"] != DBNull.Value ? Convert.ToDateTime(reader["base_started_at"]) : null;
            string? filePath = reader["file_path"]?.ToString();
            long? fileSize = reader["file_size_bytes"] != DBNull.Value ? Convert.ToInt64(reader["file_size_bytes"]) : null;
            string? error = reader["error_message"]?.ToString();

            return new BackupRunInfo(id, startedAt, endedAt, ParseState(status), filePath, fileSize, error, ParseMode(backupType), baseStartedAt);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to read latest backup status.", ex);
            return null;
        }
    }

    internal static BackupRunInfo MarkInterruptedRunAsFailed(BackupRunInfo info, string? reason = null)
    {
        if (info.State != BackupRunState.Running)
        {
            return info;
        }

        DateTime endedAt = DateTime.Now;
        string message = string.IsNullOrWhiteSpace(reason)
            ? "Backup was interrupted before completion."
            : reason.Trim();

        var failedInfo = new BackupRunInfo(
            info.BackupRunId,
            info.StartedAt,
            endedAt,
            BackupRunState.Failed,
            info.FilePath,
            info.FileSizeBytes,
            message,
            info.Mode,
            info.BaselineStartedAt);

        try
        {
            TryUpdateRun(failedInfo);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to mark interrupted backup as failed.", ex);
        }

        return failedInfo;
    }

    internal static DateTime? TryGetLastSuccessfulBackupAt()
    {
        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(
                "SELECT MAX(started_at) FROM backup_run WHERE status = 'SUCCESS'", conn);
            object? value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            return Convert.ToDateTime(value);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to read last successful backup timestamp.", ex);
            return null;
        }
    }

    internal static BackupRunInfo RunBackupNow(int? triggeredByUserId, bool compressToZip = true, BackupMode mode = BackupMode.Full)
    {
        DateTime startedAt = DateTime.Now;
        long? runId = null;
        BackupBaseline? baseline = ResolveBaseline(mode);
        BackupMode effectiveMode = mode;
        if (effectiveMode != BackupMode.Full && baseline == null)
        {
            // No prior backup exists yet; fallback to full backup for first run.
            effectiveMode = BackupMode.Full;
        }

        try
        {
            runId = TryInsertRun(startedAt, triggeredByUserId, effectiveMode, baseline);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to insert backup_run row.", ex);
        }

        try
        {
            string dumpExe = FindMySqlDumpExe()
                ?? throw new InvalidOperationException(
                    $"mysqldump.exe not found. Install MySQL client tools or set {EnvMySqlDumpPath} to the full path of mysqldump.exe.");

            var csb = GetResolvedConnectionStringBuilder();
            string dir = GetBackupDirectory();

            string modeLabel = effectiveMode.ToString().ToLowerInvariant();
            string baseName = $"{csb.Database}-{modeLabel}-backup-{startedAt:yyyyMMdd-HHmmss}";
            string sqlPath = UniquePath(Path.Combine(dir, baseName + ".sql"));

            if (effectiveMode == BackupMode.Full)
            {
                RunMySqlDump(dumpExe, csb, sqlPath);
            }
            else
            {
                RunChangedDataDump(dumpExe, csb, sqlPath, baseline!.StartedAt, effectiveMode);
            }

            string finalPath = sqlPath;
            if (compressToZip)
            {
                finalPath = TryZipSql(sqlPath) ?? sqlPath;
            }

            long fileSize = 0;
            try
            {
                fileSize = new FileInfo(finalPath).Length;
            }
            catch
            {
                // Ignore file size failures.
            }

            DateTime endedAt = DateTime.Now;
            var result = new BackupRunInfo(runId, startedAt, endedAt, BackupRunState.Success, finalPath, fileSize, null, effectiveMode, baseline?.StartedAt);

            TryUpdateRun(result);
            return result;
        }
        catch (Exception ex)
        {
            DateTime endedAt = DateTime.Now;
            string message = ex.Message;
            AppLogger.LogError("Backup failed.", ex);

            var result = new BackupRunInfo(runId, startedAt, endedAt, BackupRunState.Failed, null, null, message, effectiveMode, baseline?.StartedAt);
            TryUpdateRun(result);
            return result;
        }
    }

    internal static BackupRunInfo RunFullBackupNow(int? triggeredByUserId, bool compressToZip = true)
        => RunBackupNow(triggeredByUserId, compressToZip, BackupMode.Full);

    internal static BackupRunInfo RunIncrementalBackupNow(int? triggeredByUserId, bool compressToZip = true)
        => RunBackupNow(triggeredByUserId, compressToZip, BackupMode.Incremental);

    internal static BackupRunInfo RunDifferentialBackupNow(int? triggeredByUserId, bool compressToZip = true)
        => RunBackupNow(triggeredByUserId, compressToZip, BackupMode.Differential);

    private static BackupRunState ParseState(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BackupRunState.Unknown;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "RUNNING" => BackupRunState.Running,
            "SUCCESS" => BackupRunState.Success,
            "FAILED" => BackupRunState.Failed,
            _ => BackupRunState.Unknown
        };
    }

    private static string StateToDb(BackupRunState state)
    {
        return state switch
        {
            BackupRunState.Running => "RUNNING",
            BackupRunState.Success => "SUCCESS",
            BackupRunState.Failed => "FAILED",
            _ => "FAILED"
        };
    }

    private static BackupMode ParseMode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BackupMode.Full;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "INCREMENTAL" => BackupMode.Incremental,
            "DIFFERENTIAL" => BackupMode.Differential,
            _ => BackupMode.Full
        };
    }

    private static string ModeToDb(BackupMode mode)
    {
        return mode switch
        {
            BackupMode.Incremental => "INCREMENTAL",
            BackupMode.Differential => "DIFFERENTIAL",
            _ => "FULL"
        };
    }

    private static MySqlConnectionStringBuilder GetResolvedConnectionStringBuilder()
    {
        using var conn = DBConnection.GetConnection();
        return new MySqlConnectionStringBuilder(conn.ConnectionString);
    }

    private static long? TryInsertRun(DateTime startedAt, int? createdByUserId, BackupMode mode, BackupBaseline? baseline)
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();

        try
        {
            using var cmd = new MySqlCommand(
                @"INSERT INTO backup_run
                    (started_at, status, backup_type, base_started_at, base_backup_run_id, created_by_user_id)
                  VALUES
                    (@started_at, 'RUNNING', @backup_type, @base_started_at, @base_backup_run_id, @user_id)", conn);
            cmd.Parameters.AddWithValue("@started_at", startedAt);
            cmd.Parameters.AddWithValue("@backup_type", ModeToDb(mode));
            cmd.Parameters.AddWithValue("@base_started_at", baseline?.StartedAt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@base_backup_run_id", baseline?.BackupRunId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@user_id", createdByUserId.HasValue ? createdByUserId.Value : DBNull.Value);
            cmd.ExecuteNonQuery();
            return cmd.LastInsertedId > 0 ? cmd.LastInsertedId : null;
        }
        catch
        {
            // Backward-compatible fallback for older schemas.
            using var fallback = new MySqlCommand(
                @"INSERT INTO backup_run (started_at, status, created_by_user_id)
                  VALUES (@started_at, 'RUNNING', @user_id)", conn);
            fallback.Parameters.AddWithValue("@started_at", startedAt);
            fallback.Parameters.AddWithValue("@user_id", createdByUserId.HasValue ? createdByUserId.Value : DBNull.Value);
            fallback.ExecuteNonQuery();
            return fallback.LastInsertedId > 0 ? fallback.LastInsertedId : null;
        }
    }

    private static BackupBaseline? ResolveBaseline(BackupMode mode)
    {
        if (mode == BackupMode.Full)
        {
            return null;
        }

        return mode == BackupMode.Differential
            ? TryGetLatestSuccessfulBaseline(onlyFull: true)
            : TryGetLatestSuccessfulBaseline(onlyFull: false);
    }

    private static BackupBaseline? TryGetLatestSuccessfulBaseline(bool onlyFull)
    {
        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();

            string sql = onlyFull
                ? @"SELECT backup_run_id, started_at
                    FROM backup_run
                    WHERE status = 'SUCCESS' AND backup_type = 'FULL'
                    ORDER BY started_at DESC
                    LIMIT 1"
                : @"SELECT backup_run_id, started_at
                    FROM backup_run
                    WHERE status = 'SUCCESS'
                    ORDER BY started_at DESC
                    LIMIT 1";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            long? id = reader["backup_run_id"] != DBNull.Value ? Convert.ToInt64(reader["backup_run_id"]) : null;
            DateTime startedAt = Convert.ToDateTime(reader["started_at"]);
            return new BackupBaseline(id, startedAt);
        }
        catch
        {
            return null;
        }
    }

    private static void TryUpdateRun(BackupRunInfo info)
    {
        if (!info.BackupRunId.HasValue)
        {
            return;
        }

        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                @"UPDATE backup_run
                     SET ended_at = @ended_at,
                         status = @status,
                         file_path = @file_path,
                         file_size_bytes = @file_size_bytes,
                         error_message = @error_message
                   WHERE backup_run_id = @id", conn);
            cmd.Parameters.AddWithValue("@ended_at", info.EndedAt.HasValue ? info.EndedAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@status", StateToDb(info.State));
            cmd.Parameters.AddWithValue("@file_path", info.FilePath ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@file_size_bytes", info.FileSizeBytes.HasValue ? info.FileSizeBytes.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@error_message", info.ErrorMessage ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", info.BackupRunId.Value);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to update backup_run row.", ex);
        }
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        string dir = Path.GetDirectoryName(path) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);

        for (int i = 2; i < 5000; i++)
        {
            string candidate = Path.Combine(dir, $"{name}-{i}{ext}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(dir, $"{name}-{Guid.NewGuid():N}{ext}");
    }

    private static string? TryZipSql(string sqlPath)
    {
        try
        {
            if (!File.Exists(sqlPath))
            {
                return null;
            }

            string zipPath = UniquePath(Path.ChangeExtension(sqlPath, ".zip"));
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(sqlPath, Path.GetFileName(sqlPath), CompressionLevel.Optimal);
            }

            File.Delete(sqlPath);
            return zipPath;
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to compress backup; keeping .sql output.", ex);
            return null;
        }
    }

    private static void RunMySqlDump(string dumpExe, MySqlConnectionStringBuilder csb, string outputSqlPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputSqlPath) ?? ".");

        var baseArgs = BuildConnectionArgs(csb);
        baseArgs.AddRange(new[]
        {
            "--databases", csb.Database,
            "--routines",
            "--events",
            "--triggers",
            "--single-transaction",
            "--quick",
            $"--result-file={outputSqlPath}"
        });

        if (File.Exists(outputSqlPath))
        {
            File.Delete(outputSqlPath);
        }

        ExecuteDumpWithRetry(dumpExe, baseArgs, includeCompatFlags: true);

        var info = new FileInfo(outputSqlPath);
        if (!info.Exists || info.Length <= 0)
        {
            throw new InvalidOperationException("Backup output file was not created or is empty.");
        }
    }

    private static void RunChangedDataDump(string dumpExe, MySqlConnectionStringBuilder csb, string outputSqlPath, DateTime since, BackupMode mode)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputSqlPath) ?? ".");

        using var conn = DBConnection.GetConnection();
        conn.Open();

        var tables = new List<string>();
        using (var tablesCmd = new MySqlCommand(
                   @"SELECT table_name
                     FROM information_schema.tables
                     WHERE table_schema = DATABASE()
                       AND table_type = 'BASE TABLE'
                     ORDER BY table_name", conn))
        using (var reader = tablesCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string? table = reader["table_name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(table))
                {
                    tables.Add(table);
                }
            }
        }

        File.WriteAllText(outputSqlPath,
            $"-- {mode} backup generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
            $"-- Baseline: {since:yyyy-MM-dd HH:mm:ss}\n" +
            $"USE `{csb.Database}`;\n" +
            "SET FOREIGN_KEY_CHECKS=0;\n\n");

        foreach (string table in tables)
        {
            string? timeColumn = ResolveTemporalColumn(conn, table);
            string? whereClause = timeColumn == null
                ? null
                : $"`{timeColumn}` >= '{since:yyyy-MM-dd HH:mm:ss}'";

            string tempPath = Path.Combine(Path.GetTempPath(), $"bms-{Guid.NewGuid():N}.sql");
            try
            {
                var args = BuildConnectionArgs(csb);
                args.AddRange(new[]
                {
                    "--single-transaction",
                    "--quick",
                    "--no-create-info",
                    "--skip-triggers",
                    "--skip-lock-tables",
                    $"--result-file={tempPath}",
                    csb.Database,
                    table
                });

                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    args.Insert(args.Count - 2, $"--where={whereClause}");
                }

                ExecuteDumpWithRetry(dumpExe, args, includeCompatFlags: true);

                if (File.Exists(tempPath))
                {
                    string content = File.ReadAllText(tempPath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        File.AppendAllText(outputSqlPath, content + Environment.NewLine);
                    }
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Ignore temp cleanup failures.
                }
            }
        }

        File.AppendAllText(outputSqlPath, "\nSET FOREIGN_KEY_CHECKS=1;\n");
    }

    private static string? ResolveTemporalColumn(MySqlConnection conn, string tableName)
    {
        foreach (string candidate in TemporalColumnCandidates)
        {
            using var cmd = new MySqlCommand(
                @"SELECT COUNT(*)
                  FROM information_schema.columns
                  WHERE table_schema = DATABASE()
                    AND table_name = @table
                    AND column_name = @column", conn);
            cmd.Parameters.AddWithValue("@table", tableName);
            cmd.Parameters.AddWithValue("@column", candidate);

            if (Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    private static List<string> BuildConnectionArgs(MySqlConnectionStringBuilder csb)
    {
        var args = new List<string>
        {
            "--host", csb.Server,
            "--port", csb.Port.ToString(),
            "--user", csb.UserID
        };

        if (csb.Password != null)
        {
            // Avoid interactive password prompt, even when password is empty.
            args.Add($"--password={csb.Password}");
        }

        return args;
    }

    private static void ExecuteDumpWithRetry(string dumpExe, List<string> args, bool includeCompatFlags)
    {
        var tryArgs = includeCompatFlags
            ? new[]
            {
                args.Concat(new[] { "--set-gtid-purged=OFF", "--column-statistics=0" }).ToList(),
                args
            }
            : new[] { args };

        Exception? lastError = null;
        foreach (var runArgs in tryArgs)
        {
            try
            {
                (int exitCode, string stderr) = RunProcessCaptureStderr(dumpExe, runArgs);
                if (exitCode != 0)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(stderr)
                            ? $"mysqldump failed with exit code {exitCode}."
                            : $"mysqldump failed: {stderr.Trim()}");
                }

                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                string message = ex.Message.ToLowerInvariant();
                bool looksLikeUnknownOption =
                    message.Contains("unknown option") ||
                    message.Contains("unknown variable") ||
                    message.Contains("unrecognized option");

                if (!looksLikeUnknownOption)
                {
                    break;
                }
            }
        }

        throw lastError ?? new InvalidOperationException("mysqldump failed.");
    }

    private static (int ExitCode, string Stderr) RunProcessCaptureStderr(string exePath, List<string> args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false
        };

        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stderr);
    }

    private static string? FindMySqlDumpExe()
    {
        string? env = Environment.GetEnvironmentVariable(EnvMySqlDumpPath);
        if (!string.IsNullOrWhiteSpace(env))
        {
            env = env.Trim().Trim('"');
            if (File.Exists(env))
            {
                return env;
            }
        }

        foreach (string candidate in EnumerateMySqlDumpCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateMySqlDumpCandidates()
    {
        foreach (string candidate in EnumerateFromPath())
        {
            yield return candidate;
        }

        foreach (string candidate in EnumerateCommonInstalls())
        {
            yield return candidate;
        }
    }

    private static IEnumerable<string> EnumerateFromPath()
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        foreach (string raw in path.Split(';'))
        {
            string part = (raw ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            string candidate = Path.Combine(part, "mysqldump.exe");
            yield return candidate;
        }
    }

    private static IEnumerable<string> EnumerateCommonInstalls()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        foreach (string baseDir in new[] { programFiles, programFilesX86 })
        {
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                continue;
            }

            string mysqlBase = Path.Combine(baseDir, "MySQL");
            if (Directory.Exists(mysqlBase))
            {
                foreach (string dir in SafeEnumerateDirectories(mysqlBase, "MySQL Server*"))
                {
                    yield return Path.Combine(dir, "bin", "mysqldump.exe");
                }
            }

            string mariaBase = Path.Combine(baseDir, "MariaDB");
            if (Directory.Exists(mariaBase))
            {
                foreach (string dir in SafeEnumerateDirectories(mariaBase, "MariaDB*"))
                {
                    yield return Path.Combine(dir, "bin", "mysqldump.exe");
                }
            }
        }

        // Popular bundled stacks.
        yield return @"C:\xampp\mysql\bin\mysqldump.exe";

        foreach (string candidate in SafeEnumerateFiles(@"C:\wamp64\bin\mysql", "mysqldump.exe"))
        {
            yield return candidate;
            yield break;
        }

        foreach (string candidate in SafeEnumerateFiles(@"C:\laragon\bin\mysql", "mysqldump.exe"))
        {
            yield return candidate;
            yield break;
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string baseDir, string pattern)
    {
        try
        {
            return Directory.EnumerateDirectories(baseDir, pattern, SearchOption.TopDirectoryOnly).ToArray();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string baseDir, string fileName)
    {
        try
        {
            if (!Directory.Exists(baseDir))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(baseDir, fileName, SearchOption.AllDirectories).ToArray();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
}
