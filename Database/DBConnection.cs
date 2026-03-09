using MySql.Data.MySqlClient;
using System;
using System.Linq;
using System.Security.Authentication;

namespace baranggaysystem1.Database
{
    internal class DBConnection
    {
        private const string DefaultDatabase = "barangay_system";
        private const string DefaultUser = "root";
        private const string DefaultPassword = "123456";
        private const string BootstrapConnectionString = "server=srv1237.hstgr.io;port=3306;database=u621755393_CBaranggayMana;user id=u621755393_cbaranggay;password=Dssc@2026;SslMode=Disabled;AllowPublicKeyRetrieval=true;AllowUserVariables=true;ConnectionTimeout=5";
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
                if (TryResolveWorkingConnectionString(runtime, out string workingRuntime, out _))
                {
                    return Cache(workingRuntime);
                }
            }

            string? envConnection = Environment.GetEnvironmentVariable("BARANGAY_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(envConnection))
            {
                envConnection = NormalizeConnectionString(envConnection);
                if (TryResolveWorkingConnectionString(envConnection, out string workingEnvironment, out _))
                {
                    return Cache(workingEnvironment);
                }
            }

            string? savedConnection = null;
            string bootstrapConnection = NormalizeConnectionString(BootstrapConnectionString);
            if (TryResolveWorkingConnectionString(bootstrapConnection, out string workingBootstrap, out _))
            {
                return Cache(workingBootstrap);
            }

            if (DbConnectionSettingsStore.TryLoad(out var profile))
            {
                savedConnection = NormalizeConnectionString(DbConnectionSettingsStore.BuildConnectionString(profile));
                if (TryResolveWorkingConnectionString(savedConnection, out string workingSaved, out _))
                {
                    return Cache(workingSaved);
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
                if (TryResolveWorkingConnectionString(candidate, out string workingCandidate, out _))
                {
                    return Cache(workingCandidate);
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

            return Cache(bootstrapConnection);
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

        private static bool TryResolveWorkingConnectionString(
            string candidate,
            out string workingConnectionString,
            out string errorMessage)
        {
            string normalized = NormalizeConnectionString(candidate);
            if (TryOpenDirect(normalized, out errorMessage, out Exception? openException))
            {
                workingConnectionString = normalized;
                return true;
            }

            if (TryBuildSslDisabledFallback(normalized, openException, out string sslDisabledFallback))
            {
                if (TryOpenDirect(sslDisabledFallback, out string fallbackErrorMessage, out _))
                {
                    workingConnectionString = sslDisabledFallback;
                    errorMessage = string.Empty;
                    return true;
                }

                errorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? fallbackErrorMessage
                    : $"{errorMessage} Retry with SSL disabled failed: {fallbackErrorMessage}";
            }

            workingConnectionString = normalized;
            return false;
        }

        private static bool TryOpenDirect(string connectionString, out string errorMessage, out Exception? openException)
        {
            errorMessage = string.Empty;
            openException = null;

            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                openException = ex;
                errorMessage = ex.Message;
                return false;
            }
        }

        private static bool TryBuildSslDisabledFallback(
            string connectionString,
            Exception? openException,
            out string fallbackConnectionString)
        {
            fallbackConnectionString = connectionString;
            if (!IsSslHandshakeFailure(openException))
            {
                return false;
            }

            try
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                if (builder.SslMode != MySqlSslMode.Preferred)
                {
                    return false;
                }

                builder.SslMode = MySqlSslMode.Disabled;
                fallbackConnectionString = NormalizeConnectionString(builder.ConnectionString);
                return !string.Equals(fallbackConnectionString, connectionString, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSslHandshakeFailure(Exception? exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is AuthenticationException)
                {
                    return true;
                }

                string message = current.Message ?? string.Empty;
                if (message.IndexOf("SSL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("TLS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("security package", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("certificate", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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
            return TryResolveWorkingConnectionString(connectionString, out _, out errorMessage);
        }

        public static bool TryGetWorkingConnectionString(
            string connectionString,
            out string workingConnectionString,
            out string errorMessage)
        {
            return TryResolveWorkingConnectionString(connectionString, out workingConnectionString, out errorMessage);
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
