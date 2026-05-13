namespace baranggaysystem1;

internal sealed class ResidentPickerItem
{
	public int ResidentId { get; init; }

	public string FullName { get; init; } = string.Empty;

	public string ContactNo { get; init; } = string.Empty;

	public string CurrentAddress { get; init; } = string.Empty;

	public int? CurrentHouseholdId { get; init; }

	public int? CurrentPurokId { get; init; }
}
