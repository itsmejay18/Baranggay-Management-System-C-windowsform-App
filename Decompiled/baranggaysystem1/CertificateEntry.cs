using System;

namespace baranggaysystem1;

public sealed class CertificateEntry
{
	public string? Type { get; set; }

	public string? Purpose { get; set; }

	public decimal Fee { get; set; }

	public string? OrNumber { get; set; }

	public string? PaymentMethod { get; set; }

	public DateTime? IssuedDate { get; set; }

	public string? BusinessName { get; set; }

	public string? BusinessNature { get; set; }

	public string? Remarks { get; set; }
}
