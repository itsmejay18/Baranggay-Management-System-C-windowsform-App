namespace baranggaysystem1;

internal sealed class MonthlyTrendRow
{
	public string MonthKey { get; init; } = string.Empty;

	public string MonthLabel { get; init; } = string.Empty;

	public int Residents { get; init; }

	public int Certificates { get; init; }

	public int Blotters { get; init; }
}
