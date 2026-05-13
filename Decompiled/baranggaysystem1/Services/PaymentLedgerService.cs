using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class PaymentLedgerService
{
	private const string GeneralPaymentTypeName = "General Payment";

	private const string GeneralPaymentTypeCode = "PAY";

	public async Task<DataTable> GetLedgerAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT p.payment_id,\n                       p.doc_request_id,\n                       COALESCE(p.or_no, dr.or_number, '') AS or_no,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       COALESCE(dt.name, 'General Payment') AS item_type,\n                       IFNULL(p.amount, IFNULL(dr.fee, 0.00)) AS amount,\n                       COALESCE(p.payment_method, 'Cash') AS payment_method,\n                       'PAID' AS payment_status,\n                       DATE_FORMAT(p.paid_at, '%Y-%m-%d %h:%i %p') AS paid_at,\n                       COALESCE(dr.document_no, '') AS document_no,\n                       COALESCE(dr.remarks, '') AS remarks\n                FROM document_payment p\n                LEFT JOIN document_request dr ON dr.doc_request_id = p.doc_request_id\n                LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n                LEFT JOIN resident r ON r.resident_id = dr.resident_id\n                ORDER BY p.paid_at DESC, p.payment_id DESC\n                LIMIT 250");
	}

	public async Task<string> RecordPaymentAsync(int residentId, string residentName, decimal amount, string orNumber, string paymentMethod, string remarks)
	{
		if (residentId <= 0)
		{
			throw new InvalidOperationException("A resident must be selected before recording payment.");
		}
		int docTypeId = await EnsureGeneralPaymentDocumentTypeAsync();
		string documentNo = BuildDocumentNumber();
		string finalRemarks = (string.IsNullOrWhiteSpace(remarks) ? ("General payment recorded for " + residentName + ".") : remarks.Trim());
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO document_request\n                    (barangay_id, doc_type_id, resident_id, purpose, status, requested_at, approved_at, released_at, requested_by_user_id, approved_by_user_id, released_by_user_id, remarks, document_no, fee, or_number)\n                VALUES\n                    (@barangayId, @docTypeId, @residentId, @purpose, 'RELEASED', NOW(), NOW(), NOW(), @userId, @userId, @userId, @remarks, @documentNo, @fee, @orNumber)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)UserSession.BarangayId);
			cmd.Parameters.AddWithValue("@docTypeId", (object)docTypeId);
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
			cmd.Parameters.AddWithValue("@purpose", (object)"General resident payment");
			cmd.Parameters.AddWithValue("@userId", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
			cmd.Parameters.AddWithValue("@remarks", (object)finalRemarks);
			cmd.Parameters.AddWithValue("@documentNo", (object)documentNo);
			cmd.Parameters.AddWithValue("@fee", (object)amount);
			cmd.Parameters.AddWithValue("@orNumber", NormalizeNullable(orNumber));
		});
		int requestId = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT doc_request_id\n                  FROM document_request\n                  WHERE document_no = @documentNo\n                  ORDER BY doc_request_id DESC\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@documentNo", (object)documentNo);
		});
		if (requestId <= 0)
		{
			throw new InvalidOperationException("Payment request record was created, but the ledger link could not be resolved.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO document_payment\n                    (doc_request_id, amount, or_no, payment_method, paid_at, received_by_user_id)\n                VALUES\n                    (@requestId, @amount, @orNo, @paymentMethod, NOW(), @userId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@requestId", (object)requestId);
			cmd.Parameters.AddWithValue("@amount", (object)amount);
			cmd.Parameters.AddWithValue("@orNo", (object)orNumber.Trim());
			cmd.Parameters.AddWithValue("@paymentMethod", (object)paymentMethod.Trim());
			cmd.Parameters.AddWithValue("@userId", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
		});
		return orNumber.Trim();
	}

	private async Task<int> EnsureGeneralPaymentDocumentTypeAsync()
	{
		int num = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT doc_type_id\n                  FROM document_type\n                  WHERE UPPER(code) = @code\n                     OR UPPER(name) = @name\n                  ORDER BY doc_type_id ASC\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@code", (object)"PAY");
			cmd.Parameters.AddWithValue("@name", (object)"General Payment".ToUpperInvariant());
		});
		if (num > 0)
		{
			return num;
		}
		try
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO document_type\n                        (name, code, fee_default, validity_days, requires_approval, renewal_reminder_days)\n                      VALUES\n                        (@name, @code, 0.00, NULL, 0, NULL)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@name", (object)"General Payment");
				cmd.Parameters.AddWithValue("@code", (object)"PAY");
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to create General Payment document type on first attempt.", ex);
		}
		int num2 = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT doc_type_id\n                  FROM document_type\n                  WHERE UPPER(code) = @code\n                     OR UPPER(name) = @name\n                  ORDER BY doc_type_id ASC\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@code", (object)"PAY");
			cmd.Parameters.AddWithValue("@name", (object)"General Payment".ToUpperInvariant());
		});
		if (num2 <= 0)
		{
			throw new InvalidOperationException("The General Payment document type could not be prepared.");
		}
		return num2;
	}

	private static string BuildDocumentNumber()
	{
		string text = $"{"PAY"}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToUpperInvariant();
		if (text.Length > 30)
		{
			return text.Substring(0, 30);
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
}
