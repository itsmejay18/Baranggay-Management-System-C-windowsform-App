using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal static class DbConnectionSettingsStore
{
    private const uint DefaultPort = 3306;
    private const uint ConnectionTimeoutSeconds = 5;
    private const string SettingsFileName = "db.connection.json";
    private const string EncryptedPrefix = "enc:";
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BarangayManagementSystem");
    private static readonly string FilePath = Path.Combine(SettingsDirectory, SettingsFileName);
    private static readonly string LegacyFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
    private static readonly byte[] ProtectionEntropy =
        Encoding.UTF8.GetBytes("BarangayManagementSystem.DbConnection.v1");

    public static bool TryLoad(out DatabaseConnectionProfile profile)
    {
        profile = DatabaseConnectionProfile.CreateDefault();
        try
        {
            string? path = ResolveLoadPath();
            if (path == null)
            {
                return false;
            }

            string json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<DatabaseConnectionProfile>(json);
            if (loaded == null)
            {
                return false;
            }

            profile = Normalize(loaded);
            profile.Password = DecodePassword(profile.Password);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static DatabaseConnectionProfile LoadOrDefault()
    {
        return TryLoad(out var profile)
            ? profile
            : DatabaseConnectionProfile.CreateDefault();
    }

    public static void Save(DatabaseConnectionProfile profile)
    {
        var normalized = Normalize(profile);
        var toSave = new DatabaseConnectionProfile
        {
            Server = normalized.Server,
            Port = normalized.Port,
            Database = normalized.Database,
            Username = normalized.Username,
            Password = EncodePassword(normalized.Password),
            UseSsl = normalized.UseSsl
        };
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(toSave, options);
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(FilePath, json);
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

    public static string BuildConnectionString(DatabaseConnectionProfile profile, bool includeDatabase = true)
    {
        var normalized = Normalize(profile);
        var builder = new MySqlConnectionStringBuilder
        {
            Server = normalized.Server,
            Port = normalized.Port,
            UserID = normalized.Username,
            Password = normalized.Password,
            SslMode = normalized.UseSsl ? MySqlSslMode.Preferred : MySqlSslMode.Disabled,
            AllowPublicKeyRetrieval = true,
            AllowUserVariables = true,
            ConnectionTimeout = ConnectionTimeoutSeconds
        };

        if (includeDatabase)
        {
            builder.Database = normalized.Database;
        }

        return builder.ConnectionString;
    }

    private static string EncodePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        if (password.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return password;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(password);
            byte[] protectedData = ProtectedData.Protect(data, ProtectionEntropy, DataProtectionScope.CurrentUser);
            return EncryptedPrefix + Convert.ToBase64String(protectedData);
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

        if (!value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            string payload = value.Substring(EncryptedPrefix.Length);
            byte[] protectedData = Convert.FromBase64String(payload);
            byte[] data = ProtectedData.Unprotect(protectedData, ProtectionEntropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static DatabaseConnectionProfile Normalize(DatabaseConnectionProfile? profile)
    {
        var normalized = profile ?? DatabaseConnectionProfile.CreateDefault();
        normalized.Server = string.IsNullOrWhiteSpace(normalized.Server)
            ? "localhost"
            : normalized.Server.Trim();
        normalized.Database = string.IsNullOrWhiteSpace(normalized.Database)
            ? "barangay_system"
            : normalized.Database.Trim();
        normalized.Username = string.IsNullOrWhiteSpace(normalized.Username)
            ? "root"
            : normalized.Username.Trim();
        normalized.Port = normalized.Port == 0 ? DefaultPort : normalized.Port;
        normalized.Password ??= string.Empty;
        return normalized;
    }
}
