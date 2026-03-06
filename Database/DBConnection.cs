using MySql.Data.MySqlClient;
using System;
using System.Linq;

namespace baranggaysystem1.Database
{
    internal class DBConnection
    {
        private const string DefaultDatabase = "barangay_system";
        private const string DefaultUser = "root";
        private const string DefaultPassword = "123456";
        private const uint DefaultPort = 3306;
        private const uint AlternatePort = 3307;
        private const uint ConnectionTimeoutSeconds = 5;
        private static readonly object SyncRoot = new();

        private static string? resolvedConnectionString;
        private static string? runtimeConnectionString;

        private static string ResolveConnectionString()
        {
            lock (SyncRoot)
            {
                if (!string.IsNullOrWhiteSpace(resolvedConnectionString))
                {
                    return resolvedConnectionString;
                }
            }

            string? runtime = null;
            lock (SyncRoot)
            {
                runtime = runtimeConnectionString;
            }

            if (!string.IsNullOrWhiteSpace(runtime))
            {
                runtime = NormalizeConnectionString(runtime);
                if (CanOpen(runtime))
                {
                    return Cache(runtime);
                }
            }

            string? envConnection = Environment.GetEnvironmentVariable("BARANGAY_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(envConnection))
            {
                envConnection = NormalizeConnectionString(envConnection);
                if (CanOpen(envConnection))
                {
                    return Cache(envConnection);
                }
            }

            string? savedConnection = null;
            if (DbConnectionSettingsStore.TryLoad(out var profile))
            {
                savedConnection = NormalizeConnectionString(DbConnectionSettingsStore.BuildConnectionString(profile));
                if (CanOpen(savedConnection))
                {
                    return Cache(savedConnection);
                }
            }

            var candidates = new[]
            {
                BuildCandidate("localhost", DefaultPort, DefaultUser, DefaultPassword),
                BuildCandidate("localhost", DefaultPort, DefaultUser, string.Empty),
                BuildCandidate("127.0.0.1", DefaultPort, DefaultUser, string.Empty),
                BuildCandidate("localhost", AlternatePort, DefaultUser, DefaultPassword),
                BuildCandidate("localhost", AlternatePort, DefaultUser, string.Empty),
                BuildCandidate("127.0.0.1", AlternatePort, DefaultUser, string.Empty)
            };

            foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (CanOpen(candidate))
                {
                    return Cache(candidate);
                }
            }

            if (!string.IsNullOrWhiteSpace(runtime))
            {
                return Cache(runtime);
            }

            if (!string.IsNullOrWhiteSpace(envConnection))
            {
                return Cache(envConnection);
            }

            if (!string.IsNullOrWhiteSpace(savedConnection))
            {
                return Cache(savedConnection);
            }

            return Cache(BuildCandidate("localhost", DefaultPort, DefaultUser, DefaultPassword));
        }

        private static string NormalizeConnectionString(string value)
        {
            // MySql.Data uses SslMode=Disabled, not SslMode=None.
            string normalized = value.Replace("SslMode=None", "SslMode=Disabled", StringComparison.OrdinalIgnoreCase);

            try
            {
                var builder = new MySqlConnectionStringBuilder(normalized);

                // Required by migration scripts that use MySQL user variables (e.g. PREPARE ... FROM @sql).
                builder.AllowUserVariables = true;
                builder.AllowPublicKeyRetrieval = true;

                if (builder.ConnectionTimeout == 0)
                {
                    builder.ConnectionTimeout = ConnectionTimeoutSeconds;
                }

                return builder.ConnectionString;
            }
            catch
            {
                return normalized;
            }
        }

        private static string Cache(string value)
        {
            lock (SyncRoot)
            {
                resolvedConnectionString = value;
            }

            return value;
        }

        private static string BuildCandidate(string server, uint port, string user, string password)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                Port = port,
                Database = DefaultDatabase,
                UserID = user,
                Password = password,
                SslMode = MySqlSslMode.Disabled,
                AllowPublicKeyRetrieval = true,
                AllowUserVariables = true,
                ConnectionTimeout = ConnectionTimeoutSeconds
            };

            return builder.ConnectionString;
        }

        private static bool CanOpen(string candidate)
        {
            try
            {
                using var conn = new MySqlConnection(candidate);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetCurrentConnectionString()
        {
            return ResolveConnectionString();
        }

        public static void SetRuntimeConnectionString(string connectionString)
        {
            string normalized = NormalizeConnectionString(connectionString);
            lock (SyncRoot)
            {
                runtimeConnectionString = normalized;
                resolvedConnectionString = null;
            }
        }

        public static bool TryOpen(string connectionString, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                string normalized = NormalizeConnectionString(connectionString);
                using var conn = new MySqlConnection(normalized);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static bool TryOpenCurrent(out string errorMessage)
        {
            return TryOpen(GetCurrentConnectionString(), out errorMessage);
        }

        public static string BuildFromParts(string server, uint port, string database, string user, string password, bool useSsl)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                Port = port == 0 ? DefaultPort : port,
                Database = database,
                UserID = user,
                Password = password,
                SslMode = useSsl ? MySqlSslMode.Preferred : MySqlSslMode.Disabled,
                AllowPublicKeyRetrieval = true,
                AllowUserVariables = true,
                ConnectionTimeout = ConnectionTimeoutSeconds
            };

            return NormalizeConnectionString(builder.ConnectionString);
        }

        public static string BuildServerConnectionString(string server, uint port, string user, string password, bool useSsl)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                Port = port == 0 ? DefaultPort : port,
                UserID = user,
                Password = password,
                SslMode = useSsl ? MySqlSslMode.Preferred : MySqlSslMode.Disabled,
                AllowPublicKeyRetrieval = true,
                AllowUserVariables = true,
                ConnectionTimeout = ConnectionTimeoutSeconds
            };

            return NormalizeConnectionString(builder.ConnectionString);
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ResolveConnectionString());
        }
    }
}
