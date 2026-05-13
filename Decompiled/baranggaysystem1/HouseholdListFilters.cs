namespace baranggaysystem1;

internal sealed class HouseholdListFilters
{
	public int BarangayId { get; set; }

	public string SearchText { get; set; } = string.Empty;

	public int? PurokId { get; set; }

	public bool WithSeniors { get; set; }

	public bool WithPwd { get; set; }

	public bool With4Ps { get; set; }

	public bool EmptyHouseholdOnly { get; set; }

	public bool HasActiveCasesOnly { get; set; }

	public int PageNumber { get; set; } = 1;

	public int PageSize { get; set; } = 25;
}
