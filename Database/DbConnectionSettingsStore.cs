using System;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database;

internal static class DbConnectionSettingsStore
{
    private const uint DefaultPort = 3306;
    private const uint ConnectionTimeoutSeconds = 5;
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "db.connection.json");

    public static bool TryLoad(out DatabaseConnectionProfile profile)
    {
        profile = DatabaseConnectionProfile.CreateDefault();
        try
        {
            if (!File.Exists(FilePath))
            {
                return false;
            }

            string json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<DatabaseConnectionProfile>(json);
            if (loaded == null)
            {
                return false;
            }

            profile = Normalize(loaded);
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
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(normalized, options);
        File.WriteAllText(FilePath, json);
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

    private static DatabaseConnectionProfile Normalize(DatabaseConnectionProfile? profile)
    {
        var normalized = profile ?? DatabaseConnectionProfile.CreateDefault();
        normalized.Mode = string.Equals(normalized.Mode, "Network", StringComparison.OrdinalIgnoreCase)
            ? "Network"
            : "Local";
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
