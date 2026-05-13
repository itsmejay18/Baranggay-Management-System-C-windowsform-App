using System;

namespace baranggaysystem1;

internal sealed class HouseholdTransferHistoryItem
{
	public long TransferId { get; init; }

	public int ResidentId { get; init; }

	public string ResidentName { get; init; } = string.Empty;

	public string OldAddress { get; init; } = string.Empty;

	public string NewAddress { get; init; } = string.Empty;

	public string Reason { get; init; } = string.Empty;

	public string TransferredBy { get; init; } = string.Empty;

	public DateTime? TransferredAt { get; init; }
}
