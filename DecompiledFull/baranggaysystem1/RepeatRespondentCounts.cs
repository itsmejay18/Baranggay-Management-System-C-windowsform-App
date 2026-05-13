namespace baranggaysystem1;

internal readonly struct RepeatRespondentCounts
{
	public int TotalCases { get; }

	public int ActiveCases { get; }

	public static RepeatRespondentCounts Zero => new RepeatRespondentCounts(0, 0);

	public RepeatRespondentCounts(int totalCases, int activeCases)
	{
		TotalCases = ((totalCases >= 0) ? totalCases : 0);
		ActiveCases = ((activeCases >= 0) ? activeCases : 0);
	}

	public RepeatRespondentCounts Add(RepeatRespondentCounts other)
	{
		return new RepeatRespondentCounts(TotalCases + other.TotalCases, ActiveCases + other.ActiveCases);
	}
}
