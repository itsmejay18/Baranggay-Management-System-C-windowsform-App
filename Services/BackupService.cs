using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Manages database backup operations.
/// </summary>
public static class BackupService
{
    private static readonly string BackupFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BarangayManagementSystem", "Backups");

    private static BackupRunInfo? _lastRun;

    public static BackupRunInfo? TryGetLatestRun()
    {
        return _lastRun;
    }

    public static DateTime? TryGetLastSuccessfulBackupAt()
    {
        return _lastRun?.State == BackupRunState.Success ? _lastRun.CompletedAt : null;
    }

    public static BackupRunInfo MarkInterruptedRunAsFailed(BackupRunInfo info, string reason)
    {
        var failed = new BackupRunInfo(
            info.FilePath,
            info.StartedAt,
            DateTime.Now,
            BackupRunState.Failed,
            info.SizeBytes,
            info.TriggeredByUserId,
            reason,
            info.Mode);
        _lastRun = failed;
        return failed;
    }

    public static BackupRunInfo RunBackupNow(int? triggeredByUserId, bool compressToZip = true, BackupMode mode = BackupMode.Full)
    {
        Directory.CreateDirectory(BackupFolder);

        DateTime startedAt = DateTime.Now;
        string timestamp = startedAt.ToString("yyyyMMdd_HHmmss");
        string modeLabel = mode.ToString().ToLowerInvariant();
        string fileName = $"backup_{modeLabel}_{timestamp}.sql";
        string filePath = Path.Combine(BackupFolder, fileName);

        try
        {
            // Get connection info for mysqldump
            var profile = DbConnectionSettingsStore.LoadOrDefault();

            string dumpArgs = $"--host={profile.Server} --port={profile.Port} --user={profile.Username} --password={profile.Password} {profile.Database}";

            if (mode == BackupMode.Incremental)
            {
                dumpArgs += " --flush-logs --master-data=2";
            }

            var psi = new ProcessStartInfo
            {
                FileName = "mysqldump",
                Arguments = dumpArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start mysqldump process.");
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"mysqldump failed: {error}");
            }

            File.WriteAllText(filePath, output);

            string finalPath = filePath;
            if (compressToZip)
            {
                string zipPath = Path.ChangeExtension(filePath, ".zip");
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(filePath, fileName, CompressionLevel.Optimal);
                }
                File.Delete(filePath);
                finalPath = zipPath;
            }

            long sizeBytes = new FileInfo(finalPath).Length;
            var result = new BackupRunInfo(finalPath, startedAt, DateTime.Now, BackupRunState.Success, sizeBytes, triggeredByUserId, null, mode);
            _lastRun = result;
            return result;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Backup failed.", ex);
            var result = new BackupRunInfo(null, startedAt, DateTime.Now, BackupRunState.Failed, null, triggeredByUserId, ex.Message, mode);
            _lastRun = result;
            return result;
        }
    }

    public static void OpenBackupFolder()
    {
        Directory.CreateDirectory(BackupFolder);
        Process.Start(new ProcessStartInfo
        {
            FileName = BackupFolder,
            UseShellExecute = true
        });
    }
}

public enum BackupMode
{
    Full,
    Incremental,
    Differential
}

public enum BackupRunState
{
    Running,
    Success,
    Failed
}

public sealed class BackupRunInfo
{
    public string? FilePath { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
    public DateTime? EndedAt => CompletedAt;
    public BackupRunState State { get; }
    public long? SizeBytes { get; }
    public int? TriggeredByUserId { get; }
    public string? ErrorMessage { get; }
    public BackupMode Mode { get; }

    public BackupRunInfo(
        string? filePath,
        DateTime startedAt,
        DateTime? completedAt,
        BackupRunState state,
        long? sizeBytes,
        int? triggeredByUserId,
        string? errorMessage,
        BackupMode mode)
    {
        FilePath = filePath;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        State = state;
        SizeBytes = sizeBytes;
        TriggeredByUserId = triggeredByUserId;
        ErrorMessage = errorMessage;
        Mode = mode;
    }
}
