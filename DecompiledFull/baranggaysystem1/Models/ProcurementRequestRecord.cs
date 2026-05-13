using System;

namespace baranggaysystem1.Models;

public sealed class ProcurementRequestRecord
{
	public int ProcurementId { get; set; }

	public string RequestType { get; set; } = "PROCUREMENT";

	public DateTime RequestDate { get; set; } = DateTime.Today;

	public DateTime? NeededByDate { get; set; }

	public string RequestTitle { get; set; } = string.Empty;

	public string ProcurementCategory { get; set; } = string.Empty;

	public string VendorName { get; set; } = string.Empty;

	public string RequestedByName { get; set; } = string.Empty;

	public decimal TotalAmount { get; set; }

	public string WorkflowStatus { get; set; } = "DRAFT";

	public string PurchaseOrderNo { get; set; } = string.Empty;

	public string ApprovedByName { get; set; } = string.Empty;

	public DateTime? ApprovedAt { get; set; }

	public string ItemSummary { get; set; } = string.Empty;

	public string ApprovalNotes { get; set; } = string.Empty;

	public string Notes { get; set; } = string.Empty;
}
