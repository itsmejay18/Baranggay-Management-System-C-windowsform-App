namespace baranggaysystem1.Models;

internal sealed class RolePermissionSummary
{
	public int RoleId { get; init; }

	public string Name { get; init; } = string.Empty;

	public string Description { get; init; } = string.Empty;

	public int UserCount { get; init; }

	public int ActiveUserCount { get; init; }

	public bool IsCoreRole { get; init; }

	public bool IsSuperAdmin { get; init; }
}
