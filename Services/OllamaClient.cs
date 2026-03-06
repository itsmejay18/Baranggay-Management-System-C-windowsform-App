using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace baranggaysystem1;

internal sealed class OllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaClient(string baseUrl = "http://localhost:11434", string model = "gemma3:1b", HttpClient? httpClient = null)
    {
        _model = model;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public string Model => _model;

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _model,
            prompt,
            stream = false,
            temperature = 0.2
        };

        string json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PostAsync("api/generate", content, cancellationToken).ConfigureAwait(false);
        string raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Ollama request failed ({(int)response.StatusCode}): {raw}");
        }

        using JsonDocument doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("response", out JsonElement responseElement))
        {
            throw new JsonException("Ollama response does not contain 'response' field.");
        }

        string? output = responseElement.GetString();
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("Ollama returned an empty response.");
        }

        return output;
    }
}
