using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using baranggaysystem1.Models;

namespace baranggaysystem1;

/// <summary>
/// Service for AI-powered blotter analysis and recommendations.
/// </summary>
public sealed class AiBlotterService
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _apiEndpoint;

    public AiBlotterService()
    {
        _apiEndpoint = Environment.GetEnvironmentVariable("AI_BLOTTER_ENDPOINT")
            ?? "http://localhost:5000/api/blotter/analyze";
    }

    /// <summary>
    /// Gets the AI model name used for analysis.
    /// </summary>
    public string ModelName => Environment.GetEnvironmentVariable("AI_BLOTTER_MODEL") ?? "gpt-4";

    /// <summary>
    /// Analyzes a blotter case and returns AI-generated insights (public wrapper).
    /// </summary>
    internal async Task<AiBlotterAnalysis?> AnalyzeBlotterAsync(int blotterId)
    {
        return await AnalyzeAsync(blotterId).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves an AI analysis result to the database.
    /// </summary>
    internal async Task SaveAnalysisAsync(int blotterId, AiBlotterAnalysis analysis)
    {
        await Task.Run(() =>
        {
            string json = JsonSerializer.Serialize(analysis);
            DbHelper.ExecuteNonQuery(
                @"INSERT INTO ai_blotter_analysis (case_id, model, summary, risk_level, risk_score, analysis_json, created_at)
                  VALUES (@caseId, @model, @summary, @riskLevel, @riskScore, @json, NOW())
                  ON DUPLICATE KEY UPDATE model = @model, summary = @summary, risk_level = @riskLevel,
                      risk_score = @riskScore, analysis_json = @json, created_at = NOW()",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@caseId", blotterId);
                    cmd.Parameters.AddWithValue("@model", analysis.Model ?? ModelName);
                    cmd.Parameters.AddWithValue("@summary", analysis.Summary);
                    cmd.Parameters.AddWithValue("@riskLevel", analysis.RiskLevel);
                    cmd.Parameters.AddWithValue("@riskScore", analysis.RiskScore);
                    cmd.Parameters.AddWithValue("@json", json);
                });
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Analyzes a blotter case and returns AI-generated insights.
    /// </summary>
    internal async Task<AiBlotterAnalysis?> AnalyzeAsync(int blotterId)
    {
        try
        {
            var table = DbHelper.LoadTable(
                @"SELECT cr.case_id, cr.incident_type, cr.incident_details, cr.incident_location,
                         cr.status, cr.action_taken, cr.resolution_details,
                         cr.incident_date, cr.created_at
                  FROM case_record cr
                  WHERE cr.case_id = @id",
                cmd => cmd.Parameters.AddWithValue("@id", blotterId));

            if (table.Rows.Count == 0)
            {
                return null;
            }

            var row = table.Rows[0];
            var payload = new
            {
                caseId = blotterId,
                incidentType = row["incident_type"]?.ToString(),
                incidentDetails = row["incident_details"]?.ToString(),
                incidentLocation = row["incident_location"]?.ToString(),
                status = row["status"]?.ToString(),
                actionTaken = row["action_taken"]?.ToString()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AppLogger.LogWarning($"AI Blotter API returned {response.StatusCode}");
                return new AiBlotterAnalysis
                {
                    Summary = "Analysis unavailable at this time.",
                    RiskLevel = "Unknown",
                    RecommendedNextAction = "Unable to generate recommendations."
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<AiBlotterAnalysis>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            AppLogger.LogError("AI blotter analysis failed.", ex);
            return new AiBlotterAnalysis
            {
                Summary = "Analysis could not be completed.",
                RiskLevel = "Unknown",
                RecommendedNextAction = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets a quick summary for a blotter case.
    /// </summary>
    public async Task<string> GetQuickSummaryAsync(int blotterId)
    {
        var analysis = await AnalyzeAsync(blotterId).ConfigureAwait(false);
        return analysis?.Summary ?? "No summary available.";
    }
}
