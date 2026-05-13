namespace baranggaysystem1.helper;

internal static class PermissionKeys
{
	public const string CreateResidents = "residents.create";

	public const string UpdateResidents = "residents.update";

	public const string DeleteResidents = "residents.delete";

	public const string RequestCertificates = "certificates.request";

	public const string EditCertificateRequests = "certificates.edit_request";

	public const string ApproveCertificates = "certificates.approve";

	public const string IssueCertificates = "certificates.issue";

	public const string CancelCertificates = "certificates.cancel";

	public const string ExportCertificates = "certificates.export";

	public const string CreateBlotter = "blotter.create";

	public const string UpdateBlotterStatus = "blotter.update_status";

	public const string ManageUsers = "users.manage";

	public const string OpenSettings = "settings.open";

	public const string ManageAnnouncements = "announcements.manage";

	public const string ManageProjects = "projects.manage";

	public const string ManageAttachments = "attachments.manage";

	public const string DispatchNotifications = "notifications.dispatch";

	public const string ViewHotspotReports = "reports.view_hotspot";

	public const string ViewHouseholds = "household.view";

	public const string CreateHouseholds = "household.create";

	public const string EditHouseholds = "household.edit";

	public const string DeleteHouseholds = "household.delete";

	public const string TransferHouseholds = "household.transfer";

	internal static readonly string[] All = new string[23]
	{
		"residents.create", "residents.update", "residents.delete", "certificates.request", "certificates.edit_request", "certificates.approve", "certificates.issue", "certificates.cancel", "certificates.export", "blotter.create",
		"blotter.update_status", "users.manage", "settings.open", "announcements.manage", "projects.manage", "attachments.manage", "notifications.dispatch", "reports.view_hotspot", "household.view", "household.create",
		"household.edit", "household.delete", "household.transfer"
	};
}
