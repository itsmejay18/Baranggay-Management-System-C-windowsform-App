using System;

namespace baranggaysystem1.Models;

public sealed class AyudaReleaseRecord
{
	public int ReleaseId { get; set; }

	public int BatchId { get; set; }

	public string BatchReference { get; set; } = string.Empty;

	public string ReportFilePath { get; set; } = string.Empty;

	public int BatchBeneficiaryCount { get; set; }

	public int ProgramId { get; set; }

	public string ProgramName { get; set; } = string.Empty;

	public int ResidentId { get; set; }

	public string ResidentName { get; set; } = string.Empty;

	public string ResidentContactNo { get; set; } = string.Empty;

	public string ReferenceNo { get; set; } = string.Empty;

	public decimal Amount { get; set; }

	public DateTime ReleasedAt { get; set; }

	public string ReleaseStatus { get; set; } = "RELEASED";

	public string Notes { get; set; } = string.Empty;
}
