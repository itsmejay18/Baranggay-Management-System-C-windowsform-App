namespace baranggaysystem1.Database;

internal sealed class DatabaseConnectionProfile
{
    public string Mode { get; set; } = "Network";
    public string Server { get; set; } = "srv1237.hstgr.io";
    public uint Port { get; set; } = 3306;
    public string Database { get; set; } = "u621755393_CBaranggayMana";
    public string Username { get; set; } = "u621755393_cbaranggay";
    public string Password { get; set; } = "Dssc@2026";
    public bool UseSsl { get; set; } = false;

    public static DatabaseConnectionProfile CreateDefault()
    {
        return new DatabaseConnectionProfile();
    }
}
