using System;

namespace baranggaysystem1.Models;

public sealed class AyudaProgramRecord
{
	public int ProgramId { get; set; }

	public string ProgramName { get; set; } = string.Empty;

	public string Category { get; set; } = "Financial Assistance";

	public decimal AllocatedBudget { get; set; }

	public decimal SpentBudget { get; set; }

	public decimal RemainingBudget { get; set; }

	public string Status { get; set; } = "ACTIVE";

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public string Notes { get; set; } = string.Empty;
}
