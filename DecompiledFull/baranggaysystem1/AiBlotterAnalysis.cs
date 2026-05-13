using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace baranggaysystem1;

internal sealed class AiBlotterAnalysis
{
	[JsonPropertyName("summary")]
	public string Summary { get; set; } = string.Empty;

	[JsonPropertyName("key_points")]
	public List<string> KeyPoints { get; set; } = new List<string>();

	[JsonPropertyName("suggested_category")]
	public string SuggestedCategory { get; set; } = "Other";

	[JsonPropertyName("category_confidence")]
	public decimal CategoryConfidence { get; set; }

	[JsonPropertyName("risk_level")]
	public string RiskLevel { get; set; } = "Low";

	[JsonPropertyName("risk_score")]
	public int RiskScore { get; set; }

	[JsonPropertyName("risk_reasons")]
	public List<string> RiskReasons { get; set; } = new List<string>();

	[JsonPropertyName("entities")]
	public AiBlotterEntities Entities { get; set; } = new AiBlotterEntities();

	[JsonPropertyName("recommended_next_action")]
	public string RecommendedNextAction { get; set; } = string.Empty;

	[JsonIgnore]
	public string Model { get; set; } = string.Empty;

	[JsonIgnore]
	public DateTime ProcessedAt { get; set; } = DateTime.Now;

	public static AiBlotterAnalysis CreateFailed(string reason, string model)
	{
		return new AiBlotterAnalysis
		{
			Summary = "AI analysis failed: " + reason,
			SuggestedCategory = "Other",
			CategoryConfidence = 0m,
			RiskLevel = "Low",
			RiskScore = 0,
			KeyPoints = new List<string> { "Analysis could not be parsed as valid JSON." },
			RiskReasons = new List<string> { reason },
			RecommendedNextAction = "Review incident manually and run AI analysis again.",
			Model = model,
			ProcessedAt = DateTime.Now,
			Entities = new AiBlotterEntities()
		};
	}
}
