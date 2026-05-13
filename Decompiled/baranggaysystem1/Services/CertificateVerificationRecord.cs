using System;

namespace baranggaysystem1.Services;

internal sealed class CertificateVerificationRecord
{
	public int RequestId { get; set; }

	public int ResidentId { get; set; }

	public string TrackingCode { get; set; } = string.Empty;

	public string DocumentNo { get; set; } = string.Empty;

	public string VerificationToken { get; set; } = string.Empty;

	public string ResidentName { get; set; } = string.Empty;

	public string DocumentTypeName { get; set; } = string.Empty;

	public string Purpose { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public DateTime? RequestedAt { get; set; }

	public DateTime? ReleasedAt { get; set; }

	public DateTime? VerificationTokenCreatedAt { get; set; }

	public DateTime? ExpiresAt { get; set; }

	public decimal Fee { get; set; }

	public string OrNumber { get; set; } = string.Empty;

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
