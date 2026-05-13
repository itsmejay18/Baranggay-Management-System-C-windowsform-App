using System;

namespace baranggaysystem1;

internal sealed class AttachmentListItem
{
	public long AttachmentId { get; set; }

	public string FileName { get; set; } = string.Empty;

	public string MimeType { get; set; } = string.Empty;

	public long FileSizeBytes { get; set; }

	public string Notes { get; set; } = string.Empty;

	public string UploadedBy { get; set; } = string.Empty;

	public DateTime UploadedAt { get; set; }
}
