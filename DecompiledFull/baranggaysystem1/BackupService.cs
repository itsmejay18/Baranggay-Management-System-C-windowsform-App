using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class BackupService
{
	private sealed class MySqlDumpDefaultsFile : IDisposable
	{
		public string Path { get; }

		private MySqlDumpDefaultsFile(string path)
		{
			Path = path;
		}

		public static MySqlDumpDefaultsFile? Create(MySqlConnectionStringBuilder csb)
		{
			if (string.IsNullOrEmpty(((MySqlBaseConnectionStringBuilder)csb).Password))
			{
				return null;
			}
			string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bms-mysqldump-{Guid.NewGuid():N}.cnf");
			string contents = "[client]" + Environment.NewLine + "user=" + QuoteValue(((MySqlBaseConnectionStringBuilder)csb).UserID) + Environment.NewLine + "password=" + QuoteValue(((MySqlBaseConnectionStringBuilder)csb).Password) + Environment.NewLine + "host=" + QuoteValue(((MySqlBaseConnectionStringBuilder)csb).Server) + Environment.NewLine + "port=" + ((MySqlBaseConnectionStringBuilder)csb).Port;
			File.WriteAllText(path, contents);
			return new MySqlDumpDefaultsFile(path);
		}

		public void Dispose()
		{
			try
			{
				if (File.Exists(Path))
				{
					File.Delete(Path);
				}
			}
			catch
			{
			}
		}

		private static string QuoteValue(string value)
		{
			string text = (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
			return "\"" + text + "\"";
		}
	}

	private sealed class BackupRunSnapshot
	{
		public long? BackupRunId { get; set; }

		public DateTime StartedAt { get; set; }

		public DateTime? EndedAt { get; set; }

		public string State { get; set; } = "FAILED";

		public string? FilePath { get; set; }

		public long? FileSizeBytes { get; set; }

		public string? ErrorMessage { get; set; }

		public string Mode { get; set; } = "FULL";

		public DateTime? BaselineStartedAt { get; set; }

		public string? TargetDescription { get; set; }
	}

	private const string EnvMySqlDumpPath = "BARANGAY_MYSQLDUMP_PATH";

	private const string EnvMySqlExePath = "BARANGAY_MYSQL_PATH";

	private const string LatestRunManifestFileName = "latest-backup-run.json";

	private static readonly string[] TemporalColumnCandidates = new string[14]
	{
		"updated_at", "modified_at", "changed_at", "created_at", "requested_at", "released_at", "approved_at", "date_registered", "date_filed", "started_at",
		"transferred_at", "uploaded_at", "action_at", "applied_at"
	};

	internal static string GetBackupDirectory()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarangaySystem", "backups");
		Directory.CreateDirectory(text);
		return text;
	}

	internal static void OpenBackupFolder()
	{
		string backupDirectory = GetBackupDirectory();
		Process.Start(new ProcessStartInfo
		{
			FileName = backupDirectory,
			UseShellExecute = true
		});
	}

	internal static string GetCurrentBackupSourceSummary()
	{
		return "Current source: " + GetCurrentTargetDescription();
	}

	internal static string RestoreBackupFile(string backupPath)
	{
		if (OfflineDatabaseSupport.IsOffline)
		{
			throw new InvalidOperationException("SQL and ZIP restore is only available while the app is connected to MySQL. The app is currently using the local offline SQLite database.");
		}
		if (string.IsNullOrWhiteSpace(backupPath))
		{
			throw new InvalidOperationException("Select a backup file to restore first.");
		}
		string fullPath = Path.GetFullPath(backupPath.Trim());
		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException("Selected backup file could not be found.", fullPath);
		}
		string mysqlExe = FindMySqlExe() ?? throw new InvalidOperationException("mysql.exe not found. Install MySQL client tools or set BARANGAY_MYSQL_PATH to the full path of mysql.exe.");
		string tempSqlPath = null;
		try
		{
			string inputSqlPath = PrepareRestoreSql(fullPath, out tempSqlPath);
			MySqlConnectionStringBuilder resolvedConnectionStringBuilder = GetResolvedConnectionStringBuilder();
			RunMySqlRestore(mysqlExe, resolvedConnectionStringBuilder, inputSqlPath);
			return $"{Path.GetFileName(fullPath)} was restored to {((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Database} on {((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Server}:{((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Port}.";
		}
		finally
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(tempSqlPath) && File.Exists(tempSqlPath))
				{
					File.Delete(tempSqlPath);
				}
			}
			catch
			{
			}
		}
	}

	internal static BackupRunInfo? TryGetLatestRun()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		if (ShouldSkipRemoteBackupMetadata())
		{
			return TryLoadLatestLocalRun();
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand val = new MySqlCommand("SELECT backup_run_id,\n                         started_at,\n                         ended_at,\n                         status,\n                         backup_type,\n                         base_started_at,\n                         file_path,\n                         file_size_bytes,\n                         error_message\n                  FROM backup_run\n                  ORDER BY started_at DESC\n                  LIMIT 1", connection);
				try
				{
					MySqlDataReader val2 = val.ExecuteReader();
					try
					{
						if (!((DbDataReader)(object)val2).Read())
						{
							return null;
						}
						long? backupRunId = ((((DbDataReader)(object)val2)["backup_run_id"] != DBNull.Value) ? new long?(Convert.ToInt64(((DbDataReader)(object)val2)["backup_run_id"])) : ((long?)null));
						DateTime startedAt = ((((DbDataReader)(object)val2)["started_at"] != DBNull.Value) ? Convert.ToDateTime(((DbDataReader)(object)val2)["started_at"]) : DateTime.MinValue);
						DateTime? endedAt = ((((DbDataReader)(object)val2)["ended_at"] != DBNull.Value) ? new DateTime?(Convert.ToDateTime(((DbDataReader)(object)val2)["ended_at"])) : ((DateTime?)null));
						string value = ((DbDataReader)(object)val2)["status"]?.ToString() ?? string.Empty;
						string value2 = ((DbDataReader)(object)val2)["backup_type"]?.ToString() ?? string.Empty;
						return TryEnrichWithLatestLocalRun(new BackupRunInfo(BaselineStartedAt: (((DbDataReader)(object)val2)["base_started_at"] != DBNull.Value) ? new DateTime?(Convert.ToDateTime(((DbDataReader)(object)val2)["base_started_at"])) : ((DateTime?)null), FilePath: ((DbDataReader)(object)val2)["file_path"]?.ToString(), FileSizeBytes: (((DbDataReader)(object)val2)["file_size_bytes"] != DBNull.Value) ? new long?(Convert.ToInt64(((DbDataReader)(object)val2)["file_size_bytes"])) : ((long?)null), ErrorMessage: ((DbDataReader)(object)val2)["error_message"]?.ToString(), BackupRunId: backupRunId, StartedAt: startedAt, EndedAt: endedAt, State: ParseState(value), Mode: ParseMode(value2)));
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			DBConnection.RegisterConnectivityFailure(ex);
			AppLogger.LogWarning("Failed to read latest backup status.", ex);
			return TryLoadLatestLocalRun();
		}
	}

	internal static BackupRunInfo MarkInterruptedRunAsFailed(BackupRunInfo info, string? reason = null)
	{
		if (info.State != BackupRunState.Running)
		{
			return info;
		}
		DateTime now = DateTime.Now;
		string errorMessage = (string.IsNullOrWhiteSpace(reason) ? "Backup was interrupted before completion." : reason.Trim());
		BackupRunInfo backupRunInfo = new BackupRunInfo(info.BackupRunId, info.StartedAt, now, BackupRunState.Failed, info.FilePath, info.FileSizeBytes, errorMessage, info.Mode, info.BaselineStartedAt);
		try
		{
			TryUpdateRun(backupRunInfo);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Failed to mark interrupted backup as failed.", ex);
		}
		SaveLatestLocalRun(backupRunInfo);
		return backupRunInfo;
	}

	internal static DateTime? TryGetLastSuccessfulBackupAt()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		if (ShouldSkipRemoteBackupMetadata())
		{
			return null;
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				DBConnection.RegisterConnectivitySuccess();
				MySqlCommand val = new MySqlCommand("SELECT MAX(started_at) FROM backup_run WHERE status = 'SUCCESS'", connection);
				try
				{
					object obj = ((DbCommand)(object)val).ExecuteScalar();
					if (obj == null || obj == DBNull.Value)
					{
						return null;
					}
					return Convert.ToDateTime(obj);
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			DBConnection.RegisterConnectivityFailure(ex);
			AppLogger.LogWarning("Failed to read last successful backup timestamp.", ex);
			return null;
		}
	}

	private static bool ShouldSkipRemoteBackupMetadata()
	{
		if (!OfflineDatabaseSupport.IsOffline)
		{
			return DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false);
		}
		return true;
	}

	internal static BackupRunInfo RunBackupNow(int? triggeredByUserId, bool compressToZip = true, BackupMode mode = BackupMode.Full)
	{
		if (OfflineDatabaseSupport.IsOffline)
		{
			return RunOfflineDatabaseBackupNow(compressToZip, mode);
		}
		DateTime now = DateTime.Now;
		long? backupRunId = null;
		BackupBaseline backupBaseline = ResolveBaseline(mode);
		BackupMode backupMode = mode;
		if (backupMode != BackupMode.Full && backupBaseline == null)
		{
			backupMode = BackupMode.Full;
		}
		try
		{
			backupRunId = TryInsertRun(now, triggeredByUserId, backupMode, backupBaseline);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Failed to insert backup_run row.", ex);
		}
		try
		{
			string dumpExe = FindMySqlDumpExe() ?? throw new InvalidOperationException("mysqldump.exe not found. Install MySQL client tools or set BARANGAY_MYSQLDUMP_PATH to the full path of mysqldump.exe.");
			MySqlConnectionStringBuilder resolvedConnectionStringBuilder = GetResolvedConnectionStringBuilder();
			string targetDescription = $"MySQL {((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Database} on {((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Server}:{((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Port}";
			string backupDirectory = GetBackupDirectory();
			string value = backupMode.ToString().ToLowerInvariant();
			string text = $"{((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Database}-{value}-backup-{now:yyyyMMdd-HHmmss}";
			string text2 = UniquePath(Path.Combine(backupDirectory, text + ".sql"));
			if (backupMode == BackupMode.Full)
			{
				RunMySqlDump(dumpExe, resolvedConnectionStringBuilder, text2);
			}
			else
			{
				RunChangedDataDump(dumpExe, resolvedConnectionStringBuilder, text2, backupBaseline.StartedAt, backupMode);
			}
			string text3 = text2;
			if (compressToZip)
			{
				text3 = TryZipSql(text2) ?? text2;
			}
			long value2 = 0L;
			try
			{
				value2 = new FileInfo(text3).Length;
			}
			catch
			{
			}
			DateTime now2 = DateTime.Now;
			BackupRunInfo backupRunInfo = new BackupRunInfo(backupRunId, now, now2, BackupRunState.Success, text3, value2, null, backupMode, backupBaseline?.StartedAt, targetDescription);
			TryUpdateRun(backupRunInfo);
			SaveLatestLocalRun(backupRunInfo);
			return backupRunInfo;
		}
		catch (Exception ex2)
		{
			DateTime now3 = DateTime.Now;
			string message = ex2.Message;
			AppLogger.LogError("Backup failed.", ex2);
			BackupRunInfo backupRunInfo2 = new BackupRunInfo(backupRunId, now, now3, BackupRunState.Failed, null, null, message, backupMode, backupBaseline?.StartedAt, GetCurrentTargetDescription());
			TryUpdateRun(backupRunInfo2);
			SaveLatestLocalRun(backupRunInfo2);
			return backupRunInfo2;
		}
	}

	internal static BackupRunInfo RunFullBackupNow(int? triggeredByUserId, bool compressToZip = true)
	{
		return RunBackupNow(triggeredByUserId, compressToZip);
	}

	internal static BackupRunInfo RunIncrementalBackupNow(int? triggeredByUserId, bool compressToZip = true)
	{
		return RunBackupNow(triggeredByUserId, compressToZip, BackupMode.Incremental);
	}

	internal static BackupRunInfo RunDifferentialBackupNow(int? triggeredByUserId, bool compressToZip = true)
	{
		return RunBackupNow(triggeredByUserId, compressToZip, BackupMode.Differential);
	}

	private static BackupRunInfo RunOfflineDatabaseBackupNow(bool compressToZip, BackupMode requestedMode)
	{
		DateTime now = DateTime.Now;
		string currentTargetDescription = GetCurrentTargetDescription();
		string errorMessage = ((requestedMode == BackupMode.Full) ? null : "Offline mode only supports full-file SQLite backups. A full backup was created instead.");
		try
		{
			if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
			{
				throw new InvalidOperationException("The local offline SQLite database is not ready yet.");
			}
			string text = UniquePath(Path.Combine(GetBackupDirectory(), $"offline-full-backup-{now:yyyyMMdd-HHmmss}.sqlite"));
			CreateOfflineDatabaseCopy(text);
			long length = new FileInfo(text).Length;
			DateTime now2 = DateTime.Now;
			BackupRunInfo backupRunInfo = new BackupRunInfo(null, now, now2, BackupRunState.Success, text, length, errorMessage, BackupMode.Full, null, currentTargetDescription);
			SaveLatestLocalRun(backupRunInfo);
			return backupRunInfo;
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Offline SQLite backup failed.", ex);
			BackupRunInfo backupRunInfo2 = new BackupRunInfo(null, now, DateTime.Now, BackupRunState.Failed, null, null, ex.Message, BackupMode.Full, null, currentTargetDescription);
			SaveLatestLocalRun(backupRunInfo2);
			return backupRunInfo2;
		}
	}

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
			_ => BackupRunState.Unknown, 
		};
	}

	private static string StateToDb(BackupRunState state)
	{
		return state switch
		{
			BackupRunState.Running => "RUNNING", 
			BackupRunState.Success => "SUCCESS", 
			BackupRunState.Failed => "FAILED", 
			_ => "FAILED", 
		};
	}

	private static BackupMode ParseMode(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return BackupMode.Full;
		}
		string text = value.Trim().ToUpperInvariant();
		if (!(text == "INCREMENTAL"))
		{
			if (text == "DIFFERENTIAL")
			{
				return BackupMode.Differential;
			}
			return BackupMode.Full;
		}
		return BackupMode.Incremental;
	}

	private static string ModeToDb(BackupMode mode)
	{
		return mode switch
		{
			BackupMode.Incremental => "INCREMENTAL", 
			BackupMode.Differential => "DIFFERENTIAL", 
			_ => "FULL", 
		};
	}

	private static MySqlConnectionStringBuilder GetResolvedConnectionStringBuilder()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			return new MySqlConnectionStringBuilder(((DbConnection)(object)connection).ConnectionString, false);
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static string GetCurrentTargetDescription()
	{
		if (OfflineDatabaseSupport.IsOffline)
		{
			return "SQLite " + Path.GetFileName(OfflineDatabaseSupport.GetDatabasePath());
		}
		try
		{
			MySqlConnectionStringBuilder resolvedConnectionStringBuilder = GetResolvedConnectionStringBuilder();
			return $"MySQL {((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Database} on {((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Server}:{((MySqlBaseConnectionStringBuilder)resolvedConnectionStringBuilder).Port}";
		}
		catch
		{
			return "active MySQL database";
		}
	}

	private static long? TryInsertRun(DateTime startedAt, int? createdByUserId, BackupMode mode, BackupBaseline? baseline)
	{
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		MySqlConnection connection = DBConnection.GetConnection();
		long? num;
		try
		{
			((DbConnection)(object)connection).Open();
			try
			{
				MySqlCommand val = new MySqlCommand("INSERT INTO backup_run\n                    (started_at, status, backup_type, base_started_at, base_backup_run_id, created_by_user_id)\n                  VALUES\n                    (@started_at, 'RUNNING', @backup_type, @base_started_at, @base_backup_run_id, @user_id)", connection);
				try
				{
					val.Parameters.AddWithValue("@started_at", (object)startedAt);
					val.Parameters.AddWithValue("@backup_type", (object)ModeToDb(mode));
					MySqlParameterCollection parameters = val.Parameters;
					DateTime? dateTime = baseline?.StartedAt;
					parameters.AddWithValue("@base_started_at", dateTime.HasValue ? ((object)dateTime.GetValueOrDefault()) : DBNull.Value);
					MySqlParameterCollection parameters2 = val.Parameters;
					num = baseline?.BackupRunId;
					parameters2.AddWithValue("@base_backup_run_id", num.HasValue ? ((object)num.GetValueOrDefault()) : DBNull.Value);
					val.Parameters.AddWithValue("@user_id", createdByUserId.HasValue ? ((object)createdByUserId.Value) : DBNull.Value);
					((DbCommand)(object)val).ExecuteNonQuery();
					long? num2;
					if (val.LastInsertedId <= 0)
					{
						num = null;
						num2 = num;
					}
					else
					{
						num2 = val.LastInsertedId;
					}
					num = num2;
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			catch
			{
				MySqlCommand val2 = new MySqlCommand("INSERT INTO backup_run (started_at, status, created_by_user_id)\n                  VALUES (@started_at, 'RUNNING', @user_id)", connection);
				try
				{
					val2.Parameters.AddWithValue("@started_at", (object)startedAt);
					val2.Parameters.AddWithValue("@user_id", createdByUserId.HasValue ? ((object)createdByUserId.Value) : DBNull.Value);
					((DbCommand)(object)val2).ExecuteNonQuery();
					num = ((val2.LastInsertedId > 0) ? new long?(val2.LastInsertedId) : ((long?)null));
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
		return num;
	}

	private static BackupBaseline? ResolveBaseline(BackupMode mode)
	{
		return mode switch
		{
			BackupMode.Full => null, 
			BackupMode.Differential => TryGetLatestSuccessfulBaseline(onlyFull: true), 
			_ => TryGetLatestSuccessfulBaseline(onlyFull: false), 
		};
	}

	private static BackupBaseline? TryGetLatestSuccessfulBaseline(bool onlyFull)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				MySqlCommand val = new MySqlCommand(onlyFull ? "SELECT backup_run_id, started_at\n                    FROM backup_run\n                    WHERE status = 'SUCCESS' AND backup_type = 'FULL'\n                    ORDER BY started_at DESC\n                    LIMIT 1" : "SELECT backup_run_id, started_at\n                    FROM backup_run\n                    WHERE status = 'SUCCESS'\n                    ORDER BY started_at DESC\n                    LIMIT 1", connection);
				try
				{
					MySqlDataReader val2 = val.ExecuteReader();
					try
					{
						if (!((DbDataReader)(object)val2).Read())
						{
							return null;
						}
						long? backupRunId = ((((DbDataReader)(object)val2)["backup_run_id"] != DBNull.Value) ? new long?(Convert.ToInt64(((DbDataReader)(object)val2)["backup_run_id"])) : ((long?)null));
						DateTime startedAt = Convert.ToDateTime(((DbDataReader)(object)val2)["started_at"]);
						return new BackupBaseline(backupRunId, startedAt);
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch
		{
			return null;
		}
	}

	private static void TryUpdateRun(BackupRunInfo info)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		if (!info.BackupRunId.HasValue)
		{
			return;
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				MySqlCommand val = new MySqlCommand("UPDATE backup_run\n                     SET ended_at = @ended_at,\n                         status = @status,\n                         file_path = @file_path,\n                         file_size_bytes = @file_size_bytes,\n                         error_message = @error_message\n                   WHERE backup_run_id = @id", connection);
				try
				{
					val.Parameters.AddWithValue("@ended_at", info.EndedAt.HasValue ? ((object)info.EndedAt.Value) : DBNull.Value);
					val.Parameters.AddWithValue("@status", (object)StateToDb(info.State));
					val.Parameters.AddWithValue("@file_path", ((object)info.FilePath) ?? ((object)DBNull.Value));
					val.Parameters.AddWithValue("@file_size_bytes", info.FileSizeBytes.HasValue ? ((object)info.FileSizeBytes.Value) : DBNull.Value);
					val.Parameters.AddWithValue("@error_message", ((object)info.ErrorMessage) ?? ((object)DBNull.Value));
					val.Parameters.AddWithValue("@id", (object)info.BackupRunId.Value);
					((DbCommand)(object)val).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Failed to update backup_run row.", ex);
		}
	}

	private static void CreateOfflineDatabaseCopy(string destinationPath)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		if (string.IsNullOrWhiteSpace(destinationPath))
		{
			throw new InvalidOperationException("Offline backup destination path is required.");
		}
		Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? ".");
		if (File.Exists(destinationPath))
		{
			File.Delete(destinationPath);
		}
		SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
		try
		{
			SqliteCommand val = connection.CreateCommand();
			try
			{
				((DbCommand)(object)val).CommandText = "PRAGMA wal_checkpoint(FULL);";
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			SqliteConnection val2 = new SqliteConnection(((object)new SqliteConnectionStringBuilder
			{
				DataSource = destinationPath,
				Mode = (SqliteOpenMode)0
			}).ToString());
			try
			{
				((DbConnection)(object)val2).Open();
				connection.BackupDatabase(val2);
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static string GetLatestRunManifestPath()
	{
		return Path.Combine(GetBackupDirectory(), "latest-backup-run.json");
	}

	private static void SaveLatestLocalRun(BackupRunInfo info)
	{
		try
		{
			string contents = JsonSerializer.Serialize(new BackupRunSnapshot
			{
				BackupRunId = info.BackupRunId,
				StartedAt = info.StartedAt,
				EndedAt = info.EndedAt,
				State = StateToDb(info.State),
				FilePath = info.FilePath,
				FileSizeBytes = info.FileSizeBytes,
				ErrorMessage = info.ErrorMessage,
				Mode = ModeToDb(info.Mode),
				BaselineStartedAt = info.BaselineStartedAt,
				TargetDescription = info.TargetDescription
			}, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(GetLatestRunManifestPath(), contents);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Failed to save local backup manifest.", ex);
		}
	}

	private static BackupRunInfo? TryLoadLatestLocalRun()
	{
		try
		{
			string latestRunManifestPath = GetLatestRunManifestPath();
			if (!File.Exists(latestRunManifestPath))
			{
				return null;
			}
			BackupRunSnapshot backupRunSnapshot = JsonSerializer.Deserialize<BackupRunSnapshot>(File.ReadAllText(latestRunManifestPath));
			if (backupRunSnapshot == null)
			{
				return null;
			}
			return new BackupRunInfo(backupRunSnapshot.BackupRunId, backupRunSnapshot.StartedAt, backupRunSnapshot.EndedAt, ParseState(backupRunSnapshot.State), backupRunSnapshot.FilePath, backupRunSnapshot.FileSizeBytes, backupRunSnapshot.ErrorMessage, ParseMode(backupRunSnapshot.Mode), backupRunSnapshot.BaselineStartedAt, backupRunSnapshot.TargetDescription);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Failed to load local backup manifest.", ex);
			return null;
		}
	}

	private static BackupRunInfo TryEnrichWithLatestLocalRun(BackupRunInfo info)
	{
		BackupRunInfo backupRunInfo = TryLoadLatestLocalRun();
		if (backupRunInfo == null)
		{
			return info;
		}
		bool num = backupRunInfo.StartedAt == info.StartedAt;
		bool flag = PathsEqual(backupRunInfo.FilePath, info.FilePath);
		if (!num && !flag)
		{
			return info;
		}
		return info with
		{
			FileSizeBytes = (info.FileSizeBytes ?? backupRunInfo.FileSizeBytes),
			TargetDescription = (string.IsNullOrWhiteSpace(info.TargetDescription) ? backupRunInfo.TargetDescription : info.TargetDescription)
		};
	}

	private static bool PathsEqual(string? left, string? right)
	{
		if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
		{
			return false;
		}
		try
		{
			return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
		}
	}

	private static string UniquePath(string path)
	{
		if (!File.Exists(path))
		{
			return path;
		}
		string path2 = Path.GetDirectoryName(path) ?? string.Empty;
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		string extension = Path.GetExtension(path);
		for (int i = 2; i < 5000; i++)
		{
			string text = Path.Combine(path2, $"{fileNameWithoutExtension}-{i}{extension}");
			if (!File.Exists(text))
			{
				return text;
			}
		}
		return Path.Combine(path2, $"{fileNameWithoutExtension}-{Guid.NewGuid():N}{extension}");
	}

	private static string? TryZipSql(string sqlPath)
	{
		try
		{
			if (!File.Exists(sqlPath))
			{
				return null;
			}
			string text = UniquePath(Path.ChangeExtension(sqlPath, ".zip"));
			using (ZipArchive destination = ZipFile.Open(text, ZipArchiveMode.Create))
			{
				destination.CreateEntryFromFile(sqlPath, Path.GetFileName(sqlPath), CompressionLevel.Optimal);
			}
			File.Delete(sqlPath);
			return text;
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
		using MySqlDumpDefaultsFile mySqlDumpDefaultsFile = MySqlDumpDefaultsFile.Create(csb);
		List<string> list = BuildConnectionArgs(csb, mySqlDumpDefaultsFile?.Path);
		list.AddRange(new string[8]
		{
			"--databases",
			((MySqlBaseConnectionStringBuilder)csb).Database,
			"--routines",
			"--events",
			"--triggers",
			"--single-transaction",
			"--quick",
			"--result-file=" + outputSqlPath
		});
		if (File.Exists(outputSqlPath))
		{
			File.Delete(outputSqlPath);
		}
		ExecuteDumpWithRetry(dumpExe, list, includeCompatFlags: true);
		FileInfo fileInfo = new FileInfo(outputSqlPath);
		if (!fileInfo.Exists || fileInfo.Length <= 0)
		{
			throw new InvalidOperationException("Backup output file was not created or is empty.");
		}
	}

	private static void RunChangedDataDump(string dumpExe, MySqlConnectionStringBuilder csb, string outputSqlPath, DateTime since, BackupMode mode)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		Directory.CreateDirectory(Path.GetDirectoryName(outputSqlPath) ?? ".");
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			using MySqlDumpDefaultsFile mySqlDumpDefaultsFile = MySqlDumpDefaultsFile.Create(csb);
			List<string> list = new List<string>();
			MySqlCommand val = new MySqlCommand("SELECT table_name\n                     FROM information_schema.tables\n                     WHERE table_schema = DATABASE()\n                       AND table_type = 'BASE TABLE'\n                     ORDER BY table_name", connection);
			try
			{
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						string text = ((DbDataReader)(object)val2)["table_name"]?.ToString();
						if (!string.IsNullOrWhiteSpace(text))
						{
							list.Add(text);
						}
					}
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			File.WriteAllText(outputSqlPath, $"-- {mode} backup generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-- Baseline: {since:yyyy-MM-dd HH:mm:ss}\nUSE `{((MySqlBaseConnectionStringBuilder)csb).Database}`;\n" + "SET FOREIGN_KEY_CHECKS=0;\n\n");
			foreach (string item in list)
			{
				string text2 = ResolveTemporalColumn(connection, item);
				string text3 = ((text2 == null) ? null : $"`{text2}` >= '{since:yyyy-MM-dd HH:mm:ss}'");
				string text4 = Path.Combine(Path.GetTempPath(), $"bms-{Guid.NewGuid():N}.sql");
				try
				{
					List<string> list2 = BuildConnectionArgs(csb, mySqlDumpDefaultsFile?.Path);
					list2.AddRange(new string[8]
					{
						"--single-transaction",
						"--quick",
						"--no-create-info",
						"--skip-triggers",
						"--skip-lock-tables",
						"--result-file=" + text4,
						((MySqlBaseConnectionStringBuilder)csb).Database,
						item
					});
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list2.Insert(list2.Count - 2, "--where=" + text3);
					}
					ExecuteDumpWithRetry(dumpExe, list2, includeCompatFlags: true);
					if (File.Exists(text4))
					{
						string text5 = File.ReadAllText(text4);
						if (!string.IsNullOrWhiteSpace(text5))
						{
							File.AppendAllText(outputSqlPath, text5 + Environment.NewLine);
						}
					}
				}
				finally
				{
					try
					{
						if (File.Exists(text4))
						{
							File.Delete(text4);
						}
					}
					catch
					{
					}
				}
			}
			File.AppendAllText(outputSqlPath, "\nSET FOREIGN_KEY_CHECKS=1;\n");
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static string PrepareRestoreSql(string backupPath, out string? tempSqlPath)
	{
		tempSqlPath = null;
		string extension = Path.GetExtension(backupPath);
		if (string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase))
		{
			return backupPath;
		}
		if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Select a SQL or ZIP backup created by the system.");
		}
		using ZipArchive zipArchive = ZipFile.OpenRead(backupPath);
		ZipArchiveEntry? source = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry entry) => !string.IsNullOrWhiteSpace(entry.Name) && string.Equals(Path.GetExtension(entry.Name), ".sql", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException("The selected ZIP file does not contain a SQL backup.");
		tempSqlPath = Path.Combine(Path.GetTempPath(), $"bms-restore-{Guid.NewGuid():N}.sql");
		source.ExtractToFile(tempSqlPath, overwrite: true);
		return tempSqlPath;
	}

	private static void RunMySqlRestore(string mysqlExe, MySqlConnectionStringBuilder csb, string inputSqlPath)
	{
		if (!File.Exists(inputSqlPath))
		{
			throw new FileNotFoundException("The extracted SQL restore file could not be found.", inputSqlPath);
		}
		FileInfo fileInfo = new FileInfo(inputSqlPath);
		if (!fileInfo.Exists || fileInfo.Length <= 0)
		{
			throw new InvalidOperationException("Selected restore file is empty.");
		}
		using MySqlDumpDefaultsFile mySqlDumpDefaultsFile = MySqlDumpDefaultsFile.Create(csb);
		List<string> list = BuildConnectionArgs(csb, mySqlDumpDefaultsFile?.Path);
		list.Add("--default-character-set=utf8mb4");
		var (num, text) = RunProcessWithInputCaptureStderr(mysqlExe, list, inputSqlPath);
		if (num != 0)
		{
			throw new InvalidOperationException(string.IsNullOrWhiteSpace(text) ? $"mysql restore failed with exit code {num}." : ("mysql restore failed: " + text.Trim()));
		}
	}

	private static string? ResolveTemporalColumn(MySqlConnection conn, string tableName)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		string[] temporalColumnCandidates = TemporalColumnCandidates;
		foreach (string text in temporalColumnCandidates)
		{
			MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n                  FROM information_schema.columns\n                  WHERE table_schema = DATABASE()\n                    AND table_name = @table\n                    AND column_name = @column", conn);
			try
			{
				val.Parameters.AddWithValue("@table", (object)tableName);
				val.Parameters.AddWithValue("@column", (object)text);
				if (Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) > 0)
				{
					return text;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		return null;
	}

	private static List<string> BuildConnectionArgs(MySqlConnectionStringBuilder csb, string? defaultsFilePath)
	{
		List<string> list = new List<string>
		{
			"--host",
			((MySqlBaseConnectionStringBuilder)csb).Server,
			"--port",
			((MySqlBaseConnectionStringBuilder)csb).Port.ToString(),
			"--user",
			((MySqlBaseConnectionStringBuilder)csb).UserID
		};
		if (!string.IsNullOrWhiteSpace(defaultsFilePath))
		{
			list.Insert(0, "--defaults-extra-file=" + defaultsFilePath);
		}
		else if (((MySqlBaseConnectionStringBuilder)csb).Password != null)
		{
			list.Add("--password=" + ((MySqlBaseConnectionStringBuilder)csb).Password);
		}
		return list;
	}

	private static void ExecuteDumpWithRetry(string dumpExe, List<string> args, bool includeCompatFlags)
	{
		List<string>[] obj = ((!includeCompatFlags) ? new List<string>[1] { args } : new List<string>[2]
		{
			args.Concat(new string[2] { "--set-gtid-purged=OFF", "--column-statistics=0" }).ToList(),
			args
		});
		Exception ex = null;
		List<string>[] array = obj;
		foreach (List<string> args2 in array)
		{
			try
			{
				var (num, text) = RunProcessCaptureStderr(dumpExe, args2);
				if (num != 0)
				{
					throw new InvalidOperationException(string.IsNullOrWhiteSpace(text) ? $"mysqldump failed with exit code {num}." : ("mysqldump failed: " + text.Trim()));
				}
				return;
			}
			catch (Exception ex2)
			{
				ex = ex2;
				string text2 = ex2.Message.ToLowerInvariant();
				if (!text2.Contains("unknown option") && !text2.Contains("unknown variable") && !text2.Contains("unrecognized option"))
				{
					break;
				}
			}
		}
		throw ex ?? new InvalidOperationException("mysqldump failed.");
	}

	private static (int ExitCode, string Stderr) RunProcessCaptureStderr(string exePath, List<string> args)
	{
		using Process process = new Process();
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
		string item = process.StandardError.ReadToEnd();
		process.WaitForExit();
		return (ExitCode: process.ExitCode, Stderr: item);
	}

	private static (int ExitCode, string Stderr) RunProcessWithInputCaptureStderr(string exePath, List<string> args, string inputFilePath)
	{
		using Process process = new Process();
		process.StartInfo = new ProcessStartInfo
		{
			FileName = exePath,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardError = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = false
		};
		foreach (string arg in args)
		{
			process.StartInfo.ArgumentList.Add(arg);
		}
		process.Start();
		Task<string> task = process.StandardError.ReadToEndAsync();
		using (FileStream fileStream = File.OpenRead(inputFilePath))
		{
			fileStream.CopyTo(process.StandardInput.BaseStream);
		}
		process.StandardInput.Close();
		process.WaitForExit();
		string result = task.GetAwaiter().GetResult();
		return (ExitCode: process.ExitCode, Stderr: result);
	}

	private static string? FindMySqlDumpExe()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("BARANGAY_MYSQLDUMP_PATH");
		if (!string.IsNullOrWhiteSpace(environmentVariable))
		{
			environmentVariable = environmentVariable.Trim().Trim('"');
			if (File.Exists(environmentVariable))
			{
				return environmentVariable;
			}
		}
		foreach (string item in EnumerateMySqlDumpCandidates())
		{
			if (File.Exists(item))
			{
				return item;
			}
		}
		return null;
	}

	private static string? FindMySqlExe()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("BARANGAY_MYSQL_PATH");
		if (!string.IsNullOrWhiteSpace(environmentVariable))
		{
			environmentVariable = environmentVariable.Trim().Trim('"');
			if (File.Exists(environmentVariable))
			{
				return environmentVariable;
			}
		}
		string text = FindMySqlDumpExe();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string text2 = Path.Combine(Path.GetDirectoryName(text) ?? string.Empty, "mysql.exe");
			if (File.Exists(text2))
			{
				return text2;
			}
		}
		foreach (string item in EnumerateMySqlExeCandidates())
		{
			if (File.Exists(item))
			{
				return item;
			}
		}
		return null;
	}

	private static IEnumerable<string> EnumerateMySqlDumpCandidates()
	{
		foreach (string item in EnumerateExecutableCandidates("mysqldump.exe"))
		{
			yield return item;
		}
	}

	private static IEnumerable<string> EnumerateMySqlExeCandidates()
	{
		foreach (string item in EnumerateExecutableCandidates("mysql.exe"))
		{
			yield return item;
		}
	}

	private static IEnumerable<string> EnumerateExecutableCandidates(string executableName)
	{
		foreach (string item in EnumerateFromPath(executableName))
		{
			yield return item;
		}
		foreach (string item2 in EnumerateCommonInstalls(executableName))
		{
			yield return item2;
		}
	}

	private static IEnumerable<string> EnumerateFromPath(string executableName)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrWhiteSpace(environmentVariable))
		{
			yield break;
		}
		string[] array = environmentVariable.Split(';');
		for (int i = 0; i < array.Length; i++)
		{
			string text = (array[i] ?? string.Empty).Trim().Trim('"');
			if (!string.IsNullOrWhiteSpace(text))
			{
				yield return Path.Combine(text, executableName);
			}
		}
	}

	private static IEnumerable<string> EnumerateCommonInstalls(string executableName)
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		string[] array = new string[2] { folderPath, folderPath2 };
		foreach (string baseDir in array)
		{
			if (string.IsNullOrWhiteSpace(baseDir))
			{
				continue;
			}
			string text = Path.Combine(baseDir, "MySQL");
			if (Directory.Exists(text))
			{
				foreach (string item in SafeEnumerateDirectories(text, "MySQL Server*"))
				{
					yield return Path.Combine(item, "bin", executableName);
				}
			}
			string text2 = Path.Combine(baseDir, "MariaDB");
			if (!Directory.Exists(text2))
			{
				continue;
			}
			foreach (string item2 in SafeEnumerateDirectories(text2, "MariaDB*"))
			{
				yield return Path.Combine(item2, "bin", executableName);
			}
		}
		yield return Path.Combine("C:\\xampp\\mysql\\bin", executableName);
		using (IEnumerator<string> enumerator = SafeEnumerateFiles("C:\\wamp64\\bin\\mysql", executableName).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				yield return enumerator.Current;
				yield break;
			}
		}
		using IEnumerator<string> enumerator = SafeEnumerateFiles("C:\\laragon\\bin\\mysql", executableName).GetEnumerator();
		if (enumerator.MoveNext())
		{
			yield return enumerator.Current;
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
