namespace baranggaysystem1;

internal sealed class ServiceTimeMetrics
{
	public int ApprovalSamples { get; set; }

	public double AvgRequestToApprovalSeconds { get; set; }

	public int ReleaseSamples { get; set; }

	public double AvgApprovalToReleaseSeconds { get; set; }
}
