using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace baranggaysystem1;

internal sealed class AiBlotterAnalysis
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("key_points")]
    public List<string> KeyPoints { get; set; } = new();

    [JsonPropertyName("suggested_category")]
    public string SuggestedCategory { get; set; } = "Other";

    [JsonPropertyName("category_confidence")]
    public decimal CategoryConfidence { get; set; }

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = "Low";

    [JsonPropertyName("risk_score")]
    public int RiskScore { get; set; }

    [JsonPropertyName("risk_reasons")]
    public List<string> RiskReasons { get; set; } = new();

    [JsonPropertyName("entities")]
    public AiBlotterEntities Entities { get; set; } = new();

    [JsonPropertyName("recommended_next_action")]
    public string RecommendedNextAction { get; set; } = string.Empty;

    [JsonIgnore]
    public string Model { get; set; } = string.Empty;

    [JsonIgnore]
    public System.DateTime ProcessedAt { get; set; } = System.DateTime.Now;

    public static AiBlotterAnalysis CreateFailed(string reason, string model)
    {
        return new AiBlotterAnalysis
        {
            Summary = $"AI analysis failed: {reason}",
            SuggestedCategory = "Other",
            CategoryConfidence = 0,
            RiskLevel = "Low",
            RiskScore = 0,
            KeyPoints = new List<string> { "Analysis could not be parsed as valid JSON." },
            RiskReasons = new List<string> { reason },
            RecommendedNextAction = "Review incident manually and run AI analysis again.",
            Model = model,
            ProcessedAt = System.DateTime.Now,
            Entities = new AiBlotterEntities()
        };
    }
}

internal sealed class AiBlotterEntities
{
    [JsonPropertyName("people")]
    public List<string> People { get; set; } = new();

    [JsonPropertyName("places")]
    public List<string> Places { get; set; } = new();

    [JsonPropertyName("dates_times")]
    public List<string> DatesTimes { get; set; } = new();

    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = new();
}
