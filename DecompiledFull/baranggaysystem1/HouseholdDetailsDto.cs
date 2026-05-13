using System;

namespace baranggaysystem1;

internal sealed class HouseholdDetailsDto
{
	public int HouseholdId { get; init; }

	public int BarangayId { get; init; }

	public int PurokId { get; init; }

	public string PurokName { get; init; } = string.Empty;

	public string HouseNo { get; init; } = string.Empty;

	public string Street { get; init; } = string.Empty;

	public string Subdivision { get; init; } = string.Empty;

	public string AddressNote { get; init; } = string.Empty;

	public decimal? Latitude { get; init; }

	public decimal? Longitude { get; init; }

	public int MemberCount { get; init; }

	public int SeniorCount { get; init; }

	public int PwdCount { get; init; }

	public int FourPsCount { get; init; }

	public int VoterCount { get; init; }

	public int ActiveCaseCount { get; init; }

	public DateTime? UpdatedAt { get; init; }

	public string FullAddress => HouseholdRepository.BuildAddressLabel(HouseNo, Street, Subdivision, PurokName);
}
