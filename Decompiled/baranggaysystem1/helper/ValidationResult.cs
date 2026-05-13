namespace baranggaysystem1.helper;

internal readonly struct ValidationResult
{
	internal bool IsValid { get; }

	internal string Message { get; }

	internal string Title { get; }

	internal ValidationResult(bool isValid, string message = "", string title = "Validation")
	{
		IsValid = isValid;
		Message = message;
		Title = title;
	}

	internal static ValidationResult Success()
	{
		return new ValidationResult(isValid: true);
	}

	internal static ValidationResult Fail(string message, string title = "Validation")
	{
		return new ValidationResult(isValid: false, message, title);
	}
}
