using System;

namespace baranggaysystem1;

public sealed class BlotterDto
{
	public int CaseId { get; set; }

	public string CaseNo { get; set; } = string.Empty;

	public int ComplainantId { get; set; }

	public string ComplainantName { get; set; } = string.Empty;

	public string ComplainantAddress { get; set; } = string.Empty;

	public int? RespondentResidentId { get; set; }

	public string RespondentName { get; set; } = string.Empty;

	public string IncidentType { get; set; } = string.Empty;

	public DateTime IncidentDate { get; set; }

	public TimeSpan? IncidentTime { get; set; }

	public string IncidentLocation { get; set; } = string.Empty;

	public string Witnesses { get; set; } = string.Empty;

	public string ActionTaken { get; set; } = string.Empty;

	public string ResolutionDetails { get; set; } = string.Empty;

	public string IncidentDetails { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string ReferralDestination { get; set; } = string.Empty;

	public string ClosureNotes { get; set; } = string.Empty;

	public DateTime? ScheduledMediationAt { get; set; }

	public string MediationVenue { get; set; } = string.Empty;

	public string AiSummary { get; set; } = string.Empty;

	public string AiCategory { get; set; } = string.Empty;

	public string AiRiskLevel { get; set; } = string.Empty;

	public int RecordedBy { get; set; }
}
