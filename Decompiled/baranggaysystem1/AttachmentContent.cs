using System;

namespace baranggaysystem1;

internal sealed class AttachmentContent
{
	public long AttachmentId { get; set; }

	public string FileName { get; set; } = string.Empty;

	public string MimeType { get; set; } = string.Empty;

	public byte[] Content { get; set; } = Array.Empty<byte>();
}
