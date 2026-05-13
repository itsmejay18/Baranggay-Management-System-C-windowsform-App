namespace baranggaysystem1.Database;

internal sealed class DatabaseConnectionProfile
{
    public string Server { get; set; } = "localhost";
    public uint Port { get; set; } = 3306;
    public string Database { get; set; } = "barangay_system";
    public string Username { get; set; } = "root";
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = false;

    public static DatabaseConnectionProfile CreateDefault()
    {
        return new DatabaseConnectionProfile();
    }
}
