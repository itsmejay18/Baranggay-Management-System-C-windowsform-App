namespace baranggaysystem1.Models;

internal sealed class RolePermissionGrantItem
{
	public string PermissionKey { get; set; } = string.Empty;

	public string GroupName { get; set; } = string.Empty;

	public string Label { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public int GroupOrder { get; set; }

	public int ItemOrder { get; set; }

	public bool IsAllowed { get; set; }
}
