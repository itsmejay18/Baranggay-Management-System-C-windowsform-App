using System;

namespace baranggaysystem1.Models;

public sealed class AssetRecord
{
	public int AssetId { get; set; }

	public string AssetName { get; set; } = string.Empty;

	public string AssetCategory { get; set; } = string.Empty;

	public string AssetTag { get; set; } = string.Empty;

	public DateTime? AcquisitionDate { get; set; }

	public decimal AcquisitionCost { get; set; }

	public string AssignedLocation { get; set; } = string.Empty;

	public string CustodianName { get; set; } = string.Empty;

	public string ConditionStatus { get; set; } = "GOOD";

	public string LifecycleStatus { get; set; } = "ACTIVE";

	public string Notes { get; set; } = string.Empty;
}
