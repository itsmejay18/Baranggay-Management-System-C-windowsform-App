using System;
using System.Collections.Generic;
using System.Data.Common;
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
		List<string> orderedMigrationFiles = GetOrderedMigrationFiles();
		if (orderedMigrationFiles.Count == 0)
		{
			AppLogger.LogWarning("Migrations directory not found; skipping migration runner.");
			return;
		}
		HashSet<string> hashSet = LoadAppliedMigrations(conn);
		foreach (string item in orderedMigrationFiles)
		{
			string fileName = Path.GetFileName(item);
			if (!hashSet.Contains(fileName))
			{
				string sql = File.ReadAllText(item);
				if (!IsManualMigration(sql))
				{
					ApplySqlScript(conn, fileName, sql);
					MarkApplied(conn, fileName);
					hashSet.Add(fileName);
				}
			}
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
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		if (OfflineDatabaseSupport.IsOffline || DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false))
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
				EnsureMigrationsTable(connection);
				MySqlCommand val = new MySqlCommand("SELECT migration_name\r\n                  FROM schema_migrations\r\n                  ORDER BY applied_at DESC, migration_name DESC\r\n                  LIMIT 1", connection);
				try
				{
					string text = ((DbCommand)(object)val).ExecuteScalar()?.ToString();
					if (string.IsNullOrWhiteSpace(text))
					{
						return null;
					}
					return Path.GetFileNameWithoutExtension(text);
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
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("\r\n            CREATE TABLE IF NOT EXISTS schema_migrations (\r\n                migration_name VARCHAR(255) NOT NULL PRIMARY KEY,\r\n                applied_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP\r\n            )", conn);
		try
		{
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static IReadOnlyList<string> GetPendingMigrationNames(MySqlConnection conn, bool includeManual, bool onlyManual)
	{
		EnsureMigrationsTable(conn);
		List<string> orderedMigrationFiles = GetOrderedMigrationFiles();
		if (orderedMigrationFiles.Count == 0)
		{
			return Array.Empty<string>();
		}
		HashSet<string> hashSet = LoadAppliedMigrations(conn);
		List<string> list = new List<string>();
		foreach (string item in orderedMigrationFiles)
		{
			string fileName = Path.GetFileName(item);
			if (hashSet.Contains(fileName))
			{
				continue;
			}
			bool flag = IsManualMigration(File.ReadAllText(item));
			if (onlyManual)
			{
				if (!flag)
				{
					continue;
				}
			}
			else if (!includeManual && flag)
			{
				continue;
			}
			list.Add(fileName);
		}
		return list;
	}

	private static (int DateKey, int Priority, string Name) GetSortKey(string filePath)
	{
		string fileName = Path.GetFileName(filePath);
		int item = 0;
		if (fileName.Length >= 8 && int.TryParse(fileName.Substring(0, 8), out var result))
		{
			item = result;
		}
		string text = fileName.ToLowerInvariant();
		int item2 = ((!text.Contains("new_schema")) ? (text.Contains("patch") ? 1 : (text.Contains("role_permission") ? 2 : (text.Contains("backup_run") ? 3 : ((!text.Contains("add_indexes")) ? 5 : 9)))) : 0);
		return (DateKey: item, Priority: item2, Name: fileName);
	}

	private static HashSet<string> LoadAppliedMigrations(MySqlConnection conn)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		MySqlCommand val = new MySqlCommand("SELECT migration_name FROM schema_migrations", conn);
		try
		{
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					string text = ((DbDataReader)(object)val2)["migration_name"]?.ToString();
					if (!string.IsNullOrWhiteSpace(text))
					{
						hashSet.Add(text);
					}
				}
				return hashSet;
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

	private static void MarkApplied(MySqlConnection conn, string migrationName)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("INSERT INTO schema_migrations (migration_name) VALUES (@name)", conn);
		try
		{
			val.Parameters.AddWithValue("@name", (object)migrationName);
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool IsManualMigration(string sql)
	{
		if (string.IsNullOrWhiteSpace(sql))
		{
			return false;
		}
		return sql.IndexOf("@manual", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static void ApplySqlScript(MySqlConnection conn, string migrationName, string sql)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		List<string> list = SplitStatements(sql).ToList();
		if (list.Count == 0)
		{
			return;
		}
		AppLogger.LogInfo($"Applying migration {migrationName} ({list.Count} statements)...");
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			MySqlCommand val = new MySqlCommand(text, conn);
			try
			{
				((DbCommand)(object)val).CommandTimeout = 60;
				try
				{
					((DbCommand)(object)val).ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					string text2 = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
					if (text2.Length > 240)
					{
						text2 = text2.Substring(0, 240) + "...";
					}
					AppLogger.LogError($"Migration {migrationName} failed at statement {i + 1}/{list.Count}: {text2}", ex);
					throw;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	private static IEnumerable<string> SplitStatements(string sql)
	{
		if (sql == null)
		{
			yield break;
		}
		if (sql.Length > 0 && sql[0] == '\ufeff')
		{
			string text = sql;
			sql = text.Substring(1, text.Length - 1);
		}
		string delimiter = ";";
		StringBuilder sb = new StringBuilder();
		using StringReader reader = new StringReader(sql);
		string text2;
		while ((text2 = reader.ReadLine()) != null)
		{
			string text3 = text2.Trim();
			if (text3.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
			{
				string text4 = text3.Substring("DELIMITER".Length).Trim();
				delimiter = (string.IsNullOrWhiteSpace(text4) ? ";" : text4);
				continue;
			}
			sb.AppendLine(text2);
			while (true)
			{
				int num = FindDelimiterIndex(sb, delimiter);
				if (num < 0)
				{
					break;
				}
				string text5 = sb.ToString(0, num);
				sb.Remove(0, num + delimiter.Length);
				text5 = text5.Trim();
				if (!string.IsNullOrWhiteSpace(text5))
				{
					yield return text5;
				}
			}
		}
		string text6 = sb.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(text6))
		{
			yield return text6;
		}
	}

	private static int FindDelimiterIndex(StringBuilder sb, string delimiter)
	{
		if (sb.Length == 0)
		{
			return -1;
		}
		if (delimiter == null)
		{
			delimiter = ";";
		}
		if (delimiter.Length == 0)
		{
			delimiter = ";";
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		for (int i = 0; i <= sb.Length - delimiter.Length; i++)
		{
			char c = sb[i];
			char c2 = ((i + 1 < sb.Length) ? sb[i + 1] : '\0');
			if (flag4)
			{
				if (c == '\n')
				{
					flag4 = false;
				}
				continue;
			}
			if (flag5)
			{
				if (c == '*' && c2 == '/')
				{
					flag5 = false;
					i++;
				}
				continue;
			}
			if (flag)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '\'':
					flag = false;
					break;
				}
				continue;
			}
			if (flag2)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '"':
					flag2 = false;
					break;
				}
				continue;
			}
			if (flag3)
			{
				if (c == '`')
				{
					flag3 = false;
				}
				continue;
			}
			switch (c)
			{
			case '#':
				flag4 = true;
				continue;
			case '-':
				if (c2 == '-')
				{
					char c3 = ((i + 2 < sb.Length) ? sb[i + 2] : '\0');
					if (c3 == '\0' || char.IsWhiteSpace(c3))
					{
						flag4 = true;
						i++;
						continue;
					}
				}
				break;
			}
			if (c == '/' && c2 == '*')
			{
				flag5 = true;
				i++;
				continue;
			}
			switch (c)
			{
			case '\'':
				flag = true;
				continue;
			case '"':
				flag2 = true;
				continue;
			case '`':
				flag3 = true;
				continue;
			}
			bool flag6 = true;
			for (int j = 0; j < delimiter.Length; j++)
			{
				if (sb[i + j] != delimiter[j])
				{
					flag6 = false;
					break;
				}
			}
			if (!flag6)
			{
				continue;
			}
			return i;
		}
		return -1;
	}

	private static string? TryGetMigrationsDirectory()
	{
		string text = Path.Combine(AppContext.BaseDirectory, "Database", "migrations");
		if (Directory.Exists(text))
		{
			return text;
		}
		string text2 = TryGetProjectRoot();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			text = Path.Combine(text2, "Database", "migrations");
			if (Directory.Exists(text))
			{
				return text;
			}
		}
		return null;
	}

	private static List<string> GetOrderedMigrationFiles()
	{
		string text = TryGetMigrationsDirectory();
		if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
		{
			return new List<string>();
		}
		return Directory.EnumerateFiles(text, "*.sql", SearchOption.TopDirectoryOnly).OrderBy(GetSortKey).ToList();
	}

	private static string? TryGetProjectRoot()
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(AppContext.BaseDirectory);
		for (int i = 0; i < 8; i++)
		{
			if (directoryInfo == null)
			{
				break;
			}
			if (File.Exists(Path.Combine(directoryInfo.FullName, "baranggaysystem1.csproj")))
			{
				return directoryInfo.FullName;
			}
			directoryInfo = directoryInfo.Parent;
		}
		return null;
	}
}
