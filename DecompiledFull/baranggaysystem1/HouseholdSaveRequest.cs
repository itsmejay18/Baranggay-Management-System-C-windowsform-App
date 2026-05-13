namespace baranggaysystem1;

internal sealed class HouseholdSaveRequest
{
	public int BarangayId { get; init; }

	public int PurokId { get; init; }

	public string HouseNo { get; init; } = string.Empty;

	public string Street { get; init; } = string.Empty;

	public string Subdivision { get; init; } = string.Empty;

	public string AddressNote { get; init; } = string.Empty;

	public decimal? Latitude { get; init; }

	public decimal? Longitude { get; init; }
}
