using System;
using System.Collections.Generic;
using System.Data.Common;
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

	private static readonly HashSet<string> AllowedCategories = new HashSet<string>(StringComparer.Ordinal)
	{
		"Domestic Dispute", "Noise Complaint", "Physical Assault", "Threats/Harassment", "Property Damage", "Theft", "Fraud/Scam", "Neighborhood Conflict", "Public Disturbance", "Child-Related Concern",
		"VAWC", "Other"
	};

	private readonly OllamaClient _ollamaClient;

	public string ModelName => _ollamaClient.Model;

	public AiBlotterService(OllamaClient? ollamaClient = null)
	{
		_ollamaClient = ollamaClient ?? new OllamaClient();
	}

	public async Task<AiBlotterAnalysis> AnalyzeBlotterAsync(int blotterId, CancellationToken cancellationToken = default(CancellationToken))
	{
		string prompt = BuildPrompt(await LoadBlotterInputAsync(blotterId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		Exception ex = null;
		for (int attempt = 1; attempt <= 3; attempt++)
		{
			try
			{
				if (!JsonUtils.TryExtractFirstJsonObject(JsonUtils.TrimCodeFences(await _ollamaClient.GenerateAsync(prompt, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)), out string jsonObject))
				{
					throw new JsonException("AI output does not contain a valid JSON object.");
				}
				AiBlotterAnalysis aiBlotterAnalysis = JsonUtils.DeserializeStrict<AiBlotterAnalysis>(jsonObject);
				NormalizeAnalysis(aiBlotterAnalysis);
				aiBlotterAnalysis.Model = _ollamaClient.Model;
				aiBlotterAnalysis.ProcessedAt = DateTime.Now;
				return aiBlotterAnalysis;
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
		}
		return AiBlotterAnalysis.CreateFailed(ex?.Message ?? "Unknown AI parsing error.", _ollamaClient.Model);
	}

	public async Task SaveAnalysisAsync(int blotterId, AiBlotterAnalysis analysis, CancellationToken cancellationToken = default(CancellationToken))
	{
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)connection).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MySqlCommand command = new MySqlCommand("UPDATE case_record\nSET ai_summary = @summary,\n    ai_key_points = @key_points,\n    ai_category = @category,\n    ai_category_confidence = @confidence,\n    ai_risk_level = @risk_level,\n    ai_risk_score = @risk_score,\n    ai_risk_reasons = @risk_reasons,\n    ai_entities = @entities,\n    ai_recommended_next_action = @next_action,\n    ai_model = @ai_model,\n    ai_processed_at = @processed_at\nWHERE case_id = @case_id;", connection);
			try
			{
				command.Parameters.AddWithValue("@summary", ToDbNullable(analysis.Summary));
				command.Parameters.AddWithValue("@key_points", ToDbNullable(JsonSerializer.Serialize(analysis.KeyPoints)));
				command.Parameters.AddWithValue("@category", ToDbNullable(analysis.SuggestedCategory));
				command.Parameters.AddWithValue("@confidence", (analysis.CategoryConfidence > 0m) ? ((object)analysis.CategoryConfidence) : DBNull.Value);
				command.Parameters.AddWithValue("@risk_level", ToDbNullable(analysis.RiskLevel));
				command.Parameters.AddWithValue("@risk_score", (analysis.RiskScore > 0) ? ((object)analysis.RiskScore) : DBNull.Value);
				command.Parameters.AddWithValue("@risk_reasons", ToDbNullable(JsonSerializer.Serialize(analysis.RiskReasons)));
				command.Parameters.AddWithValue("@entities", ToDbNullable(JsonSerializer.Serialize(analysis.Entities)));
				command.Parameters.AddWithValue("@next_action", ToDbNullable(analysis.RecommendedNextAction));
				command.Parameters.AddWithValue("@ai_model", ToDbNullable(analysis.Model));
				command.Parameters.AddWithValue("@processed_at", (object)analysis.ProcessedAt);
				command.Parameters.AddWithValue("@case_id", (object)blotterId);
				await ((DbCommand)(object)command).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				((IDisposable)command)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public string BuildPrompt(int blotterId, string incidentType, DateTime incidentDate, string incidentDetails, string respondentName, string status, string complainantFullName, string complainantAddress)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("You are a barangay case analyst. Return JSON only. Do not add markdown, explanation, or extra text.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Analyze this blotter record and produce JSON using this schema exactly:");
		stringBuilder.AppendLine("{");
		stringBuilder.AppendLine("  \"summary\": string,");
		stringBuilder.AppendLine("  \"key_points\": [string],");
		stringBuilder.AppendLine("  \"suggested_category\": string,");
		stringBuilder.AppendLine("  \"category_confidence\": number,");
		stringBuilder.AppendLine("  \"risk_level\": \"Low\"|\"Medium\"|\"High\",");
		stringBuilder.AppendLine("  \"risk_score\": integer,");
		stringBuilder.AppendLine("  \"risk_reasons\": [string],");
		stringBuilder.AppendLine("  \"entities\": {");
		stringBuilder.AppendLine("    \"people\": [string],");
		stringBuilder.AppendLine("    \"places\": [string],");
		stringBuilder.AppendLine("    \"dates_times\": [string],");
		stringBuilder.AppendLine("    \"items\": [string]");
		stringBuilder.AppendLine("  },");
		stringBuilder.AppendLine("  \"recommended_next_action\": string");
		stringBuilder.AppendLine("}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Allowed categories (must match exactly one):");
		stringBuilder.AppendLine("- \"Domestic Dispute\"");
		stringBuilder.AppendLine("- \"Noise Complaint\"");
		stringBuilder.AppendLine("- \"Physical Assault\"");
		stringBuilder.AppendLine("- \"Threats/Harassment\"");
		stringBuilder.AppendLine("- \"Property Damage\"");
		stringBuilder.AppendLine("- \"Theft\"");
		stringBuilder.AppendLine("- \"Fraud/Scam\"");
		stringBuilder.AppendLine("- \"Neighborhood Conflict\"");
		stringBuilder.AppendLine("- \"Public Disturbance\"");
		stringBuilder.AppendLine("- \"Child-Related Concern\"");
		stringBuilder.AppendLine("- \"VAWC\"");
		stringBuilder.AppendLine("- \"Other\"");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Rules:");
		stringBuilder.AppendLine("- Never invent facts. If missing info, reduce confidence and mention what is missing in key_points.");
		stringBuilder.AppendLine("- If weapons, repeated threats, physical harm, or minors at risk => risk at least Medium.");
		stringBuilder.AppendLine("- Output must be valid JSON and nothing else.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Blotter data:");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("- blotter_id: ");
		handler.AppendFormatted(blotterId);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("- complainant: ");
		handler.AppendFormatted(SafePromptValue(complainantFullName));
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder2);
		handler.AppendLiteral("- complainant_address: ");
		handler.AppendFormatted(SafePromptValue(complainantAddress));
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("- respondent_name: ");
		handler.AppendFormatted(SafePromptValue(respondentName));
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
		handler.AppendLiteral("- incident_type: ");
		handler.AppendFormatted(SafePromptValue(incidentType));
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
		handler.AppendLiteral("- incident_date: ");
		handler.AppendFormatted(incidentDate, "yyyy-MM-dd");
		stringBuilder8.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder9 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("- status: ");
		handler.AppendFormatted(SafePromptValue(status));
		stringBuilder9.AppendLine(ref handler);
		stringBuilder.AppendLine("- incident_details:");
		stringBuilder.AppendLine(SafePromptValue(incidentDetails));
		return stringBuilder.ToString();
	}

	private async Task<BlotterAnalysisInput> LoadBlotterInputAsync(int blotterId, CancellationToken cancellationToken)
	{
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)connection).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MySqlCommand command = new MySqlCommand("SELECT b.case_id,\n       b.respondent_name,\n       b.incident_type,\n       b.incident_date,\n       b.incident_details,\n       b.status,\n       r.first_name,\n       r.middle_name,\n       r.last_name,\n       h.house_no,\n       h.street,\n       h.subdivision,\n       h.address_note,\n       p.name AS purok_name\nFROM case_record b\nLEFT JOIN resident r ON r.resident_id = b.complainant_id\nLEFT JOIN household h ON h.household_id = r.household_id\nLEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\nWHERE b.case_id = @id\nLIMIT 1;", connection);
			try
			{
				command.Parameters.AddWithValue("@id", (object)blotterId);
				MySqlDataReader reader = (MySqlDataReader)(await ((DbCommand)(object)command).ExecuteReaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
				try
				{
					if (!(await ((DbDataReader)(object)reader).ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
					{
						throw new InvalidOperationException($"Blotter record {blotterId} was not found.");
					}
					string text = ((DbDataReader)(object)reader)["first_name"]?.ToString() ?? string.Empty;
					string text2 = ((DbDataReader)(object)reader)["middle_name"]?.ToString() ?? string.Empty;
					string text3 = ((DbDataReader)(object)reader)["last_name"]?.ToString() ?? string.Empty;
					string text4 = string.Join(" ", new string[3] { text, text2, text3 }.Where((string v) => !string.IsNullOrWhiteSpace(v))).Trim();
					if (string.IsNullOrWhiteSpace(text4))
					{
						text4 = "Unknown complainant";
					}
					string text5 = ((DbDataReader)(object)reader)["house_no"]?.ToString() ?? string.Empty;
					string text6 = ((DbDataReader)(object)reader)["street"]?.ToString() ?? string.Empty;
					string text7 = ((DbDataReader)(object)reader)["subdivision"]?.ToString() ?? string.Empty;
					string text8 = ((DbDataReader)(object)reader)["address_note"]?.ToString() ?? string.Empty;
					string text9 = ((DbDataReader)(object)reader)["purok_name"]?.ToString() ?? string.Empty;
					string text10 = string.Join(", ", new string[5] { text5, text6, text7, text9, text8 }.Where((string v) => !string.IsNullOrWhiteSpace(v)));
					int ordinal = ((DbDataReader)(object)reader).GetOrdinal("incident_date");
					return new BlotterAnalysisInput
					{
						BlotterId = blotterId,
						RespondentName = (((DbDataReader)(object)reader)["respondent_name"]?.ToString() ?? string.Empty),
						IncidentType = (((DbDataReader)(object)reader)["incident_type"]?.ToString() ?? string.Empty),
						IncidentDate = (((DbDataReader)(object)reader).IsDBNull(ordinal) ? DateTime.Today : ((DbDataReader)(object)reader).GetDateTime(ordinal)),
						IncidentDetails = (((DbDataReader)(object)reader)["incident_details"]?.ToString() ?? string.Empty),
						Status = (((DbDataReader)(object)reader)["status"]?.ToString() ?? string.Empty),
						ComplainantName = text4,
						ComplainantAddress = (string.IsNullOrWhiteSpace(text10) ? "Unknown address" : text10)
					};
				}
				finally
				{
					((IDisposable)reader)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)command)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private string BuildPrompt(BlotterAnalysisInput input)
	{
		return BuildPrompt(input.BlotterId, input.IncidentType, input.IncidentDate, input.IncidentDetails, input.RespondentName, input.Status, input.ComplainantName, input.ComplainantAddress);
	}

	private static object ToDbNullable(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return DBNull.Value;
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
		AiBlotterAnalysis aiBlotterAnalysis = analysis;
		if (aiBlotterAnalysis.KeyPoints == null)
		{
			List<string> list = (aiBlotterAnalysis.KeyPoints = new List<string>());
		}
		aiBlotterAnalysis = analysis;
		if (aiBlotterAnalysis.RiskReasons == null)
		{
			List<string> list = (aiBlotterAnalysis.RiskReasons = new List<string>());
		}
		aiBlotterAnalysis = analysis;
		AiBlotterEntities aiBlotterEntities;
		if (aiBlotterAnalysis.Entities == null)
		{
			aiBlotterEntities = (aiBlotterAnalysis.Entities = new AiBlotterEntities());
		}
		aiBlotterEntities = analysis.Entities;
		if (aiBlotterEntities.People == null)
		{
			List<string> list = (aiBlotterEntities.People = new List<string>());
		}
		aiBlotterEntities = analysis.Entities;
		if (aiBlotterEntities.Places == null)
		{
			List<string> list = (aiBlotterEntities.Places = new List<string>());
		}
		aiBlotterEntities = analysis.Entities;
		if (aiBlotterEntities.DatesTimes == null)
		{
			List<string> list = (aiBlotterEntities.DatesTimes = new List<string>());
		}
		aiBlotterEntities = analysis.Entities;
		if (aiBlotterEntities.Items == null)
		{
			List<string> list = (aiBlotterEntities.Items = new List<string>());
		}
		if (!AllowedCategories.Contains(analysis.SuggestedCategory))
		{
			analysis.SuggestedCategory = "Other";
		}
		if (analysis.CategoryConfidence < 0m)
		{
			analysis.CategoryConfidence = 0m;
		}
		if (analysis.CategoryConfidence > 1m)
		{
			analysis.CategoryConfidence = 1m;
		}
		analysis.RiskScore = Math.Clamp(analysis.RiskScore, 0, 100);
		if (analysis.RiskLevel != "Low" && analysis.RiskLevel != "Medium" && analysis.RiskLevel != "High")
		{
			analysis.RiskLevel = ((analysis.RiskScore >= 70) ? "High" : ((analysis.RiskScore >= 35) ? "Medium" : "Low"));
		}
		analysis.RecommendedNextAction = (analysis.RecommendedNextAction ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(analysis.RecommendedNextAction))
		{
			analysis.RecommendedNextAction = "Review and triage this case manually.";
		}
	}
}
