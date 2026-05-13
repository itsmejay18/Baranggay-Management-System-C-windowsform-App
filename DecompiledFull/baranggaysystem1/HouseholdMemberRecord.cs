namespace baranggaysystem1;

internal sealed class HouseholdMemberRecord
{
	public int ResidentId { get; init; }

	public string FullName { get; init; } = string.Empty;

	public int? Age { get; init; }

	public string Sex { get; init; } = string.Empty;

	public string CivilStatus { get; init; } = string.Empty;

	public string ContactNo { get; init; } = string.Empty;

	public string Status { get; init; } = string.Empty;

	public bool HasPhoto { get; init; }
}
