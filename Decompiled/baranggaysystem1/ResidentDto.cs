using System;

namespace baranggaysystem1;

public class ResidentDto
{
	public int? Id { get; set; }

	public string FirstName { get; set; } = string.Empty;

	public string MiddleName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public string Suffix { get; set; } = string.Empty;

	public string Gender { get; set; } = string.Empty;

	public DateTime DateOfBirth { get; set; } = DateTime.Today;

	public string CivilStatus { get; set; } = string.Empty;

	public string ContactNo { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	// Registries
	public bool IsPwd { get; set; }

	public bool IsSenior { get; set; }

	public bool Is4PsBeneficiary { get; set; }

	public bool IsRegisteredVoter { get; set; }

	public bool IsSoloParent { get; set; }

	public bool IsYouth { get; set; }

	public bool IsIndigent { get; set; }

	// Photo
	public byte[]? PhotoBytes { get; set; }

	public string? PhotoPath { get; set; }

	// Address & Household
	public int? BarangayId { get; set; }

	public int? PurokId { get; set; }

	public int? HouseholdId { get; set; }

	public string HouseNo { get; set; } = string.Empty;

	public string Street { get; set; } = string.Empty;

	// Additional Personal Info
	public string Occupation { get; set; } = string.Empty;

	public string EducationalAttainment { get; set; } = string.Empty;

	public string Nationality { get; set; } = "Filipino";

	public string Religion { get; set; } = string.Empty;

	public string BloodType { get; set; } = string.Empty;

	public string EmailAddress { get; set; } = string.Empty;

	// Government IDs
	public string PhilHealthNo { get; set; } = string.Empty;

	public string SssNo { get; set; } = string.Empty;

	public string TinNo { get; set; } = string.Empty;

	public string VotersIdNo { get; set; } = string.Empty;

	// Residency
	public DateTime? DateOfResidency { get; set; }

	public string PlaceOfBirth { get; set; } = string.Empty;

	public string HouseholdRelationship { get; set; } = string.Empty;
}
