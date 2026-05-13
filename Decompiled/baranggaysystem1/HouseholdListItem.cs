using System;

namespace baranggaysystem1;

internal sealed class HouseholdListItem
{
	public int HouseholdId { get; set; }

	public string HouseNo { get; set; } = string.Empty;

	public string Street { get; set; } = string.Empty;

	public string Subdivision { get; set; } = string.Empty;

	public int PurokId { get; set; }

	public string PurokName { get; set; } = string.Empty;

	public int MemberCount { get; set; }

	public int SeniorCount { get; set; }

	public int PwdCount { get; set; }

	public int FourPsCount { get; set; }

	public int VoterCount { get; set; }

	public int ActiveCaseCount { get; set; }

	public DateTime? UpdatedAt { get; set; }
}
