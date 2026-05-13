namespace baranggaysystem1;

internal sealed class HotspotPoint
{
	public int PurokId { get; set; }

	public string PurokName { get; set; } = string.Empty;

	public double? Latitude { get; set; }

	public double? Longitude { get; set; }

	public int IncidentCount { get; set; }
}
