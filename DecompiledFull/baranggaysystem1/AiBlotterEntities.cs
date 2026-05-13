using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace baranggaysystem1;

internal sealed class AiBlotterEntities
{
	[JsonPropertyName("people")]
	public List<string> People { get; set; } = new List<string>();

	[JsonPropertyName("places")]
	public List<string> Places { get; set; } = new List<string>();

	[JsonPropertyName("dates_times")]
	public List<string> DatesTimes { get; set; } = new List<string>();

	[JsonPropertyName("items")]
	public List<string> Items { get; set; } = new List<string>();
}
