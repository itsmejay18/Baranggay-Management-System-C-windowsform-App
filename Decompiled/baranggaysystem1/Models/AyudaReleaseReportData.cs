using System;
using System.Collections.Generic;

namespace baranggaysystem1.Models;

public sealed class AyudaReleaseReportData
{
	public int BatchId { get; set; }

	public string BatchReference { get; set; } = string.Empty;

	public string ProgramName { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public DateTime ReleaseDate { get; set; }

	public decimal TotalAmount { get; set; }

	public int BeneficiaryCount { get; set; }

	public string Notes { get; set; } = string.Empty;

	public string GeneratedBy { get; set; } = string.Empty;

	public string ReportFilePath { get; set; } = string.Empty;

	public List<AyudaReleaseReportBeneficiaryRow> Beneficiaries { get; } = new List<AyudaReleaseReportBeneficiaryRow>();
}
