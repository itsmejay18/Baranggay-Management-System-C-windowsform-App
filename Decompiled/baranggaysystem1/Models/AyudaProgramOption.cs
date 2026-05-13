namespace baranggaysystem1.Models;

public sealed class AyudaProgramOption
{
	public int ProgramId { get; set; }

	public string ProgramName { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public decimal RemainingBudget { get; set; }

	public string Status { get; set; } = string.Empty;

	public string DisplayName
	{
		get
		{
			if (!(RemainingBudget <= 0m))
			{
				return $"{ProgramName} | PHP {RemainingBudget:N2} available";
			}
			return ProgramName + " | Budget depleted";
		}
	}

	public override string ToString()
	{
		return DisplayName;
	}
}
