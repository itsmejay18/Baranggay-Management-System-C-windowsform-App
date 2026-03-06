using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1;

internal sealed class AiBlotterService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
    {
        "Domestic Dispute",
        "Noise Complaint",
        "Physical Assault",
        "Threats/Harassment",
        "Property Damage",
        "Theft",
        "Fraud/Scam",
        "Neighborhood Conflict",
        "Public Disturbance",
        "Child-Related Concern",
        "VAWC",
        "Other"
    };

    private readonly OllamaClient _ollamaClient;

    public AiBlotterService(OllamaClient? ollamaClient = null)
    {
        _ollamaClient = ollamaClient ?? new OllamaClient();
    }

    public string ModelName => _ollamaClient.Model;

    public async Task<AiBlotterAnalysis> AnalyzeBlotterAsync(int blotterId, CancellationToken cancellationToken = default)
    {
        BlotterAnalysisInput input = await LoadBlotterInputAsync(blotterId, cancellationToken).ConfigureAwait(false);
        string prompt = BuildPrompt(input);

        Exception? lastError = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                string completion = await _ollamaClient.GenerateAsync(prompt, cancellationToken).ConfigureAwait(false);
                string cleaned = JsonUtils.TrimCodeFences(completion);

                if (!JsonUtils.TryExtractFirstJsonObject(cleaned, out string jsonObject))
                {
                    throw new JsonException("AI output does not contain a valid JSON object.");
                }

                AiBlotterAnalysis analysis = JsonUtils.DeserializeStrict<AiBlotterAnalysis>(jsonObject);
                NormalizeAnalysis(analysis);
                analysis.Model = _ollamaClient.Model;
                analysis.ProcessedAt = DateTime.Now;
                return analysis;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        string reason = lastError?.Message ?? "Unknown AI parsing error.";
        return AiBlotterAnalysis.CreateFailed(reason, _ollamaClient.Model);
    }

    public async Task SaveAnalysisAsync(int blotterId, AiBlotterAnalysis analysis, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = DBConnection.GetConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"UPDATE case_record
SET ai_summary = @summary,
    ai_key_points = @key_points,
    ai_category = @category,
    ai_category_confidence = @confidence,
    ai_risk_level = @risk_level,
    ai_risk_score = @risk_score,
    ai_risk_reasons = @risk_reasons,
    ai_entities = @entities,
    ai_recommended_next_action = @next_action,
    ai_model = @ai_model,
    ai_processed_at = @processed_at
WHERE case_id = @case_id;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@summary", ToDbNullable(analysis.Summary));
        command.Parameters.AddWithValue("@key_points", ToDbNullable(JsonSerializer.Serialize(analysis.KeyPoints)));
        command.Parameters.AddWithValue("@category", ToDbNullable(analysis.SuggestedCategory));
        command.Parameters.AddWithValue("@confidence", analysis.CategoryConfidence > 0 ? analysis.CategoryConfidence : DBNull.Value);
        command.Parameters.AddWithValue("@risk_level", ToDbNullable(analysis.RiskLevel));
        command.Parameters.AddWithValue("@risk_score", analysis.RiskScore > 0 ? analysis.RiskScore : DBNull.Value);
        command.Parameters.AddWithValue("@risk_reasons", ToDbNullable(JsonSerializer.Serialize(analysis.RiskReasons)));
        command.Parameters.AddWithValue("@entities", ToDbNullable(JsonSerializer.Serialize(analysis.Entities)));
        command.Parameters.AddWithValue("@next_action", ToDbNullable(analysis.RecommendedNextAction));
        command.Parameters.AddWithValue("@ai_model", ToDbNullable(analysis.Model));
        command.Parameters.AddWithValue("@processed_at", analysis.ProcessedAt);
        command.Parameters.AddWithValue("@case_id", blotterId);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public string BuildPrompt(
        int blotterId,
        string incidentType,
        DateTime incidentDate,
        string incidentDetails,
        string respondentName,
        string status,
        string complainantFullName,
        string complainantAddress)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a barangay case analyst. Return JSON only. Do not add markdown, explanation, or extra text.");
        sb.AppendLine();
        sb.AppendLine("Analyze this blotter record and produce JSON using this schema exactly:");
        sb.AppendLine("{");
        sb.AppendLine("  \"summary\": string,");
        sb.AppendLine("  \"key_points\": [string],");
        sb.AppendLine("  \"suggested_category\": string,");
        sb.AppendLine("  \"category_confidence\": number,");
        sb.AppendLine("  \"risk_level\": \"Low\"|\"Medium\"|\"High\",");
        sb.AppendLine("  \"risk_score\": integer,");
        sb.AppendLine("  \"risk_reasons\": [string],");
        sb.AppendLine("  \"entities\": {");
        sb.AppendLine("    \"people\": [string],");
        sb.AppendLine("    \"places\": [string],");
        sb.AppendLine("    \"dates_times\": [string],");
        sb.AppendLine("    \"items\": [string]");
        sb.AppendLine("  },");
        sb.AppendLine("  \"recommended_next_action\": string");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Allowed categories (must match exactly one):");
        sb.AppendLine("- \"Domestic Dispute\"");
        sb.AppendLine("- \"Noise Complaint\"");
        sb.AppendLine("- \"Physical Assault\"");
        sb.AppendLine("- \"Threats/Harassment\"");
        sb.AppendLine("- \"Property Damage\"");
        sb.AppendLine("- \"Theft\"");
        sb.AppendLine("- \"Fraud/Scam\"");
        sb.AppendLine("- \"Neighborhood Conflict\"");
        sb.AppendLine("- \"Public Disturbance\"");
        sb.AppendLine("- \"Child-Related Concern\"");
        sb.AppendLine("- \"VAWC\"");
        sb.AppendLine("- \"Other\"");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Never invent facts. If missing info, reduce confidence and mention what is missing in key_points.");
        sb.AppendLine("- If weapons, repeated threats, physical harm, or minors at risk => risk at least Medium.");
        sb.AppendLine("- Output must be valid JSON and nothing else.");
        sb.AppendLine();
        sb.AppendLine("Blotter data:");
        sb.AppendLine($"- blotter_id: {blotterId}");
        sb.AppendLine($"- complainant: {SafePromptValue(complainantFullName)}");
        sb.AppendLine($"- complainant_address: {SafePromptValue(complainantAddress)}");
        sb.AppendLine($"- respondent_name: {SafePromptValue(respondentName)}");
        sb.AppendLine($"- incident_type: {SafePromptValue(incidentType)}");
        sb.AppendLine($"- incident_date: {incidentDate:yyyy-MM-dd}");
        sb.AppendLine($"- status: {SafePromptValue(status)}");
        sb.AppendLine("- incident_details:");
        sb.AppendLine(SafePromptValue(incidentDetails));
        return sb.ToString();
    }

    private async Task<BlotterAnalysisInput> LoadBlotterInputAsync(int blotterId, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT b.case_id,
       b.respondent_name,
       b.incident_type,
       b.incident_date,
       b.incident_details,
       b.status,
       r.first_name,
       r.middle_name,
       r.last_name,
       h.house_no,
       h.street,
       h.subdivision,
       h.address_note,
       p.name AS purok_name
FROM case_record b
LEFT JOIN resident r ON r.resident_id = b.complainant_id
LEFT JOIN household h ON h.household_id = r.household_id
LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id
WHERE b.case_id = @id
LIMIT 1;";

        using MySqlConnection connection = DBConnection.GetConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", blotterId);

        using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Blotter record {blotterId} was not found.");
        }

        string firstName = reader["first_name"]?.ToString() ?? string.Empty;
        string middleName = reader["middle_name"]?.ToString() ?? string.Empty;
        string lastName = reader["last_name"]?.ToString() ?? string.Empty;
        string fullName = string.Join(" ", new[] { firstName, middleName, lastName }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = "Unknown complainant";
        }

        string houseNo = reader["house_no"]?.ToString() ?? string.Empty;
        string street = reader["street"]?.ToString() ?? string.Empty;
        string subdivision = reader["subdivision"]?.ToString() ?? string.Empty;
        string addressNote = reader["address_note"]?.ToString() ?? string.Empty;
        string purok = reader["purok_name"]?.ToString() ?? string.Empty;
        string address = string.Join(", ", new[] { houseNo, street, subdivision, purok, addressNote }.Where(v => !string.IsNullOrWhiteSpace(v)));

        int incidentDateOrdinal = reader.GetOrdinal("incident_date");

        return new BlotterAnalysisInput
        {
            BlotterId = blotterId,
            RespondentName = reader["respondent_name"]?.ToString() ?? string.Empty,
            IncidentType = reader["incident_type"]?.ToString() ?? string.Empty,
            IncidentDate = reader.IsDBNull(incidentDateOrdinal) ? DateTime.Today : reader.GetDateTime(incidentDateOrdinal),
            IncidentDetails = reader["incident_details"]?.ToString() ?? string.Empty,
            Status = reader["status"]?.ToString() ?? string.Empty,
            ComplainantName = fullName,
            ComplainantAddress = string.IsNullOrWhiteSpace(address) ? "Unknown address" : address
        };
    }

    private string BuildPrompt(BlotterAnalysisInput input)
    {
        return BuildPrompt(
            input.BlotterId,
            input.IncidentType,
            input.IncidentDate,
            input.IncidentDetails,
            input.RespondentName,
            input.Status,
            input.ComplainantName,
            input.ComplainantAddress);
    }

    private static object ToDbNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }

    private static string SafePromptValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(missing)";
        }

        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static void NormalizeAnalysis(AiBlotterAnalysis analysis)
    {
        analysis.Summary = (analysis.Summary ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(analysis.Summary))
        {
            analysis.Summary = "No summary returned by AI.";
        }

        analysis.KeyPoints ??= new List<string>();
        analysis.RiskReasons ??= new List<string>();
        analysis.Entities ??= new AiBlotterEntities();
        analysis.Entities.People ??= new List<string>();
        analysis.Entities.Places ??= new List<string>();
        analysis.Entities.DatesTimes ??= new List<string>();
        analysis.Entities.Items ??= new List<string>();

        if (!AllowedCategories.Contains(analysis.SuggestedCategory))
        {
            analysis.SuggestedCategory = "Other";
        }

        if (analysis.CategoryConfidence < 0) analysis.CategoryConfidence = 0;
        if (analysis.CategoryConfidence > 1) analysis.CategoryConfidence = 1;

        analysis.RiskScore = Math.Clamp(analysis.RiskScore, 0, 100);

        if (analysis.RiskLevel != "Low" && analysis.RiskLevel != "Medium" && analysis.RiskLevel != "High")
        {
            analysis.RiskLevel = analysis.RiskScore >= 70 ? "High" : analysis.RiskScore >= 35 ? "Medium" : "Low";
        }

        analysis.RecommendedNextAction = (analysis.RecommendedNextAction ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(analysis.RecommendedNextAction))
        {
            analysis.RecommendedNextAction = "Review and triage this case manually.";
        }
    }

    private sealed class BlotterAnalysisInput
    {
        public int BlotterId { get; init; }
        public string RespondentName { get; init; } = string.Empty;
        public string IncidentType { get; init; } = string.Empty;
        public DateTime IncidentDate { get; init; }
        public string IncidentDetails { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string ComplainantName { get; init; } = string.Empty;
        public string ComplainantAddress { get; init; } = string.Empty;
    }
}
