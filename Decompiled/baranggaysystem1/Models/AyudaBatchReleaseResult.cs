namespace baranggaysystem1.Models;

public sealed class AyudaBatchReleaseResult
{
	public int BatchId { get; set; }

	public string BatchReference { get; set; } = string.Empty;

	public int BeneficiaryCount { get; set; }

	public decimal TotalAmount { get; set; }

	public string ReportFilePath { get; set; } = string.Empty;
}
