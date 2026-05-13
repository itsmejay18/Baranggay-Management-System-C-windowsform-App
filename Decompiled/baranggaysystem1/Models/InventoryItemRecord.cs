using System;

namespace baranggaysystem1.Models;

public sealed class InventoryItemRecord
{
	public int ItemId { get; set; }

	public string ItemName { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string Unit { get; set; } = "pcs";

	public decimal QuantityOnHand { get; set; }

	public decimal ReorderLevel { get; set; }

	public decimal UnitCost { get; set; }

	public string Location { get; set; } = string.Empty;

	public string ItemStatus { get; set; } = "ACTIVE";

	public DateTime? LastRestockedAt { get; set; }

	public string Notes { get; set; } = string.Empty;
}
