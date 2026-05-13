namespace baranggaysystem1;

internal sealed class ServiceTimeMetrics
{
	public int ApprovalSamples { get; init; }

	public double AvgRequestToApprovalSeconds { get; init; }

	public int ReleaseSamples { get; init; }

	public double AvgApprovalToReleaseSeconds { get; init; }
}
