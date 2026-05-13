namespace baranggaysystem1.Models;

internal sealed class RolePermissionGrantItem
{
	public string PermissionKey { get; init; } = string.Empty;

	public string GroupName { get; init; } = string.Empty;

	public string Label { get; init; } = string.Empty;

	public string Description { get; init; } = string.Empty;

	public int GroupOrder { get; init; }

	public int ItemOrder { get; init; }

	public bool IsAllowed { get; set; }
}
