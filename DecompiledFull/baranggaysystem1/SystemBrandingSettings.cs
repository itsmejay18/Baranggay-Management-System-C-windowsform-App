namespace baranggaysystem1;

public sealed class SystemBrandingSettings
{
	public const string DefaultSystemName = "Barangay Management System";

	public const string DefaultBarangayName = "Barangay San Jose";

	public const string DefaultMunicipality = "Municipality";

	public const string DefaultProvince = "Province";

	public const string DefaultRegion = "Region";

	public string SystemName { get; set; } = "Barangay Management System";

	public string BarangayName { get; set; } = "Barangay San Jose";

	public string Municipality { get; set; } = "Municipality";

	public string Province { get; set; } = "Province";

	public string Region { get; set; } = "Region";

	public static SystemBrandingSettings CreateDefault()
	{
		return new SystemBrandingSettings();
	}
}
