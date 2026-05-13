namespace baranggaysystem1.ViewModels;

public sealed class HouseholdResidentCandidate
{
	public int ResidentId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string ContactNo { get; set; } = string.Empty;

	public string CurrentAddress { get; set; } = string.Empty;

	public int? CurrentHouseholdId { get; set; }

	public int? CurrentPurokId { get; set; }

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
