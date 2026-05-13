namespace baranggaysystem1.Services;

public sealed class CertificateDocumentTypeOption
{
	public int DocTypeId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Code { get; set; } = string.Empty;

	public decimal DefaultFee { get; set; }

	public int? ValidityDays { get; set; }

	public override string ToString()
	{
		return Name;
	}
}
