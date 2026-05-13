using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal static class DbConnectionSettingsStore
{
	private const uint DefaultPort = 3306u;

	private const uint ConnectionTimeoutSeconds = 5u;

	private const string SettingsFileName = "db.connection.json";

	private const string EncryptedPrefix = "enc:";

	private static readonly string SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarangayManagementSystem");

	private static readonly string FilePath = Path.Combine(SettingsDirectory, "db.connection.json");

	private static readonly string LegacyFilePath = Path.Combine(AppContext.BaseDirectory, "db.connection.json");

	private static readonly byte[] ProtectionEntropy = Encoding.UTF8.GetBytes("BarangayManagementSystem.DbConnection.v1");

	public static bool TryLoad(out DatabaseConnectionProfile profile)
	{
		if (TryLoadSettings(out StoredDatabaseConnectionSettings settings))
		{
			profile = ResolveSelectedProfile(settings);
			return true;
		}
		profile = DatabaseConnectionProfile.CreateDefault();
		return false;
	}

	public static DatabaseConnectionProfile LoadOrDefault()
	{
		if (!TryLoad(out DatabaseConnectionProfile profile))
		{
			return DatabaseConnectionProfile.CreateDefault();
		}
		return profile;
	}

	public static string LoadSelectedProfileKeyOrDefault()
	{
		return LoadSettingsOrDefault().SelectedProfileKey;
	}

	public static IReadOnlyList<DatabaseConnectionOption> GetAvailableOptions()
	{
		return BuildOptions(LoadSettingsOrDefault().CustomProfile);
	}

	public static DatabaseConnectionOption GetSelectedOptionOrDefault()
	{
		StoredDatabaseConnectionSettings settings = LoadSettingsOrDefault();
		IReadOnlyList<DatabaseConnectionOption> source = BuildOptions(settings.CustomProfile);
		return source.FirstOrDefault((DatabaseConnectionOption option) => string.Equals(option.Key, settings.SelectedProfileKey, StringComparison.OrdinalIgnoreCase)) ?? source.First((DatabaseConnectionOption option) => string.Equals(option.Key, "localhost", StringComparison.OrdinalIgnoreCase));
	}

	public static DatabaseConnectionOption GetLocalOption()
	{
		return GetOptionOrDefault("localhost");
	}

	public static bool IsSqliteProfileKey(string? selectedProfileKey)
	{
		return string.Equals(NormalizeKey(selectedProfileKey), "sqlite", StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsSqliteSelected()
	{
		return IsSqliteProfileKey(LoadSelectedProfileKeyOrDefault());
	}

	public static DatabaseConnectionOption GetPreferredOnlineOption()
	{
		StoredDatabaseConnectionSettings settings = LoadSettingsOrDefault();
		IReadOnlyList<DatabaseConnectionOption> source = BuildOptions(settings.CustomProfile);
		DatabaseConnectionOption databaseConnectionOption = source.FirstOrDefault((DatabaseConnectionOption option) => string.Equals(option.Key, settings.SelectedProfileKey, StringComparison.OrdinalIgnoreCase)) ?? source.First((DatabaseConnectionOption option) => string.Equals(option.Key, "localhost", StringComparison.OrdinalIgnoreCase));
		if (!databaseConnectionOption.UsesSqlite && !string.Equals(databaseConnectionOption.Key, "localhost", StringComparison.OrdinalIgnoreCase) && !IsLocalLikeProfile(databaseConnectionOption.Profile))
		{
			return databaseConnectionOption;
		}
		DatabaseConnectionOption databaseConnectionOption2 = source.First((DatabaseConnectionOption option) => string.Equals(option.Key, "custom", StringComparison.OrdinalIgnoreCase));
		if (!ProfilesEqual(databaseConnectionOption2.Profile, DatabaseConnectionProfile.CreateDefault()) && !IsLocalLikeProfile(databaseConnectionOption2.Profile))
		{
			return databaseConnectionOption2;
		}
		return source.First((DatabaseConnectionOption option) => string.Equals(option.Key, "hostinger", StringComparison.OrdinalIgnoreCase));
	}

	public static DatabaseConnectionOption GetOptionOrDefault(string optionKey)
	{
		IReadOnlyList<DatabaseConnectionOption> availableOptions = GetAvailableOptions();
		return availableOptions.FirstOrDefault((DatabaseConnectionOption option) => string.Equals(option.Key, NormalizeKey(optionKey), StringComparison.OrdinalIgnoreCase)) ?? availableOptions.First((DatabaseConnectionOption option) => string.Equals(option.Key, "localhost", StringComparison.OrdinalIgnoreCase));
	}

	public static void Save(DatabaseConnectionProfile profile)
	{
		DatabaseConnectionProfile databaseConnectionProfile = Normalize(profile);
		StoredDatabaseConnectionSettings storedDatabaseConnectionSettings = LoadSettingsOrDefault();
		string text = MatchBuiltInProfileKey(databaseConnectionProfile);
		storedDatabaseConnectionSettings.SelectedProfileKey = text ?? "custom";
		if (storedDatabaseConnectionSettings.SelectedProfileKey == "custom")
		{
			storedDatabaseConnectionSettings.CustomProfile = databaseConnectionProfile;
		}
		SaveSettings(storedDatabaseConnectionSettings);
	}

	public static void SaveSelectedProfile(string selectedProfileKey, DatabaseConnectionProfile? customProfile = null)
	{
		StoredDatabaseConnectionSettings storedDatabaseConnectionSettings = LoadSettingsOrDefault();
		storedDatabaseConnectionSettings.SelectedProfileKey = NormalizeKey(selectedProfileKey);
		if (customProfile != null)
		{
			storedDatabaseConnectionSettings.CustomProfile = Normalize(customProfile);
		}
		SaveSettings(storedDatabaseConnectionSettings);
	}

	public static bool IsCustomProfileKey(string? selectedProfileKey)
	{
		return string.Equals(NormalizeKey(selectedProfileKey), "custom", StringComparison.OrdinalIgnoreCase);
	}

	public static string BuildConnectionString(DatabaseConnectionProfile profile, bool includeDatabase = true)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		DatabaseConnectionProfile databaseConnectionProfile = Normalize(profile);
		MySqlConnectionStringBuilder val = new MySqlConnectionStringBuilder
		{
			Server = databaseConnectionProfile.Server,
			Port = databaseConnectionProfile.Port,
			UserID = databaseConnectionProfile.Username,
			Password = databaseConnectionProfile.Password,
			SslMode = (MySqlSslMode)(databaseConnectionProfile.UseSsl ? 1 : 0),
			AllowPublicKeyRetrieval = true,
			AllowUserVariables = true,
			ConnectionTimeout = 5u
		};
		if (includeDatabase)
		{
			((MySqlBaseConnectionStringBuilder)val).Database = databaseConnectionProfile.Database;
		}
		return ((DbConnectionStringBuilder)(object)val).ConnectionString;
	}

	private static bool TryLoadSettings(out StoredDatabaseConnectionSettings settings)
	{
		settings = CreateDefaultSettings();
		try
		{
			string text = ResolveLoadPath();
			if (text == null)
			{
				return false;
			}
			string json = File.ReadAllText(text);
			if (TryDeserializeSettings(json, out settings))
			{
				return true;
			}
			if (TryDeserializeLegacyProfile(json, out DatabaseConnectionProfile profile))
			{
				settings = CreateSettingsFromLegacyProfile(profile);
				return true;
			}
			return false;
		}
		catch
		{
			settings = CreateDefaultSettings();
			return false;
		}
	}

	private static StoredDatabaseConnectionSettings LoadSettingsOrDefault()
	{
		if (!TryLoadSettings(out StoredDatabaseConnectionSettings settings))
		{
			return CreateDefaultSettings();
		}
		return settings;
	}

	private static bool TryDeserializeSettings(string json, out StoredDatabaseConnectionSettings settings)
	{
		settings = CreateDefaultSettings();
		try
		{
			StoredDatabaseConnectionSettings storedDatabaseConnectionSettings = JsonSerializer.Deserialize<StoredDatabaseConnectionSettings>(json);
			if (storedDatabaseConnectionSettings == null)
			{
				return false;
			}
			settings = Normalize(storedDatabaseConnectionSettings);
			settings.CustomProfile.Password = DecodePassword(settings.CustomProfile.Password);
			return true;
		}
		catch
		{
			settings = CreateDefaultSettings();
			return false;
		}
	}

	private static bool TryDeserializeLegacyProfile(string json, out DatabaseConnectionProfile profile)
	{
		profile = DatabaseConnectionProfile.CreateDefault();
		try
		{
			DatabaseConnectionProfile databaseConnectionProfile = JsonSerializer.Deserialize<DatabaseConnectionProfile>(json);
			if (databaseConnectionProfile == null)
			{
				return false;
			}
			profile = Normalize(databaseConnectionProfile);
			profile.Password = DecodePassword(profile.Password);
			return true;
		}
		catch
		{
			profile = DatabaseConnectionProfile.CreateDefault();
			return false;
		}
	}

	private static void SaveSettings(StoredDatabaseConnectionSettings settings)
	{
		StoredDatabaseConnectionSettings storedDatabaseConnectionSettings = Normalize(settings);
		StoredDatabaseConnectionSettings value = new StoredDatabaseConnectionSettings
		{
			SelectedProfileKey = storedDatabaseConnectionSettings.SelectedProfileKey,
			CustomProfile = new DatabaseConnectionProfile
			{
				Server = storedDatabaseConnectionSettings.CustomProfile.Server,
				Port = storedDatabaseConnectionSettings.CustomProfile.Port,
				Database = storedDatabaseConnectionSettings.CustomProfile.Database,
				Username = storedDatabaseConnectionSettings.CustomProfile.Username,
				Password = EncodePassword(storedDatabaseConnectionSettings.CustomProfile.Password),
				UseSsl = storedDatabaseConnectionSettings.CustomProfile.UseSsl
			}
		};
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		string contents = JsonSerializer.Serialize(value, options);
		Directory.CreateDirectory(SettingsDirectory);
		File.WriteAllText(FilePath, contents);
	}

	private static string? ResolveLoadPath()
	{
		if (File.Exists(FilePath))
		{
			return FilePath;
		}
		if (File.Exists(LegacyFilePath))
		{
			return LegacyFilePath;
		}
		return null;
	}

	private static IReadOnlyList<DatabaseConnectionOption> BuildOptions(DatabaseConnectionProfile? customProfile)
	{
		DatabaseConnectionProfile profile = Normalize(customProfile);
		return new List<DatabaseConnectionOption>
		{
			new DatabaseConnectionOption
			{
				Key = "localhost",
				DisplayName = "Localhost",
				Description = "Use the MySQL database running on this computer.",
				Profile = DatabaseConnectionProfile.CreateDefault()
			},
			new DatabaseConnectionOption
			{
				Key = "hostinger",
				DisplayName = "Hostinger Cloud",
				Description = "Use the built-in Hostinger connection configured for this system.",
				Profile = Normalize(DBConnection.GetBootstrapProfile())
			},
			new DatabaseConnectionOption
			{
				Key = "sqlite",
				DisplayName = "SQLite Local File",
				Description = "Use the bundled local SQLite database stored in the Database/sqlite folder for this app.",
				Profile = DatabaseConnectionProfile.CreateDefault(),
				UsesSqlite = true,
				SqliteFilePath = OfflineDatabaseSupport.GetDatabasePath()
			},
			new DatabaseConnectionOption
			{
				Key = "custom",
				DisplayName = "Custom",
				Description = "Manually manage your own server, database, user, password, and SSL settings.",
				Profile = profile
			}
		};
	}

	private static StoredDatabaseConnectionSettings CreateDefaultSettings()
	{
		return new StoredDatabaseConnectionSettings
		{
			SelectedProfileKey = "sqlite",
			CustomProfile = DatabaseConnectionProfile.CreateDefault()
		};
	}

	private static StoredDatabaseConnectionSettings CreateSettingsFromLegacyProfile(DatabaseConnectionProfile profile)
	{
		DatabaseConnectionProfile databaseConnectionProfile = Normalize(profile);
		string text = MatchBuiltInProfileKey(databaseConnectionProfile);
		return new StoredDatabaseConnectionSettings
		{
			SelectedProfileKey = (text ?? "custom"),
			CustomProfile = databaseConnectionProfile
		};
	}

	private static StoredDatabaseConnectionSettings Normalize(StoredDatabaseConnectionSettings? settings)
	{
		StoredDatabaseConnectionSettings? obj = settings ?? CreateDefaultSettings();
		obj.SelectedProfileKey = NormalizeKey(obj.SelectedProfileKey);
		obj.CustomProfile = Normalize(obj.CustomProfile);
		return obj;
	}

	private static DatabaseConnectionProfile ResolveSelectedProfile(StoredDatabaseConnectionSettings settings)
	{
		return NormalizeKey(settings.SelectedProfileKey) switch
		{
			"hostinger" => Normalize(DBConnection.GetBootstrapProfile()), 
			"custom" => Normalize(settings.CustomProfile), 
			"sqlite" => DatabaseConnectionProfile.CreateDefault(), 
			_ => DatabaseConnectionProfile.CreateDefault(), 
		};
	}

	private static string NormalizeKey(string? selectedProfileKey)
	{
		return (selectedProfileKey?.Trim().ToLowerInvariant() ?? string.Empty) switch
		{
			"hostinger" => "hostinger", 
			"sqlite" => "sqlite", 
			"custom" => "custom", 
			_ => "localhost", 
		};
	}

	private static string? MatchBuiltInProfileKey(DatabaseConnectionProfile profile)
	{
		DatabaseConnectionProfile left = Normalize(profile);
		if (ProfilesEqual(left, DatabaseConnectionProfile.CreateDefault()))
		{
			return "localhost";
		}
		if (ProfilesEqual(left, DBConnection.GetBootstrapProfile()))
		{
			return "hostinger";
		}
		return null;
	}

	private static bool ProfilesEqual(DatabaseConnectionProfile left, DatabaseConnectionProfile right)
	{
		DatabaseConnectionProfile databaseConnectionProfile = Normalize(left);
		DatabaseConnectionProfile databaseConnectionProfile2 = Normalize(right);
		if (string.Equals(databaseConnectionProfile.Server, databaseConnectionProfile2.Server, StringComparison.OrdinalIgnoreCase) && databaseConnectionProfile.Port == databaseConnectionProfile2.Port && string.Equals(databaseConnectionProfile.Database, databaseConnectionProfile2.Database, StringComparison.OrdinalIgnoreCase) && string.Equals(databaseConnectionProfile.Username, databaseConnectionProfile2.Username, StringComparison.OrdinalIgnoreCase) && string.Equals(databaseConnectionProfile.Password, databaseConnectionProfile2.Password, StringComparison.Ordinal))
		{
			return databaseConnectionProfile.UseSsl == databaseConnectionProfile2.UseSsl;
		}
		return false;
	}

	private static bool IsLocalLikeProfile(DatabaseConnectionProfile profile)
	{
		string server = Normalize(profile).Server;
		if (!string.Equals(server, "localhost", StringComparison.OrdinalIgnoreCase) && !string.Equals(server, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(server, ".", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string EncodePassword(string? password)
	{
		if (string.IsNullOrEmpty(password))
		{
			return string.Empty;
		}
		if (password.StartsWith("enc:", StringComparison.Ordinal))
		{
			return password;
		}
		try
		{
			byte[] inArray = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), ProtectionEntropy, DataProtectionScope.CurrentUser);
			return "enc:" + Convert.ToBase64String(inArray);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string DecodePassword(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}
		if (!value.StartsWith("enc:", StringComparison.Ordinal))
		{
			return value;
		}
		try
		{
			byte[] bytes = ProtectedData.Unprotect(Convert.FromBase64String(value.Substring("enc:".Length)), ProtectionEntropy, DataProtectionScope.CurrentUser);
			return Encoding.UTF8.GetString(bytes);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static DatabaseConnectionProfile Normalize(DatabaseConnectionProfile? profile)
	{
		DatabaseConnectionProfile databaseConnectionProfile = profile ?? DatabaseConnectionProfile.CreateDefault();
		databaseConnectionProfile.Server = (string.IsNullOrWhiteSpace(databaseConnectionProfile.Server) ? "localhost" : databaseConnectionProfile.Server.Trim());
		databaseConnectionProfile.Database = (string.IsNullOrWhiteSpace(databaseConnectionProfile.Database) ? "barangay_system" : databaseConnectionProfile.Database.Trim());
		databaseConnectionProfile.Username = (string.IsNullOrWhiteSpace(databaseConnectionProfile.Username) ? "root" : databaseConnectionProfile.Username.Trim());
		databaseConnectionProfile.Port = ((databaseConnectionProfile.Port == 0) ? 3306u : databaseConnectionProfile.Port);
		DatabaseConnectionProfile databaseConnectionProfile2 = databaseConnectionProfile;
		if (databaseConnectionProfile2.Password == null)
		{
			string text = (databaseConnectionProfile2.Password = string.Empty);
		}
		return databaseConnectionProfile;
	}
}
