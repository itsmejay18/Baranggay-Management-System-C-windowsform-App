using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class CertificateRequestService
{
	private static readonly SemaphoreSlim DocumentTypeCleanupLock = new SemaphoreSlim(1, 1);

	private static bool _documentTypesNormalized;

	public async Task<DataTable> GetQueueAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT dr.doc_request_id,\n                       dr.resident_id,\n                       dr.doc_type_id,\n                       COALESCE(NULLIF(dr.document_no, ''), CONCAT('REQ-', LPAD(dr.doc_request_id, 6, '0'))) AS tracking_code,\n                       COALESCE(dr.document_no, '') AS document_no,\n                       COALESCE(dr.verification_token, '') AS verification_token,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       COALESCE(dt.name, 'Certificate') AS certification_type,\n                       COALESCE(dr.purpose, '') AS purpose,\n                       UPPER(COALESCE(dr.status, 'SUBMITTED')) AS status,\n                       DATE_FORMAT(dr.requested_at, '%Y-%m-%d %h:%i %p') AS requested_on,\n                       DATE_FORMAT(dr.released_at, '%Y-%m-%d %h:%i %p') AS released_on,\n                       DATE_FORMAT(dr.expires_at, '%Y-%m-%d') AS expires_on,\n                       IFNULL(dr.fee, 0.00) AS fee,\n                       COALESCE(dr.or_number, '') AS or_number\n                FROM document_request dr\n                LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n                LEFT JOIN resident r ON r.resident_id = dr.resident_id\n                WHERE dr.barangay_id = @barangayId\n                ORDER BY COALESCE(dr.released_at, dr.requested_at) DESC, dr.doc_request_id DESC\n                LIMIT 250", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)UserSession.BarangayId);
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<IReadOnlyList<CertificateDocumentTypeOption>> GetDocumentTypesAsync()
	{
		await EnsureDocumentTypesNormalizedAsync().ConfigureAwait(continueOnCapturedContext: false);
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("\n                SELECT doc_type_id,\n                       name,\n                       COALESCE(code, '') AS code,\n                       IFNULL(fee_default, 0.00) AS fee_default,\n                       validity_days\n                FROM document_type\n                ORDER BY name ASC, doc_type_id ASC").ConfigureAwait(continueOnCapturedContext: false);
		List<CertificateDocumentTypeOption> list = new List<CertificateDocumentTypeOption>(obj.Rows.Count);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (DataRow row in obj.Rows)
		{
			string text = Convert.ToString(row["name"])?.Trim() ?? string.Empty;
			string text2 = Convert.ToString(row["code"])?.Trim() ?? string.Empty;
			string text3 = (string.IsNullOrWhiteSpace(text) ? text2 : text);
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				list.Add(new CertificateDocumentTypeOption
				{
					DocTypeId = Convert.ToInt32(row["doc_type_id"], CultureInfo.InvariantCulture),
					Name = text,
					Code = text2,
					DefaultFee = ((row["fee_default"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["fee_default"], CultureInfo.InvariantCulture)),
					ValidityDays = ((row["validity_days"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(row["validity_days"], CultureInfo.InvariantCulture)))
				});
			}
		}
		return list;
	}

	private static async Task EnsureDocumentTypesNormalizedAsync()
	{
		if (OfflineDatabaseSupport.IsOffline || _documentTypesNormalized)
		{
			return;
		}
		await DocumentTypeCleanupLock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (OfflineDatabaseSupport.IsOffline || _documentTypesNormalized)
			{
				return;
			}
			MySqlConnection conn = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)conn).Open();
				MySqlTransaction tx = conn.BeginTransaction();
				try
				{
					MySqlCommand val = new MySqlCommand("UPDATE document_type SET code = NULL WHERE code IS NOT NULL AND TRIM(code) = ''", conn, tx);
					try
					{
						((DbCommand)(object)val).ExecuteNonQuery();
					}
					finally
					{
						((IDisposable)val)?.Dispose();
					}
					List<(int, string, string)> list = new List<(int, string, string)>();
					MySqlCommand val2 = new MySqlCommand("SELECT doc_type_id,\n                             COALESCE(name, '') AS name,\n                             COALESCE(code, '') AS code\n                      FROM document_type\n                      ORDER BY doc_type_id ASC", conn, tx);
					try
					{
						MySqlDataReader val3 = val2.ExecuteReader();
						try
						{
							while (((DbDataReader)(object)val3).Read())
							{
								list.Add((Convert.ToInt32(((DbDataReader)(object)val3)["doc_type_id"], CultureInfo.InvariantCulture), Convert.ToString(((DbDataReader)(object)val3)["name"]) ?? string.Empty, Convert.ToString(((DbDataReader)(object)val3)["code"]) ?? string.Empty));
							}
						}
						finally
						{
							((IDisposable)val3)?.Dispose();
						}
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
					bool flag = TableExists(conn, tx, "document_number_sequence");
					foreach (IGrouping<string, (int, string, string)> item2 in from @group in list.GroupBy<(int, string, string), string>(GetDocumentTypeIdentityKey, StringComparer.Ordinal)
						where !string.IsNullOrWhiteSpace(@group.Key) && @group.Count() > 1
						select @group)
					{
						int item = item2.First().Item1;
						foreach (var item3 in item2.Skip(1))
						{
							MySqlCommand val4 = new MySqlCommand("UPDATE document_request\n                              SET doc_type_id = @keepId\n                              WHERE doc_type_id = @duplicateId", conn, tx);
							try
							{
								val4.Parameters.AddWithValue("@keepId", (object)item);
								val4.Parameters.AddWithValue("@duplicateId", (object)item3.Item1);
								((DbCommand)(object)val4).ExecuteNonQuery();
							}
							finally
							{
								((IDisposable)val4)?.Dispose();
							}
							if (flag)
							{
								MySqlCommand val5 = new MySqlCommand("INSERT INTO document_number_sequence (doc_type_id, year, last_no)\n                                  SELECT @keepId, year, last_no\n                                  FROM document_number_sequence\n                                  WHERE doc_type_id = @duplicateId\n                                  ON DUPLICATE KEY UPDATE\n                                      last_no = GREATEST(last_no, VALUES(last_no))", conn, tx);
								try
								{
									val5.Parameters.AddWithValue("@keepId", (object)item);
									val5.Parameters.AddWithValue("@duplicateId", (object)item3.Item1);
									((DbCommand)(object)val5).ExecuteNonQuery();
								}
								finally
								{
									((IDisposable)val5)?.Dispose();
								}
								MySqlCommand val6 = new MySqlCommand("DELETE FROM document_number_sequence WHERE doc_type_id = @duplicateId", conn, tx);
								try
								{
									val6.Parameters.AddWithValue("@duplicateId", (object)item3.Item1);
									((DbCommand)(object)val6).ExecuteNonQuery();
								}
								finally
								{
									((IDisposable)val6)?.Dispose();
								}
							}
							MySqlCommand val7 = new MySqlCommand("DELETE FROM document_type WHERE doc_type_id = @duplicateId", conn, tx);
							try
							{
								val7.Parameters.AddWithValue("@duplicateId", (object)item3.Item1);
								((DbCommand)(object)val7).ExecuteNonQuery();
							}
							finally
							{
								((IDisposable)val7)?.Dispose();
							}
						}
					}
					((DbTransaction)(object)tx).Commit();
					_documentTypesNormalized = true;
					try
					{
						await DatabaseManagerAsync.ExecuteNonQueryAsync("CREATE UNIQUE INDEX ux_document_type_code ON document_type(code)").ConfigureAwait(continueOnCapturedContext: false);
					}
					catch
					{
					}
				}
				finally
				{
					((IDisposable)tx)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)conn)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("CertificateRequestService: document type normalization skipped.", ex);
			_documentTypesNormalized = false;
		}
		finally
		{
			DocumentTypeCleanupLock.Release();
		}
	}

	private static bool TableExists(MySqlConnection conn, MySqlTransaction tx, string tableName)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n                  FROM INFORMATION_SCHEMA.TABLES\n                  WHERE TABLE_SCHEMA = DATABASE()\n                    AND TABLE_NAME = @tableName", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@tableName", (object)tableName);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static string GetDocumentTypeIdentityKey((int Id, string Name, string Code) record)
	{
		string text = record.Code.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return "CODE:" + text.ToUpperInvariant();
		}
		string text2 = record.Name.Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return "NAME:" + text2.ToUpperInvariant();
		}
		return string.Empty;
	}

	public async Task<CertificateVerificationRecord?> GetVerificationRecordAsync(int requestId)
	{
		return await LoadVerificationRecordAsync("\n                SELECT dr.doc_request_id,\n                       dr.resident_id,\n                       dr.doc_type_id,\n                       COALESCE(NULLIF(dr.document_no, ''), CONCAT('REQ-', LPAD(dr.doc_request_id, 6, '0'))) AS tracking_code,\n                       COALESCE(dr.document_no, '') AS document_no,\n                       COALESCE(dr.verification_token, '') AS verification_token,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       COALESCE(dt.name, 'Certificate') AS certification_type,\n                       COALESCE(dr.purpose, '') AS purpose,\n                       UPPER(COALESCE(dr.status, 'SUBMITTED')) AS status,\n                       dr.requested_at,\n                       dr.released_at,\n                       dr.verification_token_created_at,\n                       dr.expires_at,\n                       IFNULL(dr.fee, 0.00) AS fee,\n                       COALESCE(dr.or_number, '') AS or_number\n                FROM document_request dr\n                LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n                LEFT JOIN resident r ON r.resident_id = dr.resident_id\n                WHERE dr.doc_request_id = @requestId\n                LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@requestId", (object)requestId);
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<CertificateVerificationRecord?> VerifyDocumentAsync(string lookup)
	{
		string normalizedLookup = NormalizeVerificationLookup(lookup);
		if (string.IsNullOrWhiteSpace(normalizedLookup))
		{
			return null;
		}
		return await LoadVerificationRecordAsync("\n                SELECT dr.doc_request_id,\n                       dr.resident_id,\n                       dr.doc_type_id,\n                       COALESCE(NULLIF(dr.document_no, ''), CONCAT('REQ-', LPAD(dr.doc_request_id, 6, '0'))) AS tracking_code,\n                       COALESCE(dr.document_no, '') AS document_no,\n                       COALESCE(dr.verification_token, '') AS verification_token,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       COALESCE(dt.name, 'Certificate') AS certification_type,\n                       COALESCE(dr.purpose, '') AS purpose,\n                       UPPER(COALESCE(dr.status, 'SUBMITTED')) AS status,\n                       dr.requested_at,\n                       dr.released_at,\n                       dr.verification_token_created_at,\n                       dr.expires_at,\n                       IFNULL(dr.fee, 0.00) AS fee,\n                       COALESCE(dr.or_number, '') AS or_number\n                FROM document_request dr\n                LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n                LEFT JOIN resident r ON r.resident_id = dr.resident_id\n                WHERE UPPER(COALESCE(NULLIF(dr.document_no, ''), CONCAT('REQ-', LPAD(dr.doc_request_id, 6, '0')))) = UPPER(@lookup)\n                   OR UPPER(COALESCE(dr.document_no, '')) = UPPER(@lookup)\n                   OR UPPER(COALESCE(dr.verification_token, '')) = UPPER(@lookup)\n                ORDER BY COALESCE(dr.released_at, dr.requested_at) DESC, dr.doc_request_id DESC\n                LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@lookup", (object)normalizedLookup);
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<CertificateRequestDraft?> GetRequestAsync(int requestId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n                SELECT dr.doc_request_id,\n                       dr.resident_id,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       dr.doc_type_id,\n                       COALESCE(dt.name, 'Certificate') AS document_type_name,\n                       COALESCE(dt.code, '') AS document_type_code,\n                       IFNULL(dt.fee_default, 0.00) AS fee_default,\n                       dt.validity_days,\n                       COALESCE(dr.purpose, '') AS purpose,\n                       IFNULL(dr.fee, 0.00) AS fee,\n                       COALESCE(dr.or_number, '') AS or_number,\n                       COALESCE(dr.business_name, '') AS business_name,\n                       COALESCE(dr.business_nature, '') AS business_nature,\n                       UPPER(COALESCE(dr.status, 'SUBMITTED')) AS status\n                FROM document_request dr\n                LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n                LEFT JOIN resident r ON r.resident_id = dr.resident_id\n                WHERE dr.doc_request_id = @requestId\n                LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@requestId", (object)requestId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		DataRow dataRow = dataTable.Rows[0];
		return new CertificateRequestDraft
		{
			RequestId = requestId,
			ResidentId = Convert.ToInt32(dataRow["resident_id"], CultureInfo.InvariantCulture),
			ResidentName = (Convert.ToString(dataRow["resident_name"]) ?? string.Empty),
			DocTypeId = Convert.ToInt32(dataRow["doc_type_id"], CultureInfo.InvariantCulture),
			DocumentTypeName = (Convert.ToString(dataRow["document_type_name"]) ?? string.Empty),
			DocumentTypeCode = (Convert.ToString(dataRow["document_type_code"]) ?? string.Empty),
			ValidityDays = ((dataRow["validity_days"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(dataRow["validity_days"], CultureInfo.InvariantCulture))),
			Purpose = (Convert.ToString(dataRow["purpose"]) ?? string.Empty),
			Fee = ((dataRow["fee"] == DBNull.Value) ? 0m : Convert.ToDecimal(dataRow["fee"], CultureInfo.InvariantCulture)),
			OrNumber = (Convert.ToString(dataRow["or_number"]) ?? string.Empty),
			BusinessName = (Convert.ToString(dataRow["business_name"]) ?? string.Empty),
			BusinessNature = (Convert.ToString(dataRow["business_nature"]) ?? string.Empty),
			Status = (Convert.ToString(dataRow["status"]) ?? "SUBMITTED")
		};
	}

	public async Task<string> CreateRequestAsync(CertificateRequestDraft draft)
	{
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO document_request\n                    (barangay_id, doc_type_id, resident_id, purpose, status, requested_at, requested_by_user_id, remarks, fee, or_number, business_name, business_nature)\n                VALUES\n                    (@barangayId, @docTypeId, @residentId, @purpose, 'SUBMITTED', NOW(), @userId, @remarks, @fee, @orNumber, @businessName, @businessNature)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)UserSession.BarangayId);
			cmd.Parameters.AddWithValue("@docTypeId", (object)draft.DocTypeId);
			cmd.Parameters.AddWithValue("@residentId", (object)draft.ResidentId);
			cmd.Parameters.AddWithValue("@purpose", NormalizeNullable(draft.Purpose));
			cmd.Parameters.AddWithValue("@userId", NormalizeNullableUserId(UserSession.UserId));
			cmd.Parameters.AddWithValue("@remarks", (object)("Requested via desktop queue for " + draft.ResidentName + "."));
			cmd.Parameters.AddWithValue("@fee", (object)draft.Fee);
			cmd.Parameters.AddWithValue("@orNumber", NormalizeNullable(draft.OrNumber));
			cmd.Parameters.AddWithValue("@businessName", NormalizeNullable(draft.BusinessName));
			cmd.Parameters.AddWithValue("@businessNature", NormalizeNullable(draft.BusinessNature));
		}).ConfigureAwait(continueOnCapturedContext: false);
		return draft.DocumentTypeName + " request submitted for " + draft.ResidentName + ".";
	}

	public async Task<string> IssueAsync(CertificateRequestDraft draft)
	{
		string documentNo = BuildDocumentNumber(draft.DocumentTypeCode, draft.IssuedDate);
		string verificationToken = Guid.NewGuid().ToString("N");
		DateTime? expiresAt = ComputeExpiryDate(draft.IssuedDate, draft.ValidityDays);
		if (draft.RequestId.HasValue && draft.RequestId.Value > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                    UPDATE document_request\n                    SET doc_type_id = @docTypeId,\n                        purpose = @purpose,\n                        fee = @fee,\n                        or_number = @orNumber,\n                        business_name = @businessName,\n                        business_nature = @businessNature,\n                        status = 'RELEASED',\n                        approved_at = COALESCE(approved_at, @issuedAt),\n                        released_at = @issuedAt,\n                        approved_by_user_id = COALESCE(approved_by_user_id, @userId),\n                        released_by_user_id = @userId,\n                        document_no = COALESCE(NULLIF(document_no, ''), @documentNo),\n                        verification_token = COALESCE(NULLIF(verification_token, ''), @verificationToken),\n                        verification_token_created_at = COALESCE(verification_token_created_at, @issuedAt),\n                        expires_at = @expiresAt,\n                        remarks = @remarks,\n                        print_count = IFNULL(print_count, 0) + 1,\n                        last_printed_at = @issuedAt\n                    WHERE doc_request_id = @requestId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@docTypeId", (object)draft.DocTypeId);
				cmd.Parameters.AddWithValue("@purpose", NormalizeNullable(draft.Purpose));
				cmd.Parameters.AddWithValue("@fee", (object)draft.Fee);
				cmd.Parameters.AddWithValue("@orNumber", NormalizeNullable(draft.OrNumber));
				cmd.Parameters.AddWithValue("@businessName", NormalizeNullable(draft.BusinessName));
				cmd.Parameters.AddWithValue("@businessNature", NormalizeNullable(draft.BusinessNature));
				cmd.Parameters.AddWithValue("@issuedAt", (object)draft.IssuedDate);
				cmd.Parameters.AddWithValue("@userId", NormalizeNullableUserId(UserSession.UserId));
				cmd.Parameters.AddWithValue("@documentNo", (object)documentNo);
				cmd.Parameters.AddWithValue("@verificationToken", (object)verificationToken);
				cmd.Parameters.AddWithValue("@expiresAt", NormalizeNullable(expiresAt));
				cmd.Parameters.AddWithValue("@remarks", (object)("Released and printed for " + draft.ResidentName + "."));
				cmd.Parameters.AddWithValue("@requestId", (object)draft.RequestId.Value);
			}).ConfigureAwait(continueOnCapturedContext: false);
			return string.IsNullOrWhiteSpace(draft.DocumentTypeName) ? ("Certificate released successfully. Document No: " + documentNo) : (draft.DocumentTypeName + " released successfully. Document No: " + documentNo);
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO document_request\n                    (barangay_id, doc_type_id, resident_id, purpose, status, requested_at, approved_at, released_at, requested_by_user_id, approved_by_user_id, released_by_user_id, remarks, document_no, fee, or_number, business_name, business_nature, print_count, last_printed_at, verification_token, verification_token_created_at, expires_at)\n                VALUES\n                    (@barangayId, @docTypeId, @residentId, @purpose, 'RELEASED', @issuedAt, @issuedAt, @issuedAt, @userId, @userId, @userId, @remarks, @documentNo, @fee, @orNumber, @businessName, @businessNature, 1, @issuedAt, @verificationToken, @issuedAt, @expiresAt)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)UserSession.BarangayId);
			cmd.Parameters.AddWithValue("@docTypeId", (object)draft.DocTypeId);
			cmd.Parameters.AddWithValue("@residentId", (object)draft.ResidentId);
			cmd.Parameters.AddWithValue("@purpose", NormalizeNullable(draft.Purpose));
			cmd.Parameters.AddWithValue("@issuedAt", (object)draft.IssuedDate);
			cmd.Parameters.AddWithValue("@userId", NormalizeNullableUserId(UserSession.UserId));
			cmd.Parameters.AddWithValue("@remarks", (object)("Issued directly for " + draft.ResidentName + "."));
			cmd.Parameters.AddWithValue("@documentNo", (object)documentNo);
			cmd.Parameters.AddWithValue("@fee", (object)draft.Fee);
			cmd.Parameters.AddWithValue("@orNumber", NormalizeNullable(draft.OrNumber));
			cmd.Parameters.AddWithValue("@businessName", NormalizeNullable(draft.BusinessName));
			cmd.Parameters.AddWithValue("@businessNature", NormalizeNullable(draft.BusinessNature));
			cmd.Parameters.AddWithValue("@verificationToken", (object)verificationToken);
			cmd.Parameters.AddWithValue("@expiresAt", NormalizeNullable(expiresAt));
		}).ConfigureAwait(continueOnCapturedContext: false);
		return string.IsNullOrWhiteSpace(draft.DocumentTypeName) ? ("Certificate issued successfully. Document No: " + documentNo) : (draft.DocumentTypeName + " issued successfully. Document No: " + documentNo);
	}

	public async Task CancelRequestAsync(int requestId, string documentLabel)
	{
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                UPDATE document_request\n                SET status = 'CANCELLED',\n                    remarks = @remarks\n                WHERE doc_request_id = @requestId\n                  AND UPPER(COALESCE(status, 'SUBMITTED')) <> 'RELEASED'", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@remarks", (object)(documentLabel + " was cancelled from the queue."));
			cmd.Parameters.AddWithValue("@requestId", (object)requestId);
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<CertificateVerificationRecord?> LoadVerificationRecordAsync(string sql, Action<MySqlCommand> configure)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync(sql, configure).ConfigureAwait(continueOnCapturedContext: false);
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		DataRow dataRow = dataTable.Rows[0];
		return new CertificateVerificationRecord
		{
			RequestId = Convert.ToInt32(dataRow["doc_request_id"], CultureInfo.InvariantCulture),
			ResidentId = ((dataRow["resident_id"] != DBNull.Value) ? Convert.ToInt32(dataRow["resident_id"], CultureInfo.InvariantCulture) : 0),
			TrackingCode = (Convert.ToString(dataRow["tracking_code"]) ?? string.Empty),
			DocumentNo = (Convert.ToString(dataRow["document_no"]) ?? string.Empty),
			VerificationToken = (Convert.ToString(dataRow["verification_token"]) ?? string.Empty),
			ResidentName = (Convert.ToString(dataRow["resident_name"]) ?? string.Empty),
			DocumentTypeName = (Convert.ToString(dataRow["certification_type"]) ?? string.Empty),
			Purpose = (Convert.ToString(dataRow["purpose"]) ?? string.Empty),
			Status = (Convert.ToString(dataRow["status"]) ?? "SUBMITTED"),
			RequestedAt = ReadNullableDate(dataRow, "requested_at"),
			ReleasedAt = ReadNullableDate(dataRow, "released_at"),
			VerificationTokenCreatedAt = ReadNullableDate(dataRow, "verification_token_created_at"),
			ExpiresAt = ReadNullableDate(dataRow, "expires_at"),
			Fee = ((dataRow["fee"] == DBNull.Value) ? 0m : Convert.ToDecimal(dataRow["fee"], CultureInfo.InvariantCulture)),
			OrNumber = (Convert.ToString(dataRow["or_number"]) ?? string.Empty)
		};
	}

	private static DateTime? ReadNullableDate(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return null;
		}
		return Convert.ToDateTime(row[columnName], CultureInfo.InvariantCulture);
	}

	private static string NormalizeVerificationLookup(string? lookup)
	{
		string text = (lookup ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string[] array = new string[3] { "token=", "document=", "tracking=" };
		foreach (string text2 in array)
		{
			int num = text.IndexOf(text2, StringComparison.OrdinalIgnoreCase);
			if (num >= 0)
			{
				int num2 = num + text2.Length;
				int num3 = text.IndexOfAny(new char[4] { '|', '&', '?', ' ' }, num2);
				string text4;
				if (num3 < 0)
				{
					string text3 = text;
					int num4 = num2;
					text4 = text3.Substring(num4, text3.Length - num4);
				}
				else
				{
					text4 = text.Substring(num2, num3 - num2);
				}
				string text5 = text4;
				if (!string.IsNullOrWhiteSpace(text5))
				{
					return text5.Trim();
				}
			}
		}
		if (text.StartsWith("BMS-VERIFY:", StringComparison.OrdinalIgnoreCase))
		{
			string text3 = text;
			int i = "BMS-VERIFY:".Length;
			return text3.Substring(i, text3.Length - i).Trim();
		}
		return text;
	}

	private static object NormalizeNullable(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static object NormalizeNullable(DateTime? value)
	{
		if (!value.HasValue)
		{
			return DBNull.Value;
		}
		return value.Value;
	}

	private static object NormalizeNullableUserId(int userId)
	{
		if (userId <= 0)
		{
			return DBNull.Value;
		}
		return userId;
	}

	private static DateTime? ComputeExpiryDate(DateTime issuedDate, int? validityDays)
	{
		if (!validityDays.HasValue || validityDays.Value <= 0)
		{
			return null;
		}
		return issuedDate.Date.AddDays(validityDays.Value);
	}

	private static string BuildDocumentNumber(string? documentCode, DateTime issuedDate)
	{
		string value = (string.IsNullOrWhiteSpace(documentCode) ? "DOC" : new string(documentCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant());
		if (string.IsNullOrWhiteSpace(value))
		{
			value = "DOC";
		}
		string text = $"{value}-{issuedDate:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant();
		if (text.Length > 30)
		{
			return text.Substring(0, 30);
		}
		return text;
	}
}
