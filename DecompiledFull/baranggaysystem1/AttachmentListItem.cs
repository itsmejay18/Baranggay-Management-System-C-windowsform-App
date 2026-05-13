using System;

namespace baranggaysystem1;

internal sealed class AttachmentListItem
{
	public long AttachmentId { get; init; }

	public string FileName { get; init; } = string.Empty;

	public string MimeType { get; init; } = string.Empty;

	public long FileSizeBytes { get; init; }

	public string Notes { get; init; } = string.Empty;

	public string UploadedBy { get; init; } = string.Empty;

	public DateTime UploadedAt { get; init; }
}
