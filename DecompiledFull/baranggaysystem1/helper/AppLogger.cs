using System;
using System.IO;

namespace baranggaysystem1.helper;

internal static class AppLogger
{
	private static readonly object SyncRoot = new object();

	private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarangaySystem", "logs");

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
		}
	}

	public static void LogError(string message, Exception? ex = null)
	{
		Log("ERROR", message, ex);
	}

	public static void LogWarning(string message, Exception? ex = null)
	{
		Log("WARN", message, ex);
	}

	public static void LogInfo(string message)
	{
		Log("INFO", message, null);
	}

	internal static string GetLogDirectoryPath()
	{
		lock (SyncRoot)
		{
			EnsureDirectoriesAndRotate();
			return LogDirectory;
		}
	}

	internal static string GetErrorLogDirectoryPath()
	{
		lock (SyncRoot)
		{
			EnsureDirectoriesAndRotate();
			return ErrorLogDirectory;
		}
	}

	private static void Log(string level, string message, Exception? ex)
	{
		try
		{
			lock (SyncRoot)
			{
				EnsureDirectoriesAndRotate();
				string userTag = BuildUserTag();
				WriteEntry(Path.Combine(LogDirectory, $"app-{DateTime.Today:yyyyMMdd}.log"), level, message, userTag, ex);
				if (string.Equals(level, "ERROR", StringComparison.OrdinalIgnoreCase))
				{
					WriteEntry(Path.Combine(ErrorLogDirectory, $"error-{DateTime.Today:yyyyMMdd}.log"), level, message, userTag, ex);
				}
			}
		}
		catch
		{
		}
	}

	private static void WriteEntry(string path, string level, string message, string userTag, Exception? ex)
	{
		using StreamWriter streamWriter = new StreamWriter(path, append: true);
		streamWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
		if (!string.IsNullOrWhiteSpace(userTag))
		{
			streamWriter.WriteLine("  User: " + userTag);
		}
		if (ex != null)
		{
			streamWriter.WriteLine("  Exception: " + ex.GetType().Name + ": " + ex.Message);
			streamWriter.WriteLine(ex.StackTrace ?? string.Empty);
		}
	}

	private static void EnsureDirectoriesAndRotate()
	{
		Directory.CreateDirectory(LogDirectory);
		Directory.CreateDirectory(ErrorLogDirectory);
		DateTime today = DateTime.Today;
		if (!_isInitialized || !(_lastMaintenanceDate == today))
		{
			_retentionDays = ResolveRetentionDays();
			DateTime cutoff = today.AddDays(-_retentionDays);
			RotateOldFiles(LogDirectory, "app-*.log", cutoff);
			RotateOldFiles(ErrorLogDirectory, "error-*.log", cutoff);
			_lastMaintenanceDate = today;
		}
	}

	private static void RotateOldFiles(string directory, string pattern, DateTime cutoff)
	{
		try
		{
			foreach (string item in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
			{
				DateTime lastWriteTime;
				try
				{
					lastWriteTime = File.GetLastWriteTime(item);
				}
				catch
				{
					continue;
				}
				if (!(lastWriteTime.Date >= cutoff.Date))
				{
					try
					{
						File.Delete(item);
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	private static int ResolveRetentionDays()
	{
		if (int.TryParse(Environment.GetEnvironmentVariable("BARANGAY_LOG_RETENTION_DAYS"), out var result))
		{
			return Math.Clamp(result, 7, 365);
		}
		return 30;
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
