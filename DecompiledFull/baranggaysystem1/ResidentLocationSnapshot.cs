namespace baranggaysystem1;

internal sealed class ResidentLocationSnapshot
{
	public int? PurokId { get; init; }

	public int? HouseholdId { get; init; }

	public string AddressLabel { get; init; } = string.Empty;
}
