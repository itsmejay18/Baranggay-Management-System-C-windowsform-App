using System;

namespace baranggaysystem1.Models;

public sealed class OfficialTermOption
{
	public int TermId { get; set; }

	public DateTime? TermStart { get; set; }

	public DateTime? TermEnd { get; set; }

	public string Notes { get; set; } = string.Empty;

	public bool IsCurrent { get; set; }

	public bool IsCreateNewOption { get; set; }

	public string DisplayName { get; set; } = string.Empty;

	public override string ToString()
	{
		return DisplayName;
	}
}
