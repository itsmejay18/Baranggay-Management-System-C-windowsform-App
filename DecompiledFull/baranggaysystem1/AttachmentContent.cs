using System;

namespace baranggaysystem1;

internal sealed class AttachmentContent
{
	public long AttachmentId { get; init; }

	public string FileName { get; init; } = string.Empty;

	public string MimeType { get; init; } = string.Empty;

	public byte[] Content { get; init; } = Array.Empty<byte>();
}
