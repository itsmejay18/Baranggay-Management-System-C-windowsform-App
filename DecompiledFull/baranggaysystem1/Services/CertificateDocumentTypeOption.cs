namespace baranggaysystem1.Services;

public sealed class CertificateDocumentTypeOption
{
	public int DocTypeId { get; init; }

	public string Name { get; init; } = string.Empty;

	public string Code { get; init; } = string.Empty;

	public decimal DefaultFee { get; init; }

	public int? ValidityDays { get; init; }

	public override string ToString()
	{
		return Name;
	}
}
