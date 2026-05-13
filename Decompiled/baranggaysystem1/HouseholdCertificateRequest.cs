using System;

namespace baranggaysystem1;

internal sealed class HouseholdCertificateRequest
{
	public string Purpose { get; set; } = string.Empty;

	public string PresentedTo { get; set; } = string.Empty;

	public bool IncludeMemberRoster { get; set; } = true;

	public DateTime IssuedDate { get; set; } = DateTime.Today;

	public string GeneratedBy { get; set; } = string.Empty;
}
