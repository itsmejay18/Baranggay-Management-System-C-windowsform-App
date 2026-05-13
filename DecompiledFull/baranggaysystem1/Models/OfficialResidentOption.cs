namespace baranggaysystem1.Models;

public sealed class OfficialResidentOption
{
	public int ResidentId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string ContactNo { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string Occupation { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string PhotoUrl { get; set; } = string.Empty;

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(ContactNo))
			{
				return FullName + " | " + ContactNo;
			}
			return FullName;
		}
	}

	public override string ToString()
	{
		return DisplayName;
	}
}
