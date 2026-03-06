using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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

        private static readonly string connectionString = ResolveConnectionString();

        private static string ResolveConnectionString()
        {
            string? envConnection = Environment.GetEnvironmentVariable("BARANGAY_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(envConnection))
            {
                envConnection = NormalizeConnectionString(envConnection);
                if (CanOpen(envConnection))
                {
                    return envConnection;
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
                    return candidate;
                }
            }

            return string.IsNullOrWhiteSpace(envConnection)
                ? BuildCandidate("localhost", DefaultPort, DefaultUser, DefaultPassword)
                : envConnection;
        }

        private static string NormalizeConnectionString(string value)
        {
            // MySql.Data uses SslMode=Disabled, not SslMode=None.
            string normalized = value.Replace("SslMode=None", "SslMode=Disabled", StringComparison.OrdinalIgnoreCase);

            try
            {
                var builder = new MySqlConnectionStringBuilder(normalized);
                builder.SslMode = MySqlSslMode.Disabled;

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

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
