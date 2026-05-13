namespace baranggaysystem1.Database;

internal sealed class DatabaseConnectionOption
{
	public string Key { get; set; } = "localhost";

	public string DisplayName { get; set; } = "Localhost";

	public string Description { get; set; } = string.Empty;

	public DatabaseConnectionProfile Profile { get; set; } = DatabaseConnectionProfile.CreateDefault();

	public bool UsesSqlite { get; set; }

	public string SqliteFilePath { get; set; } = string.Empty;

	public bool IsEditable => Key == "custom";
}
