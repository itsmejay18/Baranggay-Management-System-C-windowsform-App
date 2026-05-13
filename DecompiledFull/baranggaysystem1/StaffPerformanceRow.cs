namespace baranggaysystem1;

internal sealed class StaffPerformanceRow
{
	public int UserId { get; init; }

	public string Username { get; init; } = string.Empty;

	public string DisplayName { get; init; } = string.Empty;

	public bool IsActive { get; init; }

	public int ApprovalsCompleted { get; set; }

	public int ApprovalsOverdue { get; set; }

	public double AvgRequestToApprovalSeconds { get; set; }

	public int ReleasesCompleted { get; set; }

	public int ReleasesOverdue { get; set; }

	public double AvgApprovalToReleaseSeconds { get; set; }

	public int BlotterStatusChanges { get; set; }

	public int BlotterResolutions { get; set; }

	public int BlotterResolutionsOverdue { get; set; }

	public double AvgBlotterResolutionSeconds { get; set; }
}
