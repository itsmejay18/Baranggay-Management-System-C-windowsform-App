using System;

namespace baranggaysystem1.Models;

public sealed class ExpenseEntryRecord
{
	public int ExpenseId { get; set; }

	public DateTime ExpenseDate { get; set; } = DateTime.Today;

	public string ExpenseCategory { get; set; } = string.Empty;

	public string ExpenseTitle { get; set; } = string.Empty;

	public string PayeeName { get; set; } = string.Empty;

	public decimal Amount { get; set; }

	public string PaymentMethod { get; set; } = "Cash";

	public string Status { get; set; } = "POSTED";

	public string ReferenceNo { get; set; } = string.Empty;

	public string Notes { get; set; } = string.Empty;
}
