using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using MySql.Data.MySqlClient;

const string defaultConnectionString = "server=srv1237.hstgr.io;port=3306;database=u621755393_CBaranggayMana;user id=u621755393_cbaranggay;password=Dssc@2026;SslMode=Preferred;AllowPublicKeyRetrieval=true;AllowUserVariables=true;ConnectionTimeout=5";

if (args.Length > 0 && string.Equals(args[0], "--app-current", StringComparison.OrdinalIgnoreCase))
{
    RunAppCurrentConnectionProbe();
    return;
}

string connectionString = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("DB_PROBE_CONNECTION_STRING") ?? defaultConnectionString;

try
{
    var stopwatch = Stopwatch.StartNew();
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    stopwatch.Stop();

    await using var command = connection.CreateCommand();
    command.CommandText = "select database(), current_user(), @@hostname";

    await using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        Console.WriteLine($"OPEN_OK {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"DATABASE={reader.GetValue(0)}");
        Console.WriteLine($"CURRENT_USER={reader.GetValue(1)}");
        Console.WriteLine($"HOSTNAME={reader.GetValue(2)}");
    }
    else
    {
        Console.WriteLine($"OPEN_OK {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("QUERY_RETURNED_NO_ROWS");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"OPEN_FAIL {ex.GetType().FullName}");
    Console.WriteLine(ex.Message);

    if (ex.InnerException is not null)
    {
        Console.WriteLine($"INNER_FAIL {ex.InnerException.GetType().FullName}");
        Console.WriteLine(ex.InnerException.Message);
    }

    Environment.ExitCode = 1;
}

static void RunAppCurrentConnectionProbe()
{
    string appAssemblyPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "bin", "Debug", "net8.0-windows", "baranggaysystem1.dll"));
    string appAssemblyDirectory = Path.GetDirectoryName(appAssemblyPath)
        ?? throw new InvalidOperationException("Unable to resolve app assembly directory.");

    AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
    {
        string dependencyPath = Path.Combine(appAssemblyDirectory, $"{assemblyName.Name}.dll");
        return File.Exists(dependencyPath)
            ? AssemblyLoadContext.Default.LoadFromAssemblyPath(dependencyPath)
            : null;
    };

    Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(appAssemblyPath);
    Type connectionType = assembly.GetType("baranggaysystem1.Database.DBConnection", throwOnError: true)
        ?? throw new InvalidOperationException("DBConnection type not found.");

    MethodInfo getCurrentConnectionString = connectionType.GetMethod(
        "GetCurrentConnectionString",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("GetCurrentConnectionString method not found.");

    MethodInfo tryOpenCurrent = connectionType.GetMethod(
        "TryOpenCurrent",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("TryOpenCurrent method not found.");

    string resolvedConnectionString = (string)(getCurrentConnectionString.Invoke(null, null)
        ?? throw new InvalidOperationException("Resolved connection string was null."));

    object?[] parameters = { string.Empty };
    bool opened = (bool)(tryOpenCurrent.Invoke(null, parameters)
        ?? throw new InvalidOperationException("TryOpenCurrent returned null."));

    Console.WriteLine($"APP_CONNECTION={resolvedConnectionString}");
    if (opened)
    {
        Console.WriteLine("APP_OPEN_OK");
        return;
    }

    Console.WriteLine("APP_OPEN_FAIL");
    Console.WriteLine(parameters[0]?.ToString() ?? "Unknown error");
    Environment.ExitCode = 1;
}
