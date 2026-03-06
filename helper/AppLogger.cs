using System;
using System.IO;

namespace baranggaysystem1.helper
{
    internal static class AppLogger
    {
        private static readonly object SyncRoot = new object();
        private static readonly string LogDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarangaySystem", "logs");
        private static readonly string ErrorLogDirectory = Path.Combine(LogDirectory, "errors");
        private const int DefaultRetentionDays = 30;
        private static DateTime _lastMaintenanceDate = DateTime.MinValue;
        private static int _retentionDays = ResolveRetentionDays();
        private static bool _isInitialized;

        public static void Initialize()
        {
            try
            {
                lock (SyncRoot)
                {
                    EnsureDirectoriesAndRotate();
                    _isInitialized = true;
                }
            }
            catch
            {
                // Logging should never break app startup.
            }
        }

        public static void LogError(string message, Exception? ex = null)
            => Log("ERROR", message, ex);

        public static void LogWarning(string message, Exception? ex = null)
            => Log("WARN", message, ex);

        public static void LogInfo(string message)
            => Log("INFO", message, null);

        private static void Log(string level, string message, Exception? ex)
        {
            try
            {
                lock (SyncRoot)
                {
                    EnsureDirectoriesAndRotate();
                    string userTag = BuildUserTag();
                    string appLogPath = Path.Combine(LogDirectory, $"app-{DateTime.Today:yyyyMMdd}.log");
                    WriteEntry(appLogPath, level, message, userTag, ex);

                    if (string.Equals(level, "ERROR", StringComparison.OrdinalIgnoreCase))
                    {
                        string errorLogPath = Path.Combine(ErrorLogDirectory, $"error-{DateTime.Today:yyyyMMdd}.log");
                        WriteEntry(errorLogPath, level, message, userTag, ex);
                    }
                }
            }
            catch
            {
                // Logging should never break the app.
            }
        }

        private static void WriteEntry(string path, string level, string message, string userTag, Exception? ex)
        {
            using var writer = new StreamWriter(path, append: true);
            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
            if (!string.IsNullOrWhiteSpace(userTag))
            {
                writer.WriteLine($"  User: {userTag}");
            }
            if (ex != null)
            {
                writer.WriteLine($"  Exception: {ex.GetType().Name}: {ex.Message}");
                writer.WriteLine(ex.StackTrace ?? string.Empty);
            }
        }

        private static void EnsureDirectoriesAndRotate()
        {
            Directory.CreateDirectory(LogDirectory);
            Directory.CreateDirectory(ErrorLogDirectory);

            DateTime today = DateTime.Today;
            if (_isInitialized && _lastMaintenanceDate == today)
            {
                return;
            }

            _retentionDays = ResolveRetentionDays();
            DateTime cutoff = today.AddDays(-_retentionDays);
            RotateOldFiles(LogDirectory, "app-*.log", cutoff);
            RotateOldFiles(ErrorLogDirectory, "error-*.log", cutoff);
            _lastMaintenanceDate = today;
        }

        private static void RotateOldFiles(string directory, string pattern, DateTime cutoff)
        {
            try
            {
                foreach (string filePath in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
                {
                    DateTime stamp;
                    try
                    {
                        stamp = File.GetLastWriteTime(filePath);
                    }
                    catch
                    {
                        continue;
                    }

                    if (stamp.Date >= cutoff.Date)
                    {
                        continue;
                    }

                    try
                    {
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Ignore individual file deletion failures.
                    }
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }

        private static int ResolveRetentionDays()
        {
            string? value = Environment.GetEnvironmentVariable("BARANGAY_LOG_RETENTION_DAYS");
            if (int.TryParse(value, out int parsed))
            {
                return Math.Clamp(parsed, 7, 365);
            }

            return DefaultRetentionDays;
        }

        private static string BuildUserTag()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(UserSession.Username))
                {
                    return $"{UserSession.Username} (#{UserSession.UserId}, {UserSession.Role})";
                }
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }
    }
}
