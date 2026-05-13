using System.Collections.Generic;

namespace baranggaysystem1.helper;

internal static class WorkflowRules
{
	private static readonly HashSet<(string From, string To)> CertificateTransitions = new HashSet<(string, string)>
	{
		("DRAFT", "SUBMITTED"),
		("DRAFT", "CANCELLED"),
		("DRAFT", "REJECTED"),
		("SUBMITTED", "APPROVED"),
		("SUBMITTED", "CANCELLED"),
		("SUBMITTED", "REJECTED"),
		("APPROVED", "RELEASED"),
		("APPROVED", "CANCELLED"),
		("REJECTED", "SUBMITTED")
	};

	private static readonly HashSet<(string From, string To)> BlotterTransitions = new HashSet<(string, string)>
	{
		("ONGOING", "ONGOING"),
		("ONGOING", "SETTLED"),
		("ONGOING", "REFERRED"),
		("SETTLED", "SETTLED"),
		("SETTLED", "CLOSED"),
		("REFERRED", "REFERRED"),
		("REFERRED", "CLOSED"),
		("CLOSED", "CLOSED")
	};

	internal static string NormalizeCertificateStatus(string? status)
	{
		if (string.IsNullOrWhiteSpace(status))
		{
			return "SUBMITTED";
		}
		string text = status.Trim().ToUpperInvariant();
		if (!(text == "REQUESTED"))
		{
			if (text == "ISSUED")
			{
				return "RELEASED";
			}
			return status.Trim().ToUpperInvariant();
		}
		return "SUBMITTED";
	}

	internal static string NormalizeBlotterStatus(string? status)
	{
		if (string.IsNullOrWhiteSpace(status))
		{
			return "ONGOING";
		}
		if (status.Trim().ToUpperInvariant() == "OPEN")
		{
			return "ONGOING";
		}
		return status.Trim().ToUpperInvariant();
	}

	internal static bool TryValidateCertificateTransition(string? fromStatus, string? toStatus, out string message)
	{
		string text = NormalizeCertificateStatus(fromStatus);
		string text2 = NormalizeCertificateStatus(toStatus);
		if (text == text2)
		{
			message = string.Empty;
			return true;
		}
		if (CertificateTransitions.Contains((text, text2)))
		{
			message = string.Empty;
			return true;
		}
		message = $"Invalid certificate transition: {text} -> {text2}.";
		return false;
	}

	internal static bool TryValidateBlotterTransition(string? fromStatus, string? toStatus, out string message)
	{
		string text = NormalizeBlotterStatus(fromStatus);
		string text2 = NormalizeBlotterStatus(toStatus);
		if (BlotterTransitions.Contains((text, text2)))
		{
			message = string.Empty;
			return true;
		}
		message = $"Invalid blotter transition: {text} -> {text2}.";
		return false;
	}

	internal static bool TryValidateNewBlotterStatus(string? status, out string message)
	{
		if (NormalizeBlotterStatus(status) == "ONGOING")
		{
			message = string.Empty;
			return true;
		}
		message = "New blotter records must start with Ongoing status.";
		return false;
	}
}
