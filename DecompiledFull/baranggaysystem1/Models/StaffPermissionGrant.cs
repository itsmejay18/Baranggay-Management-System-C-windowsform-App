namespace baranggaysystem1.Models;

public sealed class StaffPermissionGrant
{
	public string GroupName { get; set; } = string.Empty;

	public string PermissionKey { get; set; } = string.Empty;

	public bool IsAllowed { get; set; }
}
