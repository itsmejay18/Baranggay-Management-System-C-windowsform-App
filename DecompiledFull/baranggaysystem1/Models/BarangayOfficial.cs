using System;

namespace baranggaysystem1.Models;

public class BarangayOfficial
{
	public int OfficialId { get; set; }

	public int TermId { get; set; }

	public int ResidentId { get; set; }

	public string Position { get; set; } = string.Empty;

	public string Committee { get; set; } = string.Empty;

	public string Status { get; set; } = "ACTIVE";

	public string ResidentFirstName { get; set; } = string.Empty;

	public string ResidentMiddleName { get; set; } = string.Empty;

	public string ResidentLastName { get; set; } = string.Empty;

	public string ResidentSuffix { get; set; } = string.Empty;

	public string FullName { get; set; } = string.Empty;

	public string ContactNo { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string Occupation { get; set; } = string.Empty;

	public string PhotoUrl { get; set; } = string.Empty;

	public string ResidentStatus { get; set; } = string.Empty;

	public DateTime? TermStart { get; set; }

	public DateTime? TermEnd { get; set; }

	public string TermNotes { get; set; } = string.Empty;

	public string TermDisplay { get; set; } = string.Empty;

	public bool CreateNewTerm { get; set; }
}
