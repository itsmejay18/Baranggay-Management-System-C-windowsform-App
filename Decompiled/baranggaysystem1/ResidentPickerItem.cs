namespace baranggaysystem1;

internal sealed class ResidentPickerItem
{
	public int ResidentId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string ContactNo { get; set; } = string.Empty;

	public string CurrentAddress { get; set; } = string.Empty;

	public int? CurrentHouseholdId { get; set; }

	public int? CurrentPurokId { get; set; }
}
