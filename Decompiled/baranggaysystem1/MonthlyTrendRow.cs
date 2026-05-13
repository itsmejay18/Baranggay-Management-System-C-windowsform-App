namespace baranggaysystem1;

internal sealed class MonthlyTrendRow
{
	public string MonthKey { get; set; } = string.Empty;

	public string MonthLabel { get; set; } = string.Empty;

	public int Residents { get; set; }

	public int Certificates { get; set; }

	public int Blotters { get; set; }
}
