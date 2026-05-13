using System;

namespace baranggaysystem1.Models;

public class Staff
{
	public int StaffId { get; set; }

	public string Username { get; set; }

	public string PasswordHash { get; set; }

	public string FirstName { get; set; }

	public string MiddleName { get; set; }

	public string LastName { get; set; }

	public string FullName { get; set; }

	public string Email { get; set; }

	public string ContactNumber { get; set; }

	public string Position { get; set; }

	public string Department { get; set; }

	public string Role { get; set; }

	public bool IsActive { get; set; }

	public DateTime? LastLogin { get; set; }

	public DateTime CreatedAt { get; set; }

	public string Address { get; set; }
}
