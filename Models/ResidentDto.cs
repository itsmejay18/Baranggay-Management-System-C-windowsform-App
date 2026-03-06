using System;

namespace baranggaysystem1;

internal class ResidentDto
{
	public int? Id { get; set; }

	public string FirstName { get; set; } = string.Empty;

	public string MiddleName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public string Gender { get; set; } = string.Empty;

	public DateTime DateOfBirth { get; set; } = DateTime.Today;

	public string CivilStatus { get; set; } = string.Empty;

	public string ContactNo { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public byte[]? PhotoBytes { get; set; }

	public int? BarangayId { get; set; }

	public int? PurokId { get; set; }

	public int? HouseholdId { get; set; }
}

