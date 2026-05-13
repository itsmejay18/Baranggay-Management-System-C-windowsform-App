namespace baranggaysystem1;

internal sealed class ReportsFilters
{
	public int? PurokId { get; set; }

	public CertificateStatusFilter CertificateStatus { get; set; }

	public BlotterStatusFilter BlotterStatus { get; set; }
}
