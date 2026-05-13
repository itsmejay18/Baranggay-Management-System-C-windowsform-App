using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Service that handles communication with the Ellie AI assistant backend.
/// </summary>
public sealed class EllieAssistantService
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _apiEndpoint;

    public EllieAssistantService()
    {
        _apiEndpoint = Environment.GetEnvironmentVariable("ELLIE_API_ENDPOINT")
            ?? "http://localhost:5000/api/ellie/ask";
    }

    /// <summary>
    /// Sends a question to the Ellie assistant and returns the response.
    /// </summary>
    public async Task<string> AskAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return "Please ask me a question about the barangay system.";
        }

        try
        {
            var payload = new { question, userId = UserSession.UserId };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiEndpoint, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                AppLogger.LogWarning($"Ellie API returned {response.StatusCode}");
                return "I'm having trouble connecting right now. Please try again later.";
            }

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<EllieResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Answer ?? "I couldn't generate a response. Please try rephrasing your question.";
        }
        catch (TaskCanceledException)
        {
            return "The request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            AppLogger.LogWarning($"Ellie assistant HTTP error: {ex.Message}");
            return "I'm unable to reach the assistant service. Please check your connection.";
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Ellie assistant unexpected error.", ex);
            return $"An unexpected error occurred: {ex.Message}";
        }
    }

    private sealed class EllieResponse
    {
        public string? Answer { get; set; }
    }
}
