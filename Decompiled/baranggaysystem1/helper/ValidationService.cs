using System;

namespace baranggaysystem1.helper;

internal static class ValidationService
{
	internal static ValidationResult ValidateResidentFormSave(string? firstName, string? lastName, DateTime birthDate)
	{
		if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
		{
			return ValidationResult.Fail("First name and last name are required.", "Missing data");
		}
		if (birthDate.Date > DateTime.Today)
		{
			return ValidationResult.Fail("Date of birth cannot be in the future.", "Invalid date");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateResidentUpdate(int? residentId, string? firstName, string? lastName)
	{
		if (!residentId.HasValue || residentId <= 0)
		{
			return ValidationResult.Fail("Please select a resident row first.", "Warning");
		}
		if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
		{
			return ValidationResult.Fail("First name and last name are required.", "Warning");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateResidentDuplicate(ResidentDto resident, int? excludeResidentId = null)
	{
		ResidentDuplicateMatch residentDuplicateMatch = ResidentDuplicateService.FindDuplicate(resident, excludeResidentId);
		if (residentDuplicateMatch == null)
		{
			return ValidationResult.Success();
		}
		return ValidationResult.Fail($"Possible duplicate resident found (ID #{residentDuplicateMatch.ResidentId}): {residentDuplicateMatch.FullName}, DOB {residentDuplicateMatch.BirthDate:yyyy-MM-dd}, Address {residentDuplicateMatch.AddressLabel}.", "Duplicate resident");
	}

	internal static ValidationResult ValidateHouseholdConsistency(ResidentDto resident, int? excludeResidentId = null)
	{
		HouseholdConsistencyViolation householdConsistencyViolation = HouseholdConsistencyService.Validate(resident, excludeResidentId);
		if (householdConsistencyViolation == null)
		{
			return ValidationResult.Success();
		}
		return ValidationResult.Fail(householdConsistencyViolation.Message, householdConsistencyViolation.Title);
	}

	internal static ValidationResult ValidateResidentSelection(int? residentId, string message)
	{
		if (!residentId.HasValue || residentId <= 0)
		{
			return ValidationResult.Fail(message, "Warning");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateCertificateSelection(int? certificateId)
	{
		if (!certificateId.HasValue || certificateId <= 0)
		{
			return ValidationResult.Fail("Select a certificate first.", "Warning");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateRegistration(string? username, string? password, string? role)
	{
		if (string.IsNullOrWhiteSpace(username))
		{
			return ValidationResult.Fail("Username is required.", "Missing data");
		}
		if (string.IsNullOrWhiteSpace(password))
		{
			return ValidationResult.Fail("Password is required.", "Missing data");
		}
		if (password.Length < 6)
		{
			return ValidationResult.Fail("Password must be at least 6 characters long.", "Weak password");
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		foreach (char c in password)
		{
			if (char.IsUpper(c))
			{
				flag = true;
			}
			else if (char.IsLower(c))
			{
				flag2 = true;
			}
			else if (char.IsDigit(c))
			{
				flag3 = true;
			}
		}
		if (!flag || !flag2 || !flag3)
		{
			return ValidationResult.Fail("Password must contain at least one uppercase letter, one lowercase letter, and one digit.", "Weak password");
		}
		if (string.IsNullOrWhiteSpace(role))
		{
			return ValidationResult.Fail("Please select a role.", "Warning");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateUserUpdate(string? username)
	{
		if (string.IsNullOrWhiteSpace(username))
		{
			return ValidationResult.Fail("Username is required.", "Warning");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateBlotterQuickEntry(BlotterDto blotter)
	{
		if (blotter.ComplainantId <= 0)
		{
			return ValidationResult.Fail("Select a resident before filing a blotter.", "Warning");
		}
		if (string.IsNullOrWhiteSpace(blotter.RespondentName) || string.IsNullOrWhiteSpace(blotter.IncidentType))
		{
			return ValidationResult.Fail("Respondent and incident type are required.", "Warning");
		}
		if (blotter.IncidentDate.Date > DateTime.Today)
		{
			return ValidationResult.Fail("Incident date cannot be in the future.", "Warning");
		}
		if (!WorkflowRules.TryValidateNewBlotterStatus(blotter.Status, out string message))
		{
			return ValidationResult.Fail(message, "Invalid status");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateBlotterFormSave(bool residentMode, string? respondentName, string? incidentType, string? incidentLocation, string? incidentDetails, DateTime incidentDate, string? status, string? resolutionDetails)
	{
		if (string.IsNullOrWhiteSpace(respondentName))
		{
			return ValidationResult.Fail(residentMode ? "Please select a resident respondent from the dropdown." : "Please enter respondent name.", "Missing data");
		}
		if (string.IsNullOrWhiteSpace(incidentType))
		{
			return ValidationResult.Fail("Incident type is required.", "Missing data");
		}
		if (incidentDate.Date > DateTime.Today)
		{
			return ValidationResult.Fail("Incident date cannot be in the future.", "Invalid date");
		}
		if (!string.IsNullOrWhiteSpace(incidentLocation) && incidentLocation.Trim().Length > 120)
		{
			return ValidationResult.Fail("Incident location should be 120 characters or less.", "Invalid data");
		}
		string text = (string.IsNullOrWhiteSpace(status) ? "Ongoing" : status.Trim());
		if (!WorkflowRules.TryValidateNewBlotterStatus(text, out string message))
		{
			return ValidationResult.Fail(message, "Invalid status");
		}
		if (!text.Equals("Ongoing", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(resolutionDetails))
		{
			return ValidationResult.Fail("Resolution details are required when closing or referring a blotter.", "Missing data");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateBlotterStatusTransition(string originalStatus, string currentStatus, string? resolutionDetails, string? referralDestination)
	{
		if (string.IsNullOrWhiteSpace(currentStatus))
		{
			return ValidationResult.Fail("Select a status first.", "Update Status");
		}
		string text = WorkflowRules.NormalizeBlotterStatus(currentStatus);
		if (!text.Equals("ONGOING", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(resolutionDetails))
		{
			return ValidationResult.Fail("Resolution/notes are required for Settled, Referred, or Closed status.", "Update Status");
		}
		if (text.Equals("REFERRED", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(referralDestination))
		{
			return ValidationResult.Fail("Referral destination is required for Referred status.", "Update Status");
		}
		if (!WorkflowRules.TryValidateBlotterTransition(originalStatus, currentStatus, out string message))
		{
			return ValidationResult.Fail(message, "Invalid transition");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateAnnouncementSave(string? title)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			return ValidationResult.Fail("Title is required.", "Warning");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateProjectSave(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return ValidationResult.Fail("Project name is required.", "Warning");
		}
		return ValidationResult.Success();
	}

	internal static ValidationResult ValidateCertificateDialogSave(string? type, string? purpose, string? businessName, string? businessNature, decimal fee, string? orNumber, string? paymentMethod, DateTime issuedDate, CertificateDialogMode mode)
	{
		if (string.IsNullOrWhiteSpace(type))
		{
			return ValidationResult.Fail("Certificate type is required.", "Warning");
		}
		if (string.IsNullOrWhiteSpace(purpose))
		{
			return ValidationResult.Fail("Purpose is required.", "Warning");
		}
		if (type.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0 && (string.IsNullOrWhiteSpace(businessName) || string.IsNullOrWhiteSpace(businessNature)))
		{
			return ValidationResult.Fail("Business name and nature are required for business clearance.", "Warning");
		}
		if (fee < 0m)
		{
			return ValidationResult.Fail("Fee cannot be negative.", "Warning");
		}
		bool flag = fee > 0m || !string.IsNullOrWhiteSpace(orNumber);
		if (mode == CertificateDialogMode.Issue && flag && string.IsNullOrWhiteSpace(orNumber))
		{
			return ValidationResult.Fail("OR number is required when fee is greater than 0.", "Warning");
		}
		if (mode == CertificateDialogMode.Issue && flag && string.IsNullOrWhiteSpace(paymentMethod))
		{
			return ValidationResult.Fail("Payment method is required.", "Warning");
		}
		if (mode == CertificateDialogMode.Issue && issuedDate.Date > DateTime.Today)
		{
			return ValidationResult.Fail("Issued date cannot be in the future.", "Warning");
		}
		return ValidationResult.Success();
	}
}
