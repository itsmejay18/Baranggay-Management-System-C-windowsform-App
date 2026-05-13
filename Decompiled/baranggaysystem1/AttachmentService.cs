using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class AttachmentService
{
	private const int MaxAttachmentBytes = 20971520;

	public static IReadOnlyList<AttachmentListItem> LoadList(AttachmentEntityType entityType, int entityId)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		if (entityId <= 0)
		{
			return Array.Empty<AttachmentListItem>();
		}
		List<AttachmentListItem> list = new List<AttachmentListItem>();
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT a.attachment_id,\n                     a.file_name,\n                     a.mime_type,\n                     a.file_size_bytes,\n                     a.notes,\n                     a.uploaded_at,\n                     COALESCE(ua.username, CONCAT('User #', a.uploaded_by_user_id)) AS uploaded_by\n              FROM record_attachment a\n              LEFT JOIN user_account ua ON ua.user_id = a.uploaded_by_user_id\n              WHERE a.entity_type = @entityType\n                AND a.entity_id = @entityId\n              ORDER BY a.uploaded_at DESC, a.attachment_id DESC", connection);
			try
			{
				val.Parameters.AddWithValue("@entityType", (object)ToDbEntityType(entityType));
				val.Parameters.AddWithValue("@entityId", (object)entityId);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						list.Add(new AttachmentListItem
						{
							AttachmentId = Convert.ToInt64(((DbDataReader)(object)val2)["attachment_id"]),
							FileName = (Convert.ToString(((DbDataReader)(object)val2)["file_name"]) ?? string.Empty),
							MimeType = (Convert.ToString(((DbDataReader)(object)val2)["mime_type"]) ?? string.Empty),
							FileSizeBytes = ((((DbDataReader)(object)val2)["file_size_bytes"] == DBNull.Value) ? 0 : Convert.ToInt64(((DbDataReader)(object)val2)["file_size_bytes"])),
							Notes = (Convert.ToString(((DbDataReader)(object)val2)["notes"]) ?? string.Empty),
							UploadedBy = (Convert.ToString(((DbDataReader)(object)val2)["uploaded_by"]) ?? string.Empty),
							UploadedAt = ((((DbDataReader)(object)val2)["uploaded_at"] == DBNull.Value) ? DateTime.MinValue : Convert.ToDateTime(((DbDataReader)(object)val2)["uploaded_at"]))
						});
					}
					return list;
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public static long AddFromFile(AttachmentEntityType entityType, int entityId, string filePath, string? notes)
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		if (!Permissions.CanManageAttachments)
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
		FileInfo fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists)
		{
			throw new FileNotFoundException("Attachment file was not found.", filePath);
		}
		if (fileInfo.Length <= 0)
		{
			throw new InvalidOperationException("Attachment file is empty.");
		}
		if (fileInfo.Length > 20971520)
		{
			throw new InvalidOperationException("Attachment is too large. Maximum allowed size is 20 MB.");
		}
		byte[] array = File.ReadAllBytes(filePath);
		string name = fileInfo.Name;
		string text = fileInfo.Extension?.Trim().TrimStart('.').ToLowerInvariant() ?? string.Empty;
		string text2 = GuessMimeType(text);
		string text3 = ComputeSha256(array);
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("INSERT INTO record_attachment\n                (entity_type, entity_id, file_name, file_ext, mime_type, file_size_bytes, file_hash, file_blob, notes, uploaded_by_user_id, uploaded_at)\n              VALUES\n                (@entityType, @entityId, @fileName, @fileExt, @mimeType, @sizeBytes, @hash, @blob, @notes, @uploadedBy, NOW())", connection);
			try
			{
				val.Parameters.AddWithValue("@entityType", (object)ToDbEntityType(entityType));
				val.Parameters.AddWithValue("@entityId", (object)entityId);
				val.Parameters.AddWithValue("@fileName", (object)name);
				val.Parameters.AddWithValue("@fileExt", (object)(string.IsNullOrWhiteSpace(text) ? ((IConvertible)DBNull.Value) : ((IConvertible)text)));
				val.Parameters.AddWithValue("@mimeType", (object)(string.IsNullOrWhiteSpace(text2) ? ((IConvertible)DBNull.Value) : ((IConvertible)text2)));
				val.Parameters.AddWithValue("@sizeBytes", (object)array.LongLength);
				val.Parameters.AddWithValue("@hash", (object)text3);
				val.Parameters.AddWithValue("@notes", (object)(string.IsNullOrWhiteSpace(notes) ? ((IConvertible)DBNull.Value) : ((IConvertible)notes.Trim())));
				((DbParameter)(object)val.Parameters.Add("@blob", (MySqlDbType)251)).Value = array;
				val.Parameters.AddWithValue("@uploadedBy", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
				((DbCommand)(object)val).ExecuteNonQuery();
				return val.LastInsertedId;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public static AttachmentContent? LoadContent(long attachmentId)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		if (attachmentId <= 0)
		{
			return null;
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT attachment_id, file_name, mime_type, file_blob\n              FROM record_attachment\n              WHERE attachment_id = @id\n              LIMIT 1", connection);
			try
			{
				val.Parameters.AddWithValue("@id", (object)attachmentId);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					if (!((DbDataReader)(object)val2).Read())
					{
						return null;
					}
					byte[] content = (((DbDataReader)(object)val2)["file_blob"] as byte[]) ?? Array.Empty<byte>();
					return new AttachmentContent
					{
						AttachmentId = Convert.ToInt64(((DbDataReader)(object)val2)["attachment_id"]),
						FileName = (Convert.ToString(((DbDataReader)(object)val2)["file_name"]) ?? "attachment.bin"),
						MimeType = (Convert.ToString(((DbDataReader)(object)val2)["mime_type"]) ?? string.Empty),
						Content = content
					};
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public static void DeleteAttachment(long attachmentId)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		if (!Permissions.CanManageAttachments)
		{
			throw new UnauthorizedAccessException("You do not have permission to delete attachments.");
		}
		if (attachmentId <= 0)
		{
			return;
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("DELETE FROM record_attachment WHERE attachment_id = @id", connection);
			try
			{
				val.Parameters.AddWithValue("@id", (object)attachmentId);
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public static string GetEntityDisplayName(AttachmentEntityType entityType)
	{
		return entityType switch
		{
			AttachmentEntityType.Resident => "Resident", 
			AttachmentEntityType.Case => "Blotter Case", 
			AttachmentEntityType.Certificate => "Certificate", 
			_ => "Record", 
		};
	}

	private static string ToDbEntityType(AttachmentEntityType entityType)
	{
		return entityType switch
		{
			AttachmentEntityType.Resident => "RESIDENT", 
			AttachmentEntityType.Case => "CASE", 
			AttachmentEntityType.Certificate => "CERTIFICATE", 
			_ => "RESIDENT", 
		};
	}

	private static string ComputeSha256(byte[] bytes)
	{
		using SHA256 sHA = SHA256.Create();
		return Convert.ToHexString(sHA.ComputeHash(bytes)).ToLowerInvariant();
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
			_ => "application/octet-stream", 
		};
	}
}
