namespace baranggaysystem1;

internal sealed class HotspotPoint
{
	public int PurokId { get; init; }

	public string PurokName { get; init; } = string.Empty;

	public double? Latitude { get; init; }

	public double? Longitude { get; init; }

	public int IncidentCount { get; init; }
}
