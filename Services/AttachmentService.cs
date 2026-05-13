using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Service for managing file attachments.
/// </summary>
public static class AttachmentService
{
    /// <summary>
    /// Gets a display name for an entity type.
    /// </summary>
    public static string GetEntityDisplayName(AttachmentEntityType entityType)
    {
        return entityType switch
        {
            AttachmentEntityType.Resident => "Resident",
            AttachmentEntityType.Case => "Case",
            AttachmentEntityType.Certificate => "Certificate",
            AttachmentEntityType.Household => "Household",
            _ => entityType.ToString()
        };
    }

    /// <summary>
    /// Gets all attachments for a given entity.
    /// </summary>
    public static DataTable GetAttachments(AttachmentEntityType entityType, int entityId)
    {
        return DbHelper.LoadTable(
            @"SELECT attachment_id, file_name, file_size, uploaded_at, uploaded_by
              FROM attachment
              WHERE entity_type = @entityType AND entity_id = @entityId
              ORDER BY uploaded_at DESC",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@entityType", entityType.ToString());
                cmd.Parameters.AddWithValue("@entityId", entityId);
            });
    }

    /// <summary>
    /// Loads a list of attachment metadata for a given entity.
    /// </summary>
    public static List<AttachmentListItem> LoadList(AttachmentEntityType entityType, int entityId)
    {
        var table = DbHelper.LoadTable(
            @"SELECT attachment_id, file_name, file_size, mime_type, notes, uploaded_at, uploaded_by
              FROM attachment
              WHERE entity_type = @entityType AND entity_id = @entityId
              ORDER BY uploaded_at DESC",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@entityType", entityType.ToString());
                cmd.Parameters.AddWithValue("@entityId", entityId);
            });

        var items = new List<AttachmentListItem>();
        foreach (DataRow row in table.Rows)
        {
            items.Add(new AttachmentListItem
            {
                AttachmentId = Convert.ToInt64(row["attachment_id"]),
                FileName = row["file_name"]?.ToString() ?? string.Empty,
                FileSizeBytes = row["file_size"] != DBNull.Value ? Convert.ToInt64(row["file_size"]) : 0,
                MimeType = row["mime_type"] != DBNull.Value ? row["mime_type"]?.ToString() ?? string.Empty : string.Empty,
                Notes = row["notes"] != DBNull.Value ? row["notes"]?.ToString() ?? string.Empty : string.Empty,
                UploadedBy = row["uploaded_by"] != DBNull.Value ? row["uploaded_by"]?.ToString() ?? string.Empty : string.Empty,
                UploadedAt = row["uploaded_at"] != DBNull.Value ? Convert.ToDateTime(row["uploaded_at"]) : DateTime.MinValue
            });
        }
        return items;
    }

    /// <summary>
    /// Uploads a new attachment.
    /// </summary>
    public static void Upload(AttachmentEntityType entityType, int entityId, string fileName, byte[] content)
    {
        string mimeType = GetMimeType(fileName);

        DbHelper.ExecuteNonQuery(
            @"INSERT INTO attachment (entity_type, entity_id, file_name, file_size, mime_type, content, uploaded_at, uploaded_by)
              VALUES (@entityType, @entityId, @fileName, @fileSize, @mimeType, @content, NOW(), @uploadedBy)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@entityType", entityType.ToString());
                cmd.Parameters.AddWithValue("@entityId", entityId);
                cmd.Parameters.AddWithValue("@fileName", fileName);
                cmd.Parameters.AddWithValue("@fileSize", content.Length);
                cmd.Parameters.AddWithValue("@mimeType", mimeType);
                cmd.Parameters.AddWithValue("@content", content);
                cmd.Parameters.AddWithValue("@uploadedBy", UserSession.UserId);
            });
    }

    /// <summary>
    /// Adds an attachment from a file path.
    /// </summary>
    public static void AddFromFile(AttachmentEntityType entityType, int entityId, string filePath, string? notes = null)
    {
        string fileName = Path.GetFileName(filePath);
        byte[] content = File.ReadAllBytes(filePath);
        string mimeType = GetMimeType(fileName);

        DbHelper.ExecuteNonQuery(
            @"INSERT INTO attachment (entity_type, entity_id, file_name, file_size, mime_type, notes, content, uploaded_at, uploaded_by)
              VALUES (@entityType, @entityId, @fileName, @fileSize, @mimeType, @notes, @content, NOW(), @uploadedBy)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@entityType", entityType.ToString());
                cmd.Parameters.AddWithValue("@entityId", entityId);
                cmd.Parameters.AddWithValue("@fileName", fileName);
                cmd.Parameters.AddWithValue("@fileSize", content.Length);
                cmd.Parameters.AddWithValue("@mimeType", mimeType);
                cmd.Parameters.AddWithValue("@notes", notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@content", content);
                cmd.Parameters.AddWithValue("@uploadedBy", UserSession.UserId);
            });
    }

    /// <summary>
    /// Downloads an attachment by ID.
    /// </summary>
    public static AttachmentContent? Download(int attachmentId)
    {
        return LoadContent(attachmentId);
    }

    /// <summary>
    /// Loads attachment content by ID.
    /// </summary>
    public static AttachmentContent? LoadContent(long attachmentId)
    {
        var table = DbHelper.LoadTable(
            @"SELECT attachment_id, file_name, content
              FROM attachment
              WHERE attachment_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", attachmentId));

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new AttachmentContent
        {
            AttachmentId = Convert.ToInt64(row["attachment_id"]),
            FileName = row["file_name"]?.ToString() ?? string.Empty,
            Content = row["content"] != DBNull.Value ? (byte[])row["content"] : Array.Empty<byte>()
        };
    }

    /// <summary>
    /// Deletes an attachment by ID.
    /// </summary>
    public static void Delete(int attachmentId)
    {
        DbHelper.ExecuteNonQuery(
            "DELETE FROM attachment WHERE attachment_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", attachmentId));
    }

    /// <summary>
    /// Deletes an attachment by ID (alias).
    /// </summary>
    public static void DeleteAttachment(long attachmentId)
    {
        DbHelper.ExecuteNonQuery(
            "DELETE FROM attachment WHERE attachment_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", attachmentId));
    }

    private static string GetMimeType(string fileName)
    {
        string ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// Represents an attachment list item (metadata only).
/// </summary>
public sealed class AttachmentListItem
{
    public long AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;

    // Alias for backward compatibility
    public long FileSize => FileSizeBytes;
}

/// <summary>
/// Represents attachment content for download.
/// </summary>
public sealed class AttachmentContent
{
    public long AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
