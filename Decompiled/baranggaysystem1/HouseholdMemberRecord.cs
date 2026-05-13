namespace baranggaysystem1;

internal sealed class HouseholdMemberRecord
{
	public int ResidentId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public int? Age { get; set; }

	public string Sex { get; set; } = string.Empty;

	public string CivilStatus { get; set; } = string.Empty;

	public string ContactNo { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public bool HasPhoto { get; set; }
}
