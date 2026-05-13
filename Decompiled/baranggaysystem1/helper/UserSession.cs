using System;

namespace baranggaysystem1.helper;

internal static class UserSession
{
	public static int UserId;

	public static int BarangayId;

	public static string Role = string.Empty;

	public static string Username = string.Empty;

	/// <summary>
	/// Whether the user must change their password on this session (after admin reset).
	/// </summary>
	public static bool MustChangePassword;

	/// <summary>
	/// Timestamp of the last recorded user activity (for session timeout tracking).
	/// </summary>
	public static DateTime LastActivityUtc = DateTime.UtcNow;

	/// <summary>
	/// Whether the session is currently locked due to inactivity.
	/// </summary>
	public static bool IsSessionLocked;

	/// <summary>
	/// Clear all session data (on logout or session expiry).
	/// </summary>
	public static void Clear()
	{
		UserId = 0;
		BarangayId = 0;
		Role = string.Empty;
		Username = string.Empty;
		MustChangePassword = false;
		IsSessionLocked = false;
		LastActivityUtc = DateTime.UtcNow;
	}
}
