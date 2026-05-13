using System;

namespace baranggaysystem1;

internal sealed class HouseholdCertificateRequest
{
	public string Purpose { get; init; } = string.Empty;

	public string PresentedTo { get; init; } = string.Empty;

	public bool IncludeMemberRoster { get; init; } = true;

	public DateTime IssuedDate { get; init; } = DateTime.Today;

	public string GeneratedBy { get; init; } = string.Empty;
}
