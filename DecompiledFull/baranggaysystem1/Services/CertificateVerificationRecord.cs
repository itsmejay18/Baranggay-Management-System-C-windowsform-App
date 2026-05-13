using System;

namespace baranggaysystem1.Services;

internal sealed class CertificateVerificationRecord
{
	public int RequestId { get; init; }

	public int ResidentId { get; init; }

	public string TrackingCode { get; init; } = string.Empty;

	public string DocumentNo { get; init; } = string.Empty;

	public string VerificationToken { get; init; } = string.Empty;

	public string ResidentName { get; init; } = string.Empty;

	public string DocumentTypeName { get; init; } = string.Empty;

	public string Purpose { get; init; } = string.Empty;

	public string Status { get; init; } = string.Empty;

	public DateTime? RequestedAt { get; init; }

	public DateTime? ReleasedAt { get; init; }

	public DateTime? VerificationTokenCreatedAt { get; init; }

	public DateTime? ExpiresAt { get; init; }

	public decimal Fee { get; init; }

	public string OrNumber { get; init; } = string.Empty;

	public bool HasVerificationToken => !string.IsNullOrWhiteSpace(VerificationToken);

	public bool IsReleased => string.Equals(Status, "RELEASED", StringComparison.OrdinalIgnoreCase);

	public bool IsExpired
	{
		get
		{
			if (ExpiresAt.HasValue)
			{
				return ExpiresAt.Value.Date < DateTime.Today;
			}
			return false;
		}
	}

	public string VerificationState
	{
		get
		{
			if (IsReleased)
			{
				if (!IsExpired)
				{
					if (!HasVerificationToken)
					{
						return "Released Without Token";
					}
					return "Valid";
				}
				return "Expired";
			}
			return "Pending Release";
		}
	}

	public string VerificationStateSummary => VerificationState switch
	{
		"Valid" => "The released document is currently valid for verification.", 
		"Expired" => "The document was released, but its recorded validity period has already expired.", 
		"Pending Release" => "The request exists, but the document has not been released yet.", 
		"Released Without Token" => "The document was released, but no verification token is stored on record.", 
		_ => "Verification details are available for review.", 
	};

	public string VerificationPayload
	{
		get
		{
			if (!HasVerificationToken)
			{
				return "BMS-VERIFY|document=" + DocumentNo + "|tracking=" + TrackingCode;
			}
			return $"BMS-VERIFY|token={VerificationToken}|document={DocumentNo}|tracking={TrackingCode}";
		}
	}
}
