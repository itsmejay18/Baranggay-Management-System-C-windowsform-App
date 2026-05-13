namespace baranggaysystem1.Database;

internal sealed class StoredDatabaseConnectionSettings
{
	public string SelectedProfileKey { get; set; } = "localhost";

	public DatabaseConnectionProfile CustomProfile { get; set; } = DatabaseConnectionProfile.CreateDefault();
}
