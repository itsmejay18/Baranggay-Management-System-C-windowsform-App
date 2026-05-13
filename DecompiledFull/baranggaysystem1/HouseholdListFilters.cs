namespace baranggaysystem1;

internal sealed class HouseholdListFilters
{
	public int BarangayId { get; init; }

	public string SearchText { get; init; } = string.Empty;

	public int? PurokId { get; init; }

	public bool WithSeniors { get; init; }

	public bool WithPwd { get; init; }

	public bool With4Ps { get; init; }

	public bool EmptyHouseholdOnly { get; init; }

	public bool HasActiveCasesOnly { get; init; }

	public int PageNumber { get; init; } = 1;

	public int PageSize { get; init; } = 25;
}
