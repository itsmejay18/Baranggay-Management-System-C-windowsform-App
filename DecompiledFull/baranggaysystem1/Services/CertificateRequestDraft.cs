using System;

namespace baranggaysystem1.Services;

internal sealed class CertificateRequestDraft
{
	public int? RequestId { get; init; }

	public int ResidentId { get; init; }

	public string ResidentName { get; init; } = string.Empty;

	public int DocTypeId { get; init; }

	public string DocumentTypeName { get; init; } = string.Empty;

	public string DocumentTypeCode { get; init; } = string.Empty;

	public int? ValidityDays { get; init; }

	public string Purpose { get; init; } = string.Empty;

	public decimal Fee { get; init; }

	public string OrNumber { get; init; } = string.Empty;

	public string BusinessName { get; init; } = string.Empty;

	public string BusinessNature { get; init; } = string.Empty;

	public string Status { get; init; } = "SUBMITTED";

	public DateTime IssuedDate { get; init; } = DateTime.Now;
}
