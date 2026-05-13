using System;

namespace baranggaysystem1;

internal sealed class HouseholdDetailsDto
{
	public int HouseholdId { get; set; }

	public int BarangayId { get; set; }

	public int PurokId { get; set; }

	public string PurokName { get; set; } = string.Empty;

	public string HouseNo { get; set; } = string.Empty;

	public string Street { get; set; } = string.Empty;

	public string Subdivision { get; set; } = string.Empty;

	public string AddressNote { get; set; } = string.Empty;

	public decimal? Latitude { get; set; }

	public decimal? Longitude { get; set; }

	public int MemberCount { get; set; }

	public int SeniorCount { get; set; }

	public int PwdCount { get; set; }

	public int FourPsCount { get; set; }

	public int VoterCount { get; set; }

	public int ActiveCaseCount { get; set; }

	public DateTime? UpdatedAt { get; set; }

	public string FullAddress => HouseholdRepository.BuildAddressLabel(HouseNo, Street, Subdivision, PurokName);
}
