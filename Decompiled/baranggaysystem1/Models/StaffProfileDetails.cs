using System;
using System.Collections.Generic;

namespace baranggaysystem1.Models;

public sealed class StaffProfileDetails
{
	public int UserId { get; set; }

	public string Username { get; set; } = string.Empty;

	public string FullName { get; set; } = string.Empty;

	public string FirstName { get; set; } = string.Empty;

	public string MiddleName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string ContactNumber { get; set; } = string.Empty;

	public string Position { get; set; } = string.Empty;

	public string Department { get; set; } = string.Empty;

	public string RoleName { get; set; } = string.Empty;

	public string RoleDescription { get; set; } = string.Empty;

	public string PhotoUrl { get; set; } = string.Empty;

	public bool IsActive { get; set; }

	public DateTime? LastLoginAt { get; set; }

	public DateTime? CreatedAt { get; set; }

	public List<StaffPermissionGrant> Permissions { get; } = new List<StaffPermissionGrant>();
}
