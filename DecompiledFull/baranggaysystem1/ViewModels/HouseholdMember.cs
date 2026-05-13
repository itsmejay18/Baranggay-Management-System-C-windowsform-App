namespace baranggaysystem1.ViewModels;

public class HouseholdMember
{
	public int ResidentId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public int Age { get; set; }

	public string Gender { get; set; } = string.Empty;

	public bool IsHead { get; set; }

	public string Status { get; set; } = string.Empty;

	public bool IsCurrentContext { get; set; }

	public string RoleLabel
	{
		get
		{
			if (!IsHead)
			{
				return "MEMBER";
			}
			return "HEAD OF FAMILY";
		}
	}

	public string GenderIcon
	{
		get
		{
			if (!(Gender == "Male"))
			{
				return "Venus";
			}
			return "Mars";
		}
	}
}
