namespace baranggaysystem1.Database;

internal sealed class DatabaseConnectionProfile
{
	public string Server { get; set; } = "localhost";

	public uint Port { get; set; } = 3306u;

	public string Database { get; set; } = "barangay_system";

	public string Username { get; set; } = "root";

	public string Password { get; set; } = string.Empty;

	public bool UseSsl { get; set; }

	public static DatabaseConnectionProfile CreateDefault()
	{
		return new DatabaseConnectionProfile();
	}
}
