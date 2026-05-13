using System;

namespace baranggaysystem1;

internal sealed class HouseholdTransferHistoryItem
{
	public long TransferId { get; set; }

	public int ResidentId { get; set; }

	public string ResidentName { get; set; } = string.Empty;

	public string OldAddress { get; set; } = string.Empty;

	public string NewAddress { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;

	public string TransferredBy { get; set; } = string.Empty;

	public DateTime? TransferredAt { get; set; }
}
