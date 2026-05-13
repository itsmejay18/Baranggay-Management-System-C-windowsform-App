namespace baranggaysystem1.Models;

internal sealed class RolePermissionSummary
{
	public int RoleId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public int UserCount { get; set; }

	public int ActiveUserCount { get; set; }

	public bool IsCoreRole { get; set; }

	public bool IsSuperAdmin { get; set; }
}
