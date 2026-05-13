using System;

namespace baranggaysystem1.Services;

internal sealed class CertificateRequestDraft
{
	public int? RequestId { get; set; }

	public int ResidentId { get; set; }

	public string ResidentName { get; set; } = string.Empty;

	public int DocTypeId { get; set; }

	public string DocumentTypeName { get; set; } = string.Empty;

	public string DocumentTypeCode { get; set; } = string.Empty;

	public int? ValidityDays { get; set; }

	public string Purpose { get; set; } = string.Empty;

	public decimal Fee { get; set; }

	public string OrNumber { get; set; } = string.Empty;

	public string BusinessName { get; set; } = string.Empty;

	public string BusinessNature { get; set; } = string.Empty;

	public string Status { get; set; } = "SUBMITTED";

	public DateTime IssuedDate { get; set; } = DateTime.Now;
}
