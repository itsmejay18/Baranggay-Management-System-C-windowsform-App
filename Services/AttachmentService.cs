using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using baranggaysystem1.Database;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal enum AttachmentEntityType
{
    Resident = 1,
    Case = 2,
    Certificate = 3
}

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

internal sealed class AttachmentContent
{
    public long AttachmentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public byte[] Content { get; init; } = Array.Empty<byte>();
}

internal static class AttachmentService
{
    private const int MaxAttachmentBytes = 20 * 1024 * 1024; // 20 MB

    public static IReadOnlyList<AttachmentListItem> LoadList(AttachmentEntityType entityType, int entityId)
    {
        if (entityId <= 0)
        {
            return Array.Empty<AttachmentListItem>();
        }

        var rows = new List<AttachmentListItem>();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT a.attachment_id,
                     a.file_name,
                     a.mime_type,
                     a.file_size_bytes,
                     a.notes,
                     a.uploaded_at,
                     COALESCE(ua.username, CONCAT('User #', a.uploaded_by_user_id)) AS uploaded_by
              FROM record_attachment a
              LEFT JOIN user_account ua ON ua.user_id = a.uploaded_by_user_id
              WHERE a.entity_type = @entityType
                AND a.entity_id = @entityId
              ORDER BY a.uploaded_at DESC, a.attachment_id DESC",
            conn);
        cmd.Parameters.AddWithValue("@entityType", ToDbEntityType(entityType));
        cmd.Parameters.AddWithValue("@entityId", entityId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new AttachmentListItem
            {
                AttachmentId = Convert.ToInt64(reader["attachment_id"]),
                FileName = Convert.ToString(reader["file_name"]) ?? string.Empty,
                MimeType = Convert.ToString(reader["mime_type"]) ?? string.Empty,
                FileSizeBytes = reader["file_size_bytes"] == DBNull.Value ? 0 : Convert.ToInt64(reader["file_size_bytes"]),
                Notes = Convert.ToString(reader["notes"]) ?? string.Empty,
                UploadedBy = Convert.ToString(reader["uploaded_by"]) ?? string.Empty,
                UploadedAt = reader["uploaded_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["uploaded_at"])
            });
        }

        return rows;
    }

    public static long AddFromFile(AttachmentEntityType entityType, int entityId, string filePath, string? notes)
    {
        if (!helper.Permissions.CanManageAttachments)
        {
            throw new UnauthorizedAccessException("You do not have permission to manage attachments.");
        }

        if (entityId <= 0)
        {
            throw new InvalidOperationException("A record must be selected before attaching a file.");
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new InvalidOperationException("Attachment file path is required.");
        }

        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            throw new FileNotFoundException("Attachment file was not found.", filePath);
        }

        if (info.Length <= 0)
        {
            throw new InvalidOperationException("Attachment file is empty.");
        }

        if (info.Length > MaxAttachmentBytes)
        {
            throw new InvalidOperationException("Attachment is too large. Maximum allowed size is 20 MB.");
        }

        byte[] bytes = File.ReadAllBytes(filePath);
        string fileName = info.Name;
        string ext = info.Extension?.Trim().TrimStart('.').ToLowerInvariant() ?? string.Empty;
        string mimeType = GuessMimeType(ext);
        string hash = ComputeSha256(bytes);

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"INSERT INTO record_attachment
                (entity_type, entity_id, file_name, file_ext, mime_type, file_size_bytes, file_hash, file_blob, notes, uploaded_by_user_id, uploaded_at)
              VALUES
                (@entityType, @entityId, @fileName, @fileExt, @mimeType, @sizeBytes, @hash, @blob, @notes, @uploadedBy, NOW())",
            conn);
        cmd.Parameters.AddWithValue("@entityType", ToDbEntityType(entityType));
        cmd.Parameters.AddWithValue("@entityId", entityId);
        cmd.Parameters.AddWithValue("@fileName", fileName);
        cmd.Parameters.AddWithValue("@fileExt", string.IsNullOrWhiteSpace(ext) ? DBNull.Value : ext);
        cmd.Parameters.AddWithValue("@mimeType", string.IsNullOrWhiteSpace(mimeType) ? DBNull.Value : mimeType);
        cmd.Parameters.AddWithValue("@sizeBytes", bytes.LongLength);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim());
        cmd.Parameters.Add("@blob", MySqlDbType.LongBlob).Value = bytes;
        cmd.Parameters.AddWithValue("@uploadedBy", helper.UserSession.UserId > 0 ? helper.UserSession.UserId : DBNull.Value);
        cmd.ExecuteNonQuery();

        return cmd.LastInsertedId;
    }

    public static AttachmentContent? LoadContent(long attachmentId)
    {
        if (attachmentId <= 0)
        {
            return null;
        }

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT attachment_id, file_name, mime_type, file_blob
              FROM record_attachment
              WHERE attachment_id = @id
              LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@id", attachmentId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        byte[] bytes = reader["file_blob"] as byte[] ?? Array.Empty<byte>();
        return new AttachmentContent
        {
            AttachmentId = Convert.ToInt64(reader["attachment_id"]),
            FileName = Convert.ToString(reader["file_name"]) ?? "attachment.bin",
            MimeType = Convert.ToString(reader["mime_type"]) ?? string.Empty,
            Content = bytes
        };
    }

    public static void DeleteAttachment(long attachmentId)
    {
        if (!helper.Permissions.CanManageAttachments)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete attachments.");
        }

        if (attachmentId <= 0)
        {
            return;
        }

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand("DELETE FROM record_attachment WHERE attachment_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", attachmentId);
        cmd.ExecuteNonQuery();
    }

    public static string GetEntityDisplayName(AttachmentEntityType entityType)
    {
        return entityType switch
        {
            AttachmentEntityType.Resident => "Resident",
            AttachmentEntityType.Case => "Blotter Case",
            AttachmentEntityType.Certificate => "Certificate",
            _ => "Record"
        };
    }

    private static string ToDbEntityType(AttachmentEntityType entityType)
    {
        return entityType switch
        {
            AttachmentEntityType.Resident => "RESIDENT",
            AttachmentEntityType.Case => "CASE",
            AttachmentEntityType.Certificate => "CERTIFICATE",
            _ => "RESIDENT"
        };
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GuessMimeType(string fileExt)
    {
        return fileExt switch
        {
            "pdf" => "application/pdf",
            "jpg" => "image/jpeg",
            "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "txt" => "text/plain",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ppt" => "application/vnd.ms-powerpoint",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }
}
