namespace baranggaysystem1;

internal sealed class HouseholdSaveRequest
{
	public int BarangayId { get; set; }

	public int PurokId { get; set; }

	public string HouseNo { get; set; } = string.Empty;

	public string Street { get; set; } = string.Empty;

	public string Subdivision { get; set; } = string.Empty;

	public string AddressNote { get; set; } = string.Empty;

	public decimal? Latitude { get; set; }

	public decimal? Longitude { get; set; }
}
