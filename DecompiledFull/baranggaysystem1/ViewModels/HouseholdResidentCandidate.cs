namespace baranggaysystem1.ViewModels;

public sealed class HouseholdResidentCandidate
{
	public int ResidentId { get; init; }

	public string FullName { get; init; } = string.Empty;

	public string ContactNo { get; init; } = string.Empty;

	public string CurrentAddress { get; init; } = string.Empty;

	public int? CurrentHouseholdId { get; init; }

	public int? CurrentPurokId { get; init; }

	public string HouseholdLabel
	{
		get
		{
			if (!CurrentHouseholdId.HasValue)
			{
				return "Not assigned to a household";
			}
			return $"Currently in Household #{CurrentHouseholdId.Value}";
		}
	}

	public string AddressLabel
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(CurrentAddress))
			{
				return CurrentAddress;
			}
			return "No saved address";
		}
	}

	public bool IsTransfer => CurrentHouseholdId.HasValue;
}
