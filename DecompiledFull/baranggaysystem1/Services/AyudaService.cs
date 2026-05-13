using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class AyudaService
{
	public async Task<DataTable> GetProgramLedgerAsync()
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT ap.program_id,\n                       ap.program_name,\n                       ap.category,\n                       IFNULL(ap.allocated_budget, 0.00) AS allocated_budget,\n                       IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00) AS spent_budget,\n                       CASE\n                           WHEN IFNULL(ap.allocated_budget, 0.00) - IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00) < 0\n                               THEN 0.00\n                           ELSE IFNULL(ap.allocated_budget, 0.00) - IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00)\n                       END AS remaining_budget,\n                       COUNT(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN 1 ELSE NULL END) AS release_count,\n                       COUNT(DISTINCT CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN ar.resident_id ELSE NULL END) AS beneficiary_count,\n                       UPPER(COALESCE(ap.status, 'ACTIVE')) AS status,\n                       DATE_FORMAT(ap.start_date, '%Y-%m-%d') AS start_date_display,\n                       DATE_FORMAT(ap.end_date, '%Y-%m-%d') AS end_date_display,\n                       COALESCE(ap.notes, '') AS notes\n                FROM ayuda_program ap\n                LEFT JOIN ayuda_release ar ON ar.program_id = ap.program_id\n                WHERE ap.barangay_id = @barangayId\n                GROUP BY ap.program_id,\n                         ap.program_name,\n                         ap.category,\n                         ap.allocated_budget,\n                         ap.status,\n                         ap.start_date,\n                         ap.end_date,\n                         ap.notes,\n                         ap.created_at\n                ORDER BY CASE UPPER(COALESCE(ap.status, 'ACTIVE'))\n                             WHEN 'ACTIVE' THEN 0\n                             WHEN 'PAUSED' THEN 1\n                             ELSE 2\n                         END,\n                         ap.created_at DESC,\n                         ap.program_name ASC\n                LIMIT 250", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<DataTable> GetReleaseLedgerAsync()
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT ar.release_id,\n                       COALESCE(ar.batch_id, 0) AS batch_id,\n                       ar.program_id,\n                       ar.resident_id,\n                       ar.reference_no,\n                       COALESCE(arb.batch_reference, COALESCE(ar.batch_reference, ar.reference_no)) AS batch_reference,\n                       ap.program_name,\n                       ap.category,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       COALESCE(r.contact_no, '') AS contact_no,\n                       IFNULL(ar.amount, 0.00) AS amount,\n                       DATE_FORMAT(ar.released_at, '%Y-%m-%d %h:%i %p') AS released_at,\n                       UPPER(COALESCE(ar.release_status, 'RELEASED')) AS release_status,\n                       COALESCE(arb.beneficiary_count, 1) AS beneficiary_count,\n                       COALESCE(arb.report_file_path, '') AS report_file_path,\n                       COALESCE(arb.notes, ar.notes, '') AS notes\n                FROM ayuda_release ar\n                INNER JOIN ayuda_program ap ON ap.program_id = ar.program_id\n                LEFT JOIN ayuda_release_batch arb ON arb.batch_id = ar.batch_id\n                LEFT JOIN resident r ON r.resident_id = ar.resident_id\n                WHERE ap.barangay_id = @barangayId\n                ORDER BY ar.released_at DESC, ar.release_id DESC\n                LIMIT 500", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<IReadOnlyList<AyudaProgramOption>> GetProgramOptionsAsync(int? includeProgramId = null)
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		return (from row in (await DatabaseManagerAsync.LoadTableAsync("\n                SELECT ap.program_id,\n                       ap.program_name,\n                       ap.category,\n                       CASE\n                           WHEN IFNULL(ap.allocated_budget, 0.00) - IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00) < 0\n                               THEN 0.00\n                           ELSE IFNULL(ap.allocated_budget, 0.00) - IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00)\n                       END AS remaining_budget,\n                       UPPER(COALESCE(ap.status, 'ACTIVE')) AS status\n                FROM ayuda_program ap\n                LEFT JOIN ayuda_release ar ON ar.program_id = ap.program_id\n                WHERE ap.barangay_id = @barangayId\n                GROUP BY ap.program_id, ap.program_name, ap.category, ap.allocated_budget, ap.status\n                HAVING (\n                    UPPER(COALESCE(ap.status, 'ACTIVE')) = 'ACTIVE'\n                    AND remaining_budget > 0\n                )\n                   OR (@includeProgramId > 0 AND ap.program_id = @includeProgramId)\n                ORDER BY ap.program_name ASC", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
				cmd.Parameters.AddWithValue("@includeProgramId", (object)includeProgramId.GetValueOrDefault());
			}).ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable()
			select new AyudaProgramOption
			{
				ProgramId = Convert.ToInt32(row["program_id"], CultureInfo.InvariantCulture),
				ProgramName = (Convert.ToString(row["program_name"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
				Category = (Convert.ToString(row["category"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
				RemainingBudget = ((row["remaining_budget"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["remaining_budget"], CultureInfo.InvariantCulture)),
				Status = (Convert.ToString(row["status"], CultureInfo.InvariantCulture)?.Trim() ?? "ACTIVE")
			} into option
			group option by option.ProgramId into @group
			select @group.First()).ToList();
	}

	public async Task<AyudaProgramRecord?> GetProgramAsync(int programId)
	{
		if (programId <= 0)
		{
			return null;
		}
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n                SELECT ap.program_id,\n                       ap.program_name,\n                       ap.category,\n                       IFNULL(ap.allocated_budget, 0.00) AS allocated_budget,\n                       IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00) AS spent_budget,\n                       CASE\n                           WHEN IFNULL(ap.allocated_budget, 0.00) - IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00) < 0\n                               THEN 0.00\n                           ELSE IFNULL(ap.allocated_budget, 0.00) - IFNULL(SUM(CASE WHEN UPPER(COALESCE(ar.release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(ar.amount, 0.00) ELSE 0.00 END), 0.00)\n                       END AS remaining_budget,\n                       UPPER(COALESCE(ap.status, 'ACTIVE')) AS status,\n                       ap.start_date,\n                       ap.end_date,\n                       COALESCE(ap.notes, '') AS notes\n                FROM ayuda_program ap\n                LEFT JOIN ayuda_release ar ON ar.program_id = ap.program_id\n                WHERE ap.program_id = @programId\n                  AND ap.barangay_id = @barangayId\n                GROUP BY ap.program_id,\n                         ap.program_name,\n                         ap.category,\n                         ap.allocated_budget,\n                         ap.status,\n                         ap.start_date,\n                         ap.end_date,\n                         ap.notes\n                LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@programId", (object)programId);
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
		return (dataTable.Rows.Count == 0) ? null : MapProgramRecord(dataTable.Rows[0]);
	}

	public async Task SaveProgramAsync(AyudaProgramRecord record)
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		AyudaProgramRecord sanitized = SanitizeProgramRecord(record);
		AyudaProgramRecord ayudaProgramRecord = ((sanitized.ProgramId <= 0) ? null : (await GetProgramAsync(sanitized.ProgramId).ConfigureAwait(continueOnCapturedContext: false)));
		AyudaProgramRecord before = ayudaProgramRecord;
		if (sanitized.ProgramId > 0 && before == null)
		{
			throw new InvalidOperationException("The selected ayuda program could not be found.");
		}
		if (before != null && sanitized.AllocatedBudget < before.SpentBudget)
		{
			throw new InvalidOperationException($"Allocated budget cannot be lower than the already released amount of PHP {before.SpentBudget:N2}.");
		}
		if (sanitized.ProgramId > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                    UPDATE ayuda_program\n                    SET program_name = @programName,\n                        category = @category,\n                        allocated_budget = @allocatedBudget,\n                        status = @status,\n                        start_date = @startDate,\n                        end_date = @endDate,\n                        notes = @notes,\n                        updated_by_user_id = @userId,\n                        updated_at = CURRENT_TIMESTAMP\n                    WHERE program_id = @programId\n                      AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
			{
				AddProgramParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@programId", (object)sanitized.ProgramId);
			}).ConfigureAwait(continueOnCapturedContext: false);
			AuditTrailService.Log("Ayuda", "ayuda_program", sanitized.ProgramId, "UPDATE", before, sanitized, "Ayuda budget program updated.");
		}
		else
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO ayuda_program\n                    (barangay_id, program_name, category, allocated_budget, status, start_date, end_date, notes, created_by_user_id, updated_by_user_id)\n                VALUES\n                    (@barangayId, @programName, @category, @allocatedBudget, @status, @startDate, @endDate, @notes, @userId, @userId)", delegate(MySqlCommand cmd)
			{
				AddProgramParameters(cmd, sanitized);
			}).ConfigureAwait(continueOnCapturedContext: false);
			AuditTrailService.Log("Ayuda", "ayuda_program", sanitized.ProgramName, "CREATE", null, sanitized, "Ayuda budget program created.");
		}
	}

	public async Task DeleteProgramAsync(int programId)
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		AyudaProgramRecord before = await GetProgramAsync(programId).ConfigureAwait(continueOnCapturedContext: false);
		if (before == null)
		{
			throw new InvalidOperationException("The selected ayuda program could not be found.");
		}
		if (await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\n                  FROM ayuda_release\n                  WHERE program_id = @programId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@programId", (object)programId);
		}).ConfigureAwait(continueOnCapturedContext: false) > 0)
		{
			throw new InvalidOperationException("Programs with recorded ayuda releases cannot be deleted.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM ayuda_program\n                  WHERE program_id = @programId\n                    AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@programId", (object)programId);
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
		AuditTrailService.Log("Ayuda", "ayuda_program", programId, "DELETE", before, null, "Ayuda budget program deleted.");
	}

	public async Task<AyudaReleaseRecord?> GetReleaseAsync(int releaseId)
	{
		if (releaseId <= 0)
		{
			return null;
		}
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n                SELECT ar.release_id,\n                       COALESCE(ar.batch_id, 0) AS batch_id,\n                       COALESCE(arb.batch_reference, COALESCE(ar.batch_reference, ar.reference_no)) AS batch_reference,\n                       COALESCE(arb.report_file_path, '') AS report_file_path,\n                       COALESCE(\n                           arb.beneficiary_count,\n                           CASE\n                               WHEN ar.batch_id IS NULL THEN 1\n                               ELSE (\n                                   SELECT COUNT(*)\n                                   FROM ayuda_release sibling\n                                   WHERE sibling.batch_id = ar.batch_id\n                               )\n                           END,\n                           1\n                       ) AS beneficiary_count,\n                       ar.program_id,\n                       ap.program_name,\n                       ar.resident_id,\n                       TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                       COALESCE(r.contact_no, '') AS contact_no,\n                       ar.reference_no,\n                       IFNULL(ar.amount, 0.00) AS amount,\n                       ar.released_at,\n                       UPPER(COALESCE(ar.release_status, 'RELEASED')) AS release_status,\n                       COALESCE(arb.notes, ar.notes, '') AS notes\n                FROM ayuda_release ar\n                INNER JOIN ayuda_program ap ON ap.program_id = ar.program_id\n                LEFT JOIN ayuda_release_batch arb ON arb.batch_id = ar.batch_id\n                LEFT JOIN resident r ON r.resident_id = ar.resident_id\n                WHERE ar.release_id = @releaseId\n                  AND ap.barangay_id = @barangayId\n                LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@releaseId", (object)releaseId);
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
		return (dataTable.Rows.Count == 0) ? null : MapReleaseRecord(dataTable.Rows[0]);
	}

	public async Task<AyudaBatchReleaseResult> SaveBatchReleaseAsync(int programId, DateTime releasedAt, string? notes, IReadOnlyCollection<AyudaBeneficiaryDraft> beneficiaries)
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		List<AyudaBeneficiaryDraft> stagedBeneficiaries = SanitizeBeneficiaries(beneficiaries);
		AyudaProgramRecord ayudaProgramRecord = await GetProgramAsync(programId).ConfigureAwait(continueOnCapturedContext: false);
		if (ayudaProgramRecord == null)
		{
			throw new InvalidOperationException("The selected ayuda program could not be found.");
		}
		if (!string.Equals(ayudaProgramRecord.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Only active ayuda programs can release funds.");
		}
		decimal num = stagedBeneficiaries.Sum((AyudaBeneficiaryDraft item) => item.Amount);
		if (num > ayudaProgramRecord.RemainingBudget)
		{
			throw new InvalidOperationException($"Insufficient remaining budget. Available: PHP {ayudaProgramRecord.RemainingBudget:N2}.");
		}
		string batchReference = BuildBatchReferenceNumber();
		string notes2 = NormalizeBatchNotes(notes, stagedBeneficiaries.Count);
		AyudaBatchReleaseResult ayudaBatchReleaseResult = ((!ShouldUseOfflineMode()) ? (await SaveBatchReleaseOnlineAsync(programId, releasedAt, notes2, stagedBeneficiaries, batchReference, num).ConfigureAwait(continueOnCapturedContext: false)) : (await SaveBatchReleaseOfflineAsync(programId, releasedAt, notes2, stagedBeneficiaries, batchReference, num).ConfigureAwait(continueOnCapturedContext: false)));
		AyudaBatchReleaseResult result = ayudaBatchReleaseResult;
		try
		{
			AyudaBatchReleaseResult ayudaBatchReleaseResult2 = result;
			ayudaBatchReleaseResult2.ReportFilePath = await GenerateOrRefreshBatchReportAsync(result.BatchId).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Ayuda batch report generation failed after saving releases.", ex);
		}
		AuditTrailService.Log("Ayuda", "ayuda_release_batch", result.BatchId, "CREATE", null, new { result.BatchReference, result.BeneficiaryCount, result.TotalAmount, result.ReportFilePath }, "Ayuda batch release saved.");
		return result;
	}

	public async Task UpdateReleaseAsync(AyudaReleaseRecord record)
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		AyudaReleaseRecord sanitized = SanitizeReleaseRecord(record);
		AyudaReleaseRecord before = await GetReleaseAsync(sanitized.ReleaseId).ConfigureAwait(continueOnCapturedContext: false);
		if (before == null)
		{
			throw new InvalidOperationException("The selected ayuda release could not be found.");
		}
		if (before.BatchId > 0 && before.BatchBeneficiaryCount > 1 && sanitized.ProgramId != before.ProgramId)
		{
			throw new InvalidOperationException("This release belongs to a multi-beneficiary batch. Delete and recreate the batch to move it to another program.");
		}
		AyudaProgramRecord obj = (await GetProgramAsync(sanitized.ProgramId).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("The selected ayuda program could not be found.");
		if (!string.Equals(obj.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Only active ayuda programs can release funds.");
		}
		decimal remainingBudget = obj.RemainingBudget;
		if (before.ProgramId == sanitized.ProgramId && !string.Equals(before.ReleaseStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
		{
			remainingBudget += before.Amount;
		}
		if (sanitized.Amount > remainingBudget)
		{
			throw new InvalidOperationException($"Insufficient remaining budget. Available: PHP {remainingBudget:N2}.");
		}
		if (before.BatchId > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE ayuda_release\n                      SET released_at = @releasedAt,\n                          notes = @notes\n                      WHERE batch_id = @batchId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@releasedAt", (object)sanitized.ReleasedAt);
				cmd.Parameters.AddWithValue("@notes", NormalizeNullable(sanitized.Notes));
				cmd.Parameters.AddWithValue("@batchId", (object)before.BatchId);
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE ayuda_release\n                  SET program_id = @programId,\n                      resident_id = @residentId,\n                      amount = @amount,\n                      released_at = @releasedAt,\n                      notes = @notes\n                  WHERE release_id = @releaseId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@programId", (object)sanitized.ProgramId);
			cmd.Parameters.AddWithValue("@residentId", (object)sanitized.ResidentId);
			cmd.Parameters.AddWithValue("@amount", (object)sanitized.Amount);
			cmd.Parameters.AddWithValue("@releasedAt", (object)sanitized.ReleasedAt);
			cmd.Parameters.AddWithValue("@notes", NormalizeNullable(sanitized.Notes));
			cmd.Parameters.AddWithValue("@releaseId", (object)sanitized.ReleaseId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (before.BatchId > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE ayuda_release_batch\n                      SET program_id = @programId,\n                          release_date = @releasedAt,\n                          notes = @notes,\n                          updated_at = CURRENT_TIMESTAMP\n                      WHERE batch_id = @batchId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@programId", (object)sanitized.ProgramId);
				cmd.Parameters.AddWithValue("@releasedAt", (object)sanitized.ReleasedAt);
				cmd.Parameters.AddWithValue("@notes", NormalizeNullable(sanitized.Notes));
				cmd.Parameters.AddWithValue("@batchId", (object)before.BatchId);
			}).ConfigureAwait(continueOnCapturedContext: false);
			await RefreshBatchSummaryAsync(before.BatchId).ConfigureAwait(continueOnCapturedContext: false);
			await GenerateOrRefreshBatchReportAsync(before.BatchId).ConfigureAwait(continueOnCapturedContext: false);
		}
		AuditTrailService.Log("Ayuda", "ayuda_release", sanitized.ReleaseId, "UPDATE", before, sanitized, "Ayuda release updated.");
	}

	public async Task DeleteReleaseAsync(int releaseId)
	{
		await EnsureSchemaReadyAsync().ConfigureAwait(continueOnCapturedContext: false);
		AyudaReleaseRecord before = await GetReleaseAsync(releaseId).ConfigureAwait(continueOnCapturedContext: false);
		if (before == null)
		{
			throw new InvalidOperationException("The selected ayuda release could not be found.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM ayuda_release WHERE release_id = @releaseId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@releaseId", (object)releaseId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (before.BatchId > 0)
		{
			if (await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\n                      FROM ayuda_release\n                      WHERE batch_id = @batchId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@batchId", (object)before.BatchId);
			}).ConfigureAwait(continueOnCapturedContext: false) > 0)
			{
				await RefreshBatchSummaryAsync(before.BatchId).ConfigureAwait(continueOnCapturedContext: false);
				await GenerateOrRefreshBatchReportAsync(before.BatchId).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM ayuda_release_batch WHERE batch_id = @batchId", delegate(MySqlCommand cmd)
				{
					cmd.Parameters.AddWithValue("@batchId", (object)before.BatchId);
				}).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		AuditTrailService.Log("Ayuda", "ayuda_release", releaseId, "DELETE", before, null, "Ayuda release deleted.");
	}

	public async Task CreateProgramAsync(string programName, string category, decimal allocatedBudget, string status, DateTime? startDate, DateTime? endDate, string? notes)
	{
		await SaveProgramAsync(new AyudaProgramRecord
		{
			ProgramName = programName,
			Category = category,
			AllocatedBudget = allocatedBudget,
			Status = status,
			StartDate = startDate,
			EndDate = endDate,
			Notes = (notes ?? string.Empty)
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<string> ReleaseAyudaAsync(int programId, int residentId, string residentName, decimal amount, DateTime releasedAt, string? notes)
	{
		return (await SaveBatchReleaseAsync(programId, releasedAt, notes, new AyudaBeneficiaryDraft[1]
		{
			new AyudaBeneficiaryDraft
			{
				ResidentId = residentId,
				ResidentName = residentName,
				Amount = amount
			}
		}).ConfigureAwait(continueOnCapturedContext: false)).BatchReference;
	}

	private async Task<AyudaBatchReleaseResult> SaveBatchReleaseOnlineAsync(int programId, DateTime releasedAt, string notes, IReadOnlyList<AyudaBeneficiaryDraft> beneficiaries, string batchReference, decimal totalAmount)
	{
		MySqlConnection conn = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)conn).OpenAsync().ConfigureAwait(continueOnCapturedContext: false);
			MySqlTransaction tx = conn.BeginTransaction();
			try
			{
				int num;
				_ = num - 1;
				_ = 1;
				try
				{
					int batchId = await InsertBatchOnlineAsync(conn, tx, programId, releasedAt, notes, beneficiaries.Count, totalAmount, batchReference).ConfigureAwait(continueOnCapturedContext: false);
					for (int index = 0; index < beneficiaries.Count; index++)
					{
						AyudaBeneficiaryDraft ayudaBeneficiaryDraft = beneficiaries[index];
						string text = BuildReleaseReferenceNumber(batchReference, index + 1);
						MySqlCommand insertRelease = new MySqlCommand("INSERT INTO ayuda_release\n                            (program_id, batch_id, resident_id, batch_reference, reference_no, amount, released_at, release_status, notes, created_by_user_id)\n                          VALUES\n                            (@programId, @batchId, @residentId, @batchReference, @referenceNo, @amount, @releasedAt, 'RELEASED', @notes, @userId)", conn, tx);
						try
						{
							insertRelease.Parameters.AddWithValue("@programId", (object)programId);
							insertRelease.Parameters.AddWithValue("@batchId", (object)batchId);
							insertRelease.Parameters.AddWithValue("@residentId", (object)ayudaBeneficiaryDraft.ResidentId);
							insertRelease.Parameters.AddWithValue("@batchReference", (object)batchReference);
							insertRelease.Parameters.AddWithValue("@referenceNo", (object)text);
							insertRelease.Parameters.AddWithValue("@amount", (object)ayudaBeneficiaryDraft.Amount);
							insertRelease.Parameters.AddWithValue("@releasedAt", (object)releasedAt);
							insertRelease.Parameters.AddWithValue("@notes", NormalizeNullable(notes));
							insertRelease.Parameters.AddWithValue("@userId", GetUserIdOrNull());
							await ((DbCommand)(object)insertRelease).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
						}
						finally
						{
							((IDisposable)insertRelease)?.Dispose();
						}
					}
					((DbTransaction)(object)tx).Commit();
					return new AyudaBatchReleaseResult
					{
						BatchId = batchId,
						BatchReference = batchReference,
						BeneficiaryCount = beneficiaries.Count,
						TotalAmount = totalAmount
					};
				}
				catch
				{
					((DbTransaction)(object)tx).Rollback();
					throw;
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

	private async Task<AyudaBatchReleaseResult> SaveBatchReleaseOfflineAsync(int programId, DateTime releasedAt, string notes, IReadOnlyList<AyudaBeneficiaryDraft> beneficiaries, string batchReference, decimal totalAmount)
	{
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO ayuda_release_batch\n                    (barangay_id, program_id, batch_reference, release_date, total_amount, beneficiary_count, notes, created_by_user_id)\n                VALUES\n                    (@barangayId, @programId, @batchReference, @releaseDate, @totalAmount, @beneficiaryCount, @notes, @userId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
			cmd.Parameters.AddWithValue("@programId", (object)programId);
			cmd.Parameters.AddWithValue("@batchReference", (object)batchReference);
			cmd.Parameters.AddWithValue("@releaseDate", (object)releasedAt);
			cmd.Parameters.AddWithValue("@totalAmount", (object)totalAmount);
			cmd.Parameters.AddWithValue("@beneficiaryCount", (object)beneficiaries.Count);
			cmd.Parameters.AddWithValue("@notes", NormalizeNullable(notes));
			cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
		}).ConfigureAwait(continueOnCapturedContext: false);
		int batchId = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT batch_id\n                  FROM ayuda_release_batch\n                  WHERE batch_reference = @batchReference\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@batchReference", (object)batchReference);
		}).ConfigureAwait(continueOnCapturedContext: false);
		for (int index = 0; index < beneficiaries.Count; index++)
		{
			AyudaBeneficiaryDraft beneficiary = beneficiaries[index];
			string referenceNo = BuildReleaseReferenceNumber(batchReference, index + 1);
			await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO ayuda_release\n                        (program_id, batch_id, resident_id, batch_reference, reference_no, amount, released_at, release_status, notes, created_by_user_id)\n                      VALUES\n                        (@programId, @batchId, @residentId, @batchReference, @referenceNo, @amount, @releasedAt, 'RELEASED', @notes, @userId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@programId", (object)programId);
				cmd.Parameters.AddWithValue("@batchId", (object)batchId);
				cmd.Parameters.AddWithValue("@residentId", (object)beneficiary.ResidentId);
				cmd.Parameters.AddWithValue("@batchReference", (object)batchReference);
				cmd.Parameters.AddWithValue("@referenceNo", (object)referenceNo);
				cmd.Parameters.AddWithValue("@amount", (object)beneficiary.Amount);
				cmd.Parameters.AddWithValue("@releasedAt", (object)releasedAt);
				cmd.Parameters.AddWithValue("@notes", NormalizeNullable(notes));
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		return new AyudaBatchReleaseResult
		{
			BatchId = batchId,
			BatchReference = batchReference,
			BeneficiaryCount = beneficiaries.Count,
			TotalAmount = totalAmount
		};
	}

	private async Task<int> InsertBatchOnlineAsync(MySqlConnection connection, MySqlTransaction transaction, int programId, DateTime releasedAt, string notes, int beneficiaryCount, decimal totalAmount, string batchReference)
	{
		MySqlCommand insertBatch = new MySqlCommand("INSERT INTO ayuda_release_batch\n                    (barangay_id, program_id, batch_reference, release_date, total_amount, beneficiary_count, notes, created_by_user_id)\n                  VALUES\n                    (@barangayId, @programId, @batchReference, @releaseDate, @totalAmount, @beneficiaryCount, @notes, @userId)", connection, transaction);
		try
		{
			insertBatch.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
			insertBatch.Parameters.AddWithValue("@programId", (object)programId);
			insertBatch.Parameters.AddWithValue("@batchReference", (object)batchReference);
			insertBatch.Parameters.AddWithValue("@releaseDate", (object)releasedAt);
			insertBatch.Parameters.AddWithValue("@totalAmount", (object)totalAmount);
			insertBatch.Parameters.AddWithValue("@beneficiaryCount", (object)beneficiaryCount);
			insertBatch.Parameters.AddWithValue("@notes", NormalizeNullable(notes));
			insertBatch.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			await ((DbCommand)(object)insertBatch).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
			return Convert.ToInt32(insertBatch.LastInsertedId, CultureInfo.InvariantCulture);
		}
		finally
		{
			((IDisposable)insertBatch)?.Dispose();
		}
	}

	private async Task RefreshBatchSummaryAsync(int batchId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("SELECT MIN(program_id) AS program_id,\n                         MIN(released_at) AS release_date,\n                         IFNULL(SUM(CASE WHEN UPPER(COALESCE(release_status, 'RELEASED')) <> 'CANCELLED' THEN IFNULL(amount, 0.00) ELSE 0.00 END), 0.00) AS total_amount,\n                         COUNT(*) AS beneficiary_count\n                  FROM ayuda_release\n                  WHERE batch_id = @batchId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@batchId", (object)batchId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (dataTable.Rows.Count != 0)
		{
			DataRow dataRow = dataTable.Rows[0];
			int programId = ((dataRow["program_id"] != DBNull.Value) ? Convert.ToInt32(dataRow["program_id"], CultureInfo.InvariantCulture) : 0);
			DateTime releaseDate = ReadDateTime(dataRow, "release_date") ?? DateTime.Today;
			decimal totalAmount = ((dataRow["total_amount"] == DBNull.Value) ? 0m : Convert.ToDecimal(dataRow["total_amount"], CultureInfo.InvariantCulture));
			int beneficiaryCount = ((dataRow["beneficiary_count"] != DBNull.Value) ? Convert.ToInt32(dataRow["beneficiary_count"], CultureInfo.InvariantCulture) : 0);
			await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE ayuda_release_batch\n                  SET program_id = @programId,\n                      release_date = @releaseDate,\n                      total_amount = @totalAmount,\n                      beneficiary_count = @beneficiaryCount,\n                      updated_at = CURRENT_TIMESTAMP\n                  WHERE batch_id = @batchId", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@programId", (object)programId);
				cmd.Parameters.AddWithValue("@releaseDate", (object)releaseDate);
				cmd.Parameters.AddWithValue("@totalAmount", (object)totalAmount);
				cmd.Parameters.AddWithValue("@beneficiaryCount", (object)beneficiaryCount);
				cmd.Parameters.AddWithValue("@batchId", (object)batchId);
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task<string> GenerateOrRefreshBatchReportAsync(int batchId)
	{
		AyudaReleaseReportData ayudaReleaseReportData = await LoadBatchReportDataAsync(batchId).ConfigureAwait(continueOnCapturedContext: false);
		if (ayudaReleaseReportData == null || ayudaReleaseReportData.Beneficiaries.Count == 0)
		{
			return string.Empty;
		}
		string filePath = AyudaReleaseReportService.GeneratePdf(ayudaReleaseReportData);
		await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE ayuda_release_batch\n                  SET report_file_path = @reportFilePath,\n                      updated_at = CURRENT_TIMESTAMP\n                  WHERE batch_id = @batchId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@reportFilePath", (object)filePath);
			cmd.Parameters.AddWithValue("@batchId", (object)batchId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		return filePath;
	}

	private async Task<AyudaReleaseReportData?> LoadBatchReportDataAsync(int batchId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("SELECT arb.batch_id,\n                         arb.batch_reference,\n                         ap.program_name,\n                         ap.category,\n                         arb.release_date,\n                         IFNULL(arb.total_amount, 0.00) AS total_amount,\n                         IFNULL(arb.beneficiary_count, 0) AS beneficiary_count,\n                         COALESCE(arb.notes, '') AS notes,\n                         COALESCE(arb.report_file_path, '') AS report_file_path\n                  FROM ayuda_release_batch arb\n                  INNER JOIN ayuda_program ap ON ap.program_id = arb.program_id\n                  WHERE arb.batch_id = @batchId\n                    AND arb.barangay_id = @barangayId\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@batchId", (object)batchId);
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		DataRow row = dataTable.Rows[0];
		AyudaReleaseReportData reportData = new AyudaReleaseReportData
		{
			BatchId = ReadInt(row, "batch_id"),
			BatchReference = ReadString(row, "batch_reference"),
			ProgramName = ReadString(row, "program_name"),
			Category = ReadString(row, "category"),
			ReleaseDate = (ReadDateTime(row, "release_date") ?? DateTime.Today),
			TotalAmount = ReadDecimal(row, "total_amount"),
			BeneficiaryCount = ReadInt(row, "beneficiary_count"),
			Notes = ReadString(row, "notes"),
			ReportFilePath = ReadString(row, "report_file_path"),
			GeneratedBy = GetUserDisplayName()
		};
		foreach (DataRow row2 in (await DatabaseManagerAsync.LoadTableAsync("SELECT ar.reference_no,\n                         TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name, r.suffix)) AS resident_name,\n                         COALESCE(r.contact_no, '') AS contact_no,\n                         IFNULL(ar.amount, 0.00) AS amount\n                  FROM ayuda_release ar\n                  LEFT JOIN resident r ON r.resident_id = ar.resident_id\n                  WHERE ar.batch_id = @batchId\n                  ORDER BY ar.release_id ASC", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@batchId", (object)batchId);
		}).ConfigureAwait(continueOnCapturedContext: false)).Rows)
		{
			reportData.Beneficiaries.Add(new AyudaReleaseReportBeneficiaryRow
			{
				ReferenceNo = ReadString(row2, "reference_no"),
				ResidentName = ReadString(row2, "resident_name"),
				ContactNo = ReadString(row2, "contact_no"),
				Amount = ReadDecimal(row2, "amount")
			});
		}
		return reportData;
	}

	private static AyudaProgramRecord MapProgramRecord(DataRow row)
	{
		return new AyudaProgramRecord
		{
			ProgramId = ReadInt(row, "program_id"),
			ProgramName = ReadString(row, "program_name"),
			Category = ReadString(row, "category"),
			AllocatedBudget = ReadDecimal(row, "allocated_budget"),
			SpentBudget = ReadDecimal(row, "spent_budget"),
			RemainingBudget = ReadDecimal(row, "remaining_budget"),
			Status = NormalizeProgramStatus(ReadString(row, "status")),
			StartDate = ReadDateTime(row, "start_date"),
			EndDate = ReadDateTime(row, "end_date"),
			Notes = ReadString(row, "notes")
		};
	}

	private static AyudaReleaseRecord MapReleaseRecord(DataRow row)
	{
		return new AyudaReleaseRecord
		{
			ReleaseId = ReadInt(row, "release_id"),
			BatchId = ReadInt(row, "batch_id"),
			BatchReference = ReadString(row, "batch_reference"),
			ReportFilePath = ReadString(row, "report_file_path"),
			BatchBeneficiaryCount = ReadInt(row, "beneficiary_count"),
			ProgramId = ReadInt(row, "program_id"),
			ProgramName = ReadString(row, "program_name"),
			ResidentId = ReadInt(row, "resident_id"),
			ResidentName = ReadString(row, "resident_name"),
			ResidentContactNo = ReadString(row, "contact_no"),
			ReferenceNo = ReadString(row, "reference_no"),
			Amount = ReadDecimal(row, "amount"),
			ReleasedAt = (ReadDateTime(row, "released_at") ?? DateTime.Today),
			ReleaseStatus = ReadString(row, "release_status"),
			Notes = ReadString(row, "notes")
		};
	}

	private static AyudaProgramRecord SanitizeProgramRecord(AyudaProgramRecord record)
	{
		if (record == null)
		{
			throw new InvalidOperationException("Program details are required.");
		}
		string programName = NormalizeRequired(record.ProgramName, "Program name is required.");
		string category = (string.IsNullOrWhiteSpace(record.Category) ? "General Assistance" : record.Category.Trim());
		decimal num = decimal.Round(record.AllocatedBudget, 2, MidpointRounding.AwayFromZero);
		if (num <= 0m)
		{
			throw new InvalidOperationException("Allocated budget must be greater than zero.");
		}
		DateTime? startDate = record.StartDate?.Date;
		DateTime? endDate = record.EndDate?.Date;
		if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
		{
			throw new InvalidOperationException("End date cannot be earlier than the start date.");
		}
		return new AyudaProgramRecord
		{
			ProgramId = record.ProgramId,
			ProgramName = programName,
			Category = category,
			AllocatedBudget = num,
			Status = NormalizeProgramStatus(record.Status),
			StartDate = startDate,
			EndDate = endDate,
			Notes = NormalizeOptional(record.Notes)
		};
	}

	private static AyudaReleaseRecord SanitizeReleaseRecord(AyudaReleaseRecord record)
	{
		if (record == null)
		{
			throw new InvalidOperationException("Release details are required.");
		}
		if (record.ReleaseId <= 0)
		{
			throw new InvalidOperationException("Release ID is required.");
		}
		if (record.ProgramId <= 0)
		{
			throw new InvalidOperationException("Select an ayuda program first.");
		}
		if (record.ResidentId <= 0)
		{
			throw new InvalidOperationException("Select a resident beneficiary first.");
		}
		decimal num = decimal.Round(record.Amount, 2, MidpointRounding.AwayFromZero);
		if (num <= 0m)
		{
			throw new InvalidOperationException("Release amount must be greater than zero.");
		}
		return new AyudaReleaseRecord
		{
			ReleaseId = record.ReleaseId,
			ProgramId = record.ProgramId,
			ResidentId = record.ResidentId,
			ResidentName = NormalizeOptional(record.ResidentName),
			Amount = num,
			ReleasedAt = ((record.ReleasedAt == default(DateTime)) ? DateTime.Today : record.ReleasedAt),
			Notes = NormalizeOptional(record.Notes)
		};
	}

	private static List<AyudaBeneficiaryDraft> SanitizeBeneficiaries(IReadOnlyCollection<AyudaBeneficiaryDraft> beneficiaries)
	{
		if (beneficiaries == null || beneficiaries.Count == 0)
		{
			throw new InvalidOperationException("Add at least one beneficiary before posting ayuda.");
		}
		HashSet<int> hashSet = new HashSet<int>();
		List<AyudaBeneficiaryDraft> list = new List<AyudaBeneficiaryDraft>(beneficiaries.Count);
		foreach (AyudaBeneficiaryDraft beneficiary in beneficiaries)
		{
			if (beneficiary == null || beneficiary.ResidentId <= 0)
			{
				throw new InvalidOperationException("Each ayuda entry must target a valid resident.");
			}
			decimal num = decimal.Round(beneficiary.Amount, 2, MidpointRounding.AwayFromZero);
			if (num <= 0m)
			{
				throw new InvalidOperationException("Each ayuda entry must have an amount greater than zero.");
			}
			if (!hashSet.Add(beneficiary.ResidentId))
			{
				throw new InvalidOperationException("The same resident cannot appear twice in the same ayuda batch.");
			}
			list.Add(new AyudaBeneficiaryDraft
			{
				ResidentId = beneficiary.ResidentId,
				ResidentName = NormalizeOptional(beneficiary.ResidentName),
				ContactNo = NormalizeOptional(beneficiary.ContactNo),
				Amount = num
			});
		}
		return list;
	}

	private static void AddProgramParameters(MySqlCommand cmd, AyudaProgramRecord record)
	{
		cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		cmd.Parameters.AddWithValue("@programName", (object)record.ProgramName);
		cmd.Parameters.AddWithValue("@category", (object)record.Category);
		cmd.Parameters.AddWithValue("@allocatedBudget", (object)record.AllocatedBudget);
		cmd.Parameters.AddWithValue("@status", (object)record.Status);
		cmd.Parameters.AddWithValue("@startDate", NormalizeNullable(record.StartDate));
		cmd.Parameters.AddWithValue("@endDate", NormalizeNullable(record.EndDate));
		cmd.Parameters.AddWithValue("@notes", NormalizeNullable(record.Notes));
		cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
	}

	private static async Task EnsureSchemaReadyAsync()
	{
		if (ShouldUseOfflineMode())
		{
			if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
			{
				throw new InvalidOperationException("Offline ayuda storage is not available right now.");
			}
			if (!OfflineDatabaseSupport.IsOffline)
			{
				OfflineDatabaseSupport.ActivateOfflineMode();
			}
		}
		else
		{
			await Task.Run(delegate
			{
				SchemaGuard.EnsureDatabaseReady();
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private static bool ShouldUseOfflineMode()
	{
		if (!OfflineDatabaseSupport.IsOffline)
		{
			return DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false);
		}
		return true;
	}

	private static string BuildBatchReferenceNumber()
	{
		return $"AYB-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
	}

	private static string BuildReleaseReferenceNumber(string batchReference, int sequence)
	{
		string text = sequence.ToString("00", CultureInfo.InvariantCulture);
		if (batchReference.StartsWith("AYB-", StringComparison.OrdinalIgnoreCase))
		{
			return "AYD-" + batchReference.Substring(4, batchReference.Length - 4) + "-" + text;
		}
		return $"AYD-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}-{text}";
	}

	private static string NormalizeBatchNotes(string? notes, int beneficiaryCount)
	{
		string text = NormalizeOptional(notes);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return $"Ayuda batch release for {beneficiaryCount:N0} beneficiary(ies).";
	}

	private static string NormalizeProgramStatus(string? status)
	{
		return NormalizeOptional(status).ToUpperInvariant() switch
		{
			"ACTIVE" => "ACTIVE", 
			"PAUSED" => "PAUSED", 
			"CLOSED" => "CLOSED", 
			_ => "ACTIVE", 
		};
	}

	private static string NormalizeRequired(string? value, string message)
	{
		string text = NormalizeOptional(value);
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException(message);
		}
		return text;
	}

	private static string NormalizeOptional(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return string.Empty;
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

	private static object GetUserIdOrNull()
	{
		if (UserSession.UserId <= 0)
		{
			return DBNull.Value;
		}
		return UserSession.UserId;
	}

	private static int ResolveBarangayId()
	{
		if (UserSession.BarangayId <= 0)
		{
			return 1;
		}
		return UserSession.BarangayId;
	}

	private static string GetUserDisplayName()
	{
		if (!string.IsNullOrWhiteSpace(UserSession.Username))
		{
			return UserSession.Username.Trim();
		}
		return "Barangay Staff";
	}

	private static int ReadInt(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0;
		}
		return Convert.ToInt32(row[columnName], CultureInfo.InvariantCulture);
	}

	private static decimal ReadDecimal(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0m;
		}
		return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
	}

	private static string ReadString(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(row[columnName], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
	}

	private static DateTime? ReadDateTime(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return null;
		}
		object obj = row[columnName];
		if (obj is DateTime)
		{
			return (DateTime)obj;
		}
		if (!DateTime.TryParse(Convert.ToString(obj, CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
		{
			return null;
		}
		return result;
	}
}
