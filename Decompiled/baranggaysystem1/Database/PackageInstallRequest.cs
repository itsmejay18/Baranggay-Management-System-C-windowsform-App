namespace baranggaysystem1.Database;

internal sealed class PackageInstallRequest
{
	public DatabaseConnectionProfile ConnectionProfile { get; set; } = DatabaseConnectionProfile.CreateDefault();

	public string SuperAdminUsername { get; set; } = string.Empty;

	public string SuperAdminPassword { get; set; } = string.Empty;

	public string UserUsername { get; set; } = string.Empty;

	public string UserPassword { get; set; } = string.Empty;
}
