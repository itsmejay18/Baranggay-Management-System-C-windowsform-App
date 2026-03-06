using System;

namespace baranggaysystem1;

public sealed class BlotterDto
{
    public int ComplainantId { get; set; }

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

    public int RecordedBy { get; set; }
}
