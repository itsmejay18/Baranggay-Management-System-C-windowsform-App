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

	public string Model => _model;

	public OllamaClient(string baseUrl = "http://localhost:11434", string model = "gemma3:1b", HttpClient? httpClient = null)
	{
		_model = model;
		_httpClient = httpClient ?? new HttpClient();
		_httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
		_httpClient.Timeout = TimeSpan.FromSeconds(60.0);
	}

	public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default(CancellationToken))
	{
		string content = JsonSerializer.Serialize(new
		{
			model = _model,
			prompt = prompt,
			stream = false,
			temperature = 0.2
		});
		using StringContent content2 = new StringContent(content, Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await _httpClient.PostAsync("api/generate", content2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Ollama request failed ({(int)response.StatusCode}): {text}");
		}
		using JsonDocument jsonDocument = JsonDocument.Parse(text);
		if (!jsonDocument.RootElement.TryGetProperty("response", out var value))
		{
			throw new JsonException("Ollama response does not contain 'response' field.");
		}
		string? text2 = value.GetString();
		if (string.IsNullOrWhiteSpace(text2))
		{
			throw new InvalidOperationException("Ollama returned an empty response.");
		}
		return text2;
	}
}
