using System;
using baranggaysystem1;

namespace baranggaysystem1.helper;

internal readonly struct ValidationResult
{
    internal ValidationResult(bool isValid, string message = "", string title = "Validation")
    {
        IsValid = isValid;
        Message = message;
        Title = title;
    }

    internal bool IsValid { get; }
    internal string Message { get; }
    internal string Title { get; }

    internal static ValidationResult Success()
    {
        return new ValidationResult(true);
    }

    internal static ValidationResult Fail(string message, string title = "Validation")
    {
        return new ValidationResult(false, message, title);
    }
}

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
        if (residentId == null || residentId <= 0)
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
        ResidentDuplicateMatch? match = ResidentDuplicateService.FindDuplicate(resident, excludeResidentId);
        if (match == null)
        {
            return ValidationResult.Success();
        }

        string duplicateMessage =
            $"Possible duplicate resident found (ID #{match.ResidentId}): {match.FullName}, " +
            $"DOB {match.BirthDate:yyyy-MM-dd}, Address {match.AddressLabel}.";
        return ValidationResult.Fail(duplicateMessage, "Duplicate resident");
    }

    internal static ValidationResult ValidateHouseholdConsistency(ResidentDto resident, int? excludeResidentId = null)
    {
        HouseholdConsistencyViolation? violation = HouseholdConsistencyService.Validate(resident, excludeResidentId);
        if (violation == null)
        {
            return ValidationResult.Success();
        }

        return ValidationResult.Fail(violation.Message, violation.Title);
    }

    internal static ValidationResult ValidateResidentSelection(int? residentId, string message)
    {
        if (residentId == null || residentId <= 0)
        {
            return ValidationResult.Fail(message, "Warning");
        }

        return ValidationResult.Success();
    }

    internal static ValidationResult ValidateCertificateSelection(int? certificateId)
    {
        if (certificateId == null || certificateId <= 0)
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

        if (!WorkflowRules.TryValidateNewBlotterStatus(blotter.Status, out string statusMessage))
        {
            return ValidationResult.Fail(statusMessage, "Invalid status");
        }

        return ValidationResult.Success();
    }

    internal static ValidationResult ValidateBlotterFormSave(
        bool residentMode,
        string? respondentName,
        string? incidentType,
        string? incidentLocation,
        string? incidentDetails,
        DateTime incidentDate,
        string? status,
        string? resolutionDetails)
    {
        if (string.IsNullOrWhiteSpace(respondentName))
        {
            string message = residentMode
                ? "Please select a resident respondent from the dropdown."
                : "Please enter respondent name.";
            return ValidationResult.Fail(message, "Missing data");
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

        string normalizedStatus = string.IsNullOrWhiteSpace(status) ? "Ongoing" : status.Trim();
        if (!WorkflowRules.TryValidateNewBlotterStatus(normalizedStatus, out string statusMessage))
        {
            return ValidationResult.Fail(statusMessage, "Invalid status");
        }

        if (!normalizedStatus.Equals("Ongoing", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(resolutionDetails))
        {
            return ValidationResult.Fail(
                "Resolution details are required when closing or referring a blotter.",
                "Missing data");
        }

        return ValidationResult.Success();
    }

    internal static ValidationResult ValidateBlotterStatusTransition(
        string originalStatus,
        string currentStatus,
        string? resolutionDetails,
        string? referralDestination)
    {
        if (string.IsNullOrWhiteSpace(currentStatus))
        {
            return ValidationResult.Fail("Select a status first.", "Update Status");
        }

        string normalized = WorkflowRules.NormalizeBlotterStatus(currentStatus);

        if (!normalized.Equals("ONGOING", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(resolutionDetails))
        {
            return ValidationResult.Fail(
                "Resolution/notes are required for Settled, Referred, or Closed status.",
                "Update Status");
        }

        if (normalized.Equals("REFERRED", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(referralDestination))
        {
            return ValidationResult.Fail(
                "Referral destination is required for Referred status.",
                "Update Status");
        }

        if (!WorkflowRules.TryValidateBlotterTransition(originalStatus, currentStatus, out string transitionMessage))
        {
            return ValidationResult.Fail(transitionMessage, "Invalid transition");
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

    internal static ValidationResult ValidateCertificateDialogSave(
        string? type,
        string? purpose,
        string? businessName,
        string? businessNature,
        decimal fee,
        string? orNumber,
        string? paymentMethod,
        DateTime issuedDate,
        CertificateDialogMode mode)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return ValidationResult.Fail("Certificate type is required.", "Warning");
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            return ValidationResult.Fail("Purpose is required.", "Warning");
        }

        bool isBusiness = type.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0;
        if (isBusiness
            && (string.IsNullOrWhiteSpace(businessName) || string.IsNullOrWhiteSpace(businessNature)))
        {
            return ValidationResult.Fail("Business name and nature are required for business clearance.", "Warning");
        }

        if (fee < 0m)
        {
            return ValidationResult.Fail("Fee cannot be negative.", "Warning");
        }

        bool needsPayment = fee > 0m || !string.IsNullOrWhiteSpace(orNumber);

        if (mode == CertificateDialogMode.Issue && needsPayment && string.IsNullOrWhiteSpace(orNumber))
        {
            return ValidationResult.Fail("OR number is required when fee is greater than 0.", "Warning");
        }

        if (mode == CertificateDialogMode.Issue && needsPayment && string.IsNullOrWhiteSpace(paymentMethod))
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
