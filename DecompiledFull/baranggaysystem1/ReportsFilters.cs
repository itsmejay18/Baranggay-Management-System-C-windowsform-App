namespace baranggaysystem1;

internal sealed class ReportsFilters
{
	public int? PurokId { get; init; }

	public CertificateStatusFilter CertificateStatus { get; init; }

	public BlotterStatusFilter BlotterStatus { get; init; }
}
