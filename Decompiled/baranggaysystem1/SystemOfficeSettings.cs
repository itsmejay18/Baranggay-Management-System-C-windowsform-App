namespace baranggaysystem1;

public sealed class SystemOfficeSettings
{
	public string OfficeAddress { get; set; } = string.Empty;

	public string ContactNumber { get; set; } = string.Empty;

	public string OfficialEmail { get; set; } = string.Empty;

	public static SystemOfficeSettings CreateDefault()
	{
		return new SystemOfficeSettings();
	}
}
