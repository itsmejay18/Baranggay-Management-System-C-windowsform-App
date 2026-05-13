using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class BlotterRepository
{
	public Task<DataTable> LoadCaseListAsync(int barangayId, CancellationToken cancellationToken = default(CancellationToken))
	{
		int targetBarangayId = HouseholdRepository.ResolveBarangayId(barangayId);
		return DatabaseManagerAsync.LoadTableAsync("\nSELECT cr.case_id,\n       COALESCE(\n           NULLIF(TRIM(cr.case_no), ''),\n           CONCAT('BLT-', DATE_FORMAT(COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()), '%Y'), '-', LPAD(cr.case_id, 5, '0'))\n       ) AS case_no,\n       COALESCE(\n           NULLIF(TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name)), ''),\n           CASE\n               WHEN cr.complainant_id IS NOT NULL THEN CONCAT('Resident #', cr.complainant_id)\n               ELSE 'Unassigned resident'\n           END\n       ) AS complainant_name,\n       COALESCE(NULLIF(TRIM(cr.respondent_name), ''), 'Unspecified respondent') AS respondent_name,\n       COALESCE(NULLIF(TRIM(cr.incident_type), ''), 'General') AS incident_type,\n       DATE_FORMAT(COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)), '%Y-%m-%d') AS incident_date,\n       UPPER(COALESCE(cr.status, 'ONGOING')) AS status\nFROM case_record cr\nLEFT JOIN resident r ON r.resident_id = cr.complainant_id\nWHERE cr.barangay_id = @barangayId\nORDER BY COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)) DESC,\n         cr.case_id DESC\nLIMIT 300;", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)targetBarangayId);
		}, cancellationToken);
	}

	public async Task<BlotterDto?> LoadCaseAsync(int caseId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MySqlConnection conn = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			BlotterDto dto = new BlotterDto();
			MySqlCommand cmd = new MySqlCommand("\nSELECT cr.case_id,\n       COALESCE(\n           NULLIF(TRIM(cr.case_no), ''),\n           CONCAT('BLT-', DATE_FORMAT(COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()), '%Y'), '-', LPAD(cr.case_id, 5, '0'))\n       ) AS case_no,\n       cr.complainant_id,\n       COALESCE(NULLIF(TRIM(CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name)), ''), '') AS complainant_name,\n       CONCAT_WS(', ',\n           NULLIF(TRIM(COALESCE(h.house_no, '')), ''),\n           NULLIF(TRIM(COALESCE(h.street, '')), ''),\n           NULLIF(TRIM(COALESCE(h.subdivision, '')), ''),\n           NULLIF(TRIM(COALESCE(p.name, '')), ''),\n           NULLIF(TRIM(COALESCE(h.address_note, '')), '')\n       ) AS complainant_address,\n       cr.respondent_resident_id,\n       COALESCE(cr.respondent_name, '') AS respondent_name,\n       COALESCE(cr.incident_type, '') AS incident_type,\n       cr.incident_date,\n       cr.incident_time,\n       COALESCE(cr.incident_location, '') AS incident_location,\n       COALESCE(cr.witness_names, '') AS witness_names,\n       COALESCE(cr.action_taken, '') AS action_taken,\n       COALESCE(cr.resolution_details, '') AS resolution_details,\n       COALESCE(cr.incident_details, '') AS incident_details,\n       UPPER(COALESCE(cr.status, 'ONGOING')) AS status,\n       COALESCE(cr.referral_destination, '') AS referral_destination,\n       COALESCE(cr.closure_notes, '') AS closure_notes,\n       COALESCE(cr.ai_summary, '') AS ai_summary,\n       COALESCE(cr.ai_category, '') AS ai_category,\n       COALESCE(cr.ai_risk_level, '') AS ai_risk_level\nFROM case_record cr\nLEFT JOIN resident r ON r.resident_id = cr.complainant_id\nLEFT JOIN household h ON h.household_id = r.household_id\nLEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\nWHERE cr.case_id = @caseId\nLIMIT 1;", conn);
			try
			{
				cmd.Parameters.AddWithValue("@caseId", (object)caseId);
				MySqlDataReader reader = (MySqlDataReader)(await ((DbCommand)(object)cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
				try
				{
					if (!(await ((DbDataReader)(object)reader).ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
					{
						return null;
					}
					int ordinal = ((DbDataReader)(object)reader).GetOrdinal("incident_date");
					int ordinal2 = ((DbDataReader)(object)reader).GetOrdinal("incident_time");
					dto = new BlotterDto
					{
						CaseId = Convert.ToInt32(((DbDataReader)(object)reader)["case_id"]),
						CaseNo = (Convert.ToString(((DbDataReader)(object)reader)["case_no"]) ?? string.Empty),
						ComplainantId = ((((DbDataReader)(object)reader)["complainant_id"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)reader)["complainant_id"]) : 0),
						ComplainantName = (Convert.ToString(((DbDataReader)(object)reader)["complainant_name"]) ?? string.Empty),
						ComplainantAddress = (Convert.ToString(((DbDataReader)(object)reader)["complainant_address"]) ?? string.Empty),
						RespondentResidentId = ((((DbDataReader)(object)reader)["respondent_resident_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)reader)["respondent_resident_id"]))),
						RespondentName = (Convert.ToString(((DbDataReader)(object)reader)["respondent_name"]) ?? string.Empty),
						IncidentType = (Convert.ToString(((DbDataReader)(object)reader)["incident_type"]) ?? string.Empty),
						IncidentDate = (((DbDataReader)(object)reader).IsDBNull(ordinal) ? DateTime.Today : ((DbDataReader)(object)reader).GetDateTime(ordinal)),
						IncidentTime = (((DbDataReader)(object)reader).IsDBNull(ordinal2) ? ((TimeSpan?)null) : new TimeSpan?((TimeSpan)((DbDataReader)(object)reader)["incident_time"])),
						IncidentLocation = (Convert.ToString(((DbDataReader)(object)reader)["incident_location"]) ?? string.Empty),
						Witnesses = (Convert.ToString(((DbDataReader)(object)reader)["witness_names"]) ?? string.Empty),
						ActionTaken = (Convert.ToString(((DbDataReader)(object)reader)["action_taken"]) ?? string.Empty),
						ResolutionDetails = (Convert.ToString(((DbDataReader)(object)reader)["resolution_details"]) ?? string.Empty),
						IncidentDetails = (Convert.ToString(((DbDataReader)(object)reader)["incident_details"]) ?? string.Empty),
						Status = WorkflowRules.NormalizeBlotterStatus(Convert.ToString(((DbDataReader)(object)reader)["status"])),
						ReferralDestination = (Convert.ToString(((DbDataReader)(object)reader)["referral_destination"]) ?? string.Empty),
						ClosureNotes = (Convert.ToString(((DbDataReader)(object)reader)["closure_notes"]) ?? string.Empty),
						AiSummary = (Convert.ToString(((DbDataReader)(object)reader)["ai_summary"]) ?? string.Empty),
						AiCategory = (Convert.ToString(((DbDataReader)(object)reader)["ai_category"]) ?? string.Empty),
						AiRiskLevel = (Convert.ToString(((DbDataReader)(object)reader)["ai_risk_level"]) ?? string.Empty)
					};
				}
				finally
				{
					((IDisposable)reader)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)cmd)?.Dispose();
			}
			(dto.ScheduledMediationAt, dto.MediationVenue) = await LoadLatestHearingAsync(conn, caseId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return dto;
		}
		finally
		{
			((IDisposable)conn)?.Dispose();
		}
	}

	public async Task<BlotterResidentLookupItem?> GetResidentAsync(int residentId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MySqlConnection conn = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MySqlCommand cmd = new MySqlCommand("\nSELECT r.resident_id,\n       CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,\n       COALESCE(r.contact_no, '') AS contact_no,\n       COALESCE(h.house_no, '') AS house_no,\n       COALESCE(h.street, '') AS street,\n       COALESCE(h.subdivision, '') AS subdivision,\n       COALESCE(p.name, '') AS purok_name,\n       COALESCE(h.address_note, '') AS address_note\nFROM resident r\nLEFT JOIN household h ON h.household_id = r.household_id\nLEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\nWHERE r.resident_id = @residentId\nLIMIT 1;", conn);
			try
			{
				cmd.Parameters.AddWithValue("@residentId", (object)residentId);
				MySqlDataReader reader = (MySqlDataReader)(await ((DbCommand)(object)cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
				try
				{
					if (!(await ((DbDataReader)(object)reader).ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
					{
						return null;
					}
					return new BlotterResidentLookupItem
					{
						ResidentId = Convert.ToInt32(((DbDataReader)(object)reader)["resident_id"]),
						FullName = (Convert.ToString(((DbDataReader)(object)reader)["full_name"]) ?? string.Empty),
						ContactNo = (Convert.ToString(((DbDataReader)(object)reader)["contact_no"]) ?? string.Empty),
						Address = BuildAddress(Convert.ToString(((DbDataReader)(object)reader)["house_no"]), Convert.ToString(((DbDataReader)(object)reader)["street"]), Convert.ToString(((DbDataReader)(object)reader)["subdivision"]), Convert.ToString(((DbDataReader)(object)reader)["purok_name"]), Convert.ToString(((DbDataReader)(object)reader)["address_note"]))
					};
				}
				finally
				{
					((IDisposable)reader)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)cmd)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)conn)?.Dispose();
		}
	}

	public async Task<IReadOnlyList<BlotterResidentLookupItem>> SearchResidentsAsync(int barangayId, string? searchText, CancellationToken cancellationToken = default(CancellationToken))
	{
		int targetBarangayId = HouseholdRepository.ResolveBarangayId(barangayId);
		string search = (searchText ?? string.Empty).Trim();
		string like = "%" + search + "%";
		string exactId = search;
		List<BlotterResidentLookupItem> residents = new List<BlotterResidentLookupItem>();
		MySqlConnection conn = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MySqlCommand cmd = new MySqlCommand("\nSELECT r.resident_id,\n       CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,\n       COALESCE(r.contact_no, '') AS contact_no,\n       COALESCE(h.house_no, '') AS house_no,\n       COALESCE(h.street, '') AS street,\n       COALESCE(h.subdivision, '') AS subdivision,\n       COALESCE(p.name, '') AS purok_name,\n       COALESCE(h.address_note, '') AS address_note\nFROM resident r\nLEFT JOIN household h ON h.household_id = r.household_id\nLEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\nWHERE r.barangay_id = @barangayId\n  AND IFNULL(r.is_deleted, 0) = 0\n  AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n  AND (\n      @searchText = ''\n      OR CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @searchLike\n      OR COALESCE(r.contact_no, '') LIKE @searchLike\n      OR CAST(r.resident_id AS CHAR) = @searchId\n  )\nORDER BY r.last_name, r.first_name, r.middle_name\nLIMIT 60;", conn);
			try
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)targetBarangayId);
				cmd.Parameters.AddWithValue("@searchText", (object)search);
				cmd.Parameters.AddWithValue("@searchLike", (object)like);
				cmd.Parameters.AddWithValue("@searchId", (object)exactId);
				MySqlDataReader reader = (MySqlDataReader)(await ((DbCommand)(object)cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
				try
				{
					while (await ((DbDataReader)(object)reader).ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
					{
						residents.Add(new BlotterResidentLookupItem
						{
							ResidentId = Convert.ToInt32(((DbDataReader)(object)reader)["resident_id"]),
							FullName = (Convert.ToString(((DbDataReader)(object)reader)["full_name"]) ?? string.Empty),
							ContactNo = (Convert.ToString(((DbDataReader)(object)reader)["contact_no"]) ?? string.Empty),
							Address = BuildAddress(Convert.ToString(((DbDataReader)(object)reader)["house_no"]), Convert.ToString(((DbDataReader)(object)reader)["street"]), Convert.ToString(((DbDataReader)(object)reader)["subdivision"]), Convert.ToString(((DbDataReader)(object)reader)["purok_name"]), Convert.ToString(((DbDataReader)(object)reader)["address_note"]))
						});
					}
					return residents;
				}
				finally
				{
					((IDisposable)reader)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)cmd)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)conn)?.Dispose();
		}
	}

	public async Task<BlotterSaveResult> SaveCaseAsync(BlotterDto dto, CancellationToken cancellationToken = default(CancellationToken))
	{
		MySqlConnection conn = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MySqlTransaction tx = conn.BeginTransaction();
			try
			{
				MySqlCommand insert;
				if (dto.CaseId <= 0)
				{
					int num = await ResolveDefaultCaseTypeIdAsync(conn, tx, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					string normalizedStatus = WorkflowRules.NormalizeBlotterStatus(dto.Status);
					insert = BuildSaveCommand("\nINSERT INTO case_record\n    (barangay_id, case_type_id, case_no, date_filed, incident_date, incident_location, summary, status,\n     handled_by_user_id, complainant_id, respondent_resident_id, respondent_name, incident_type, incident_time,\n     witness_names, action_taken, resolution_details, incident_details, recorded_by)\nVALUES\n    (@barangayId, @caseTypeId, NULL, @dateFiled, @incidentDate, @incidentLocation, @summary, @status,\n     @handledBy, @complainantId, @respondentResidentId, @respondentName, @incidentType, @incidentTime,\n     @witnessNames, @actionTaken, @resolutionDetails, @incidentDetails, @recordedBy);", conn, tx, dto, normalizedStatus, isNewCase: true);
					try
					{
						((DbParameter)(object)insert.Parameters["@caseTypeId"]).Value = num;
						await ((DbCommand)(object)insert).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						int caseId = Convert.ToInt32(insert.LastInsertedId);
						string caseNo = ComposeCaseNumber(dto.CaseNo, dto.IncidentDate, caseId);
						await EnsureCaseNumberAsync(conn, tx, caseId, caseNo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						CaseTimelineService.LogTransactional(conn, tx, caseId, "FILED", "Blotter case filed", BuildFiledDetails(dto), null, normalizedStatus, (UserSession.UserId > 0) ? new int?(UserSession.UserId) : ((int?)null));
						((DbTransaction)(object)tx).Commit();
						return new BlotterSaveResult
						{
							CaseId = caseId,
							CaseNo = caseNo,
							Status = normalizedStatus
						};
					}
					finally
					{
						((IDisposable)insert)?.Dispose();
					}
				}
				string existingCaseNo = (string.IsNullOrWhiteSpace(dto.CaseNo) ? ComposeCaseNumber(null, dto.IncidentDate, dto.CaseId) : dto.CaseNo.Trim());
				insert = BuildSaveCommand("\nUPDATE case_record\nSET complainant_id = @complainantId,\n    respondent_resident_id = @respondentResidentId,\n    respondent_name = @respondentName,\n    incident_type = @incidentType,\n    incident_date = @incidentDate,\n    incident_time = @incidentTime,\n    incident_location = @incidentLocation,\n    summary = @summary,\n    witness_names = @witnessNames,\n    action_taken = @actionTaken,\n    resolution_details = @resolutionDetails,\n    incident_details = @incidentDetails,\n    case_no = @caseNo\nWHERE case_id = @caseId\n  AND barangay_id = @barangayId;", conn, tx, dto, WorkflowRules.NormalizeBlotterStatus(dto.Status), isNewCase: false);
				try
				{
					insert.Parameters.AddWithValue("@caseId", (object)dto.CaseId);
					insert.Parameters.AddWithValue("@caseNo", (object)existingCaseNo);
					if (await ((DbCommand)(object)insert).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) <= 0)
					{
						throw new InvalidOperationException("The selected blotter case could not be updated.");
					}
				}
				finally
				{
					((IDisposable)insert)?.Dispose();
				}
				CaseTimelineService.LogTransactional(conn, tx, dto.CaseId, "EDIT", "Case details updated", BuildUpdateDetails(dto), null, null, (UserSession.UserId > 0) ? new int?(UserSession.UserId) : ((int?)null));
				((DbTransaction)(object)tx).Commit();
				return new BlotterSaveResult
				{
					CaseId = dto.CaseId,
					CaseNo = existingCaseNo,
					Status = WorkflowRules.NormalizeBlotterStatus(dto.Status)
				};
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

	public async Task<BlotterSaveResult> UpdateStatusAsync(int caseId, string originalStatus, string currentStatus, string? resolutionDetails, string? referralDestination, string? closureNotes, CancellationToken cancellationToken = default(CancellationToken))
	{
		string normalizedStatus = WorkflowRules.NormalizeBlotterStatus(currentStatus);
		MySqlConnection conn = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MySqlTransaction tx = conn.BeginTransaction();
			try
			{
				MySqlCommand cmd = new MySqlCommand("\nUPDATE case_record\nSET status = @status,\n    resolution_details = @resolutionDetails,\n    referral_destination = @referralDestination,\n    closure_notes = @closureNotes,\n    closed_at = @closedAt,\n    closed_by_user_id = @closedBy\nWHERE case_id = @caseId;", conn);
				try
				{
					cmd.Transaction = tx;
					cmd.Parameters.AddWithValue("@status", (object)normalizedStatus);
					cmd.Parameters.AddWithValue("@resolutionDetails", ToDbValue(resolutionDetails));
					cmd.Parameters.AddWithValue("@referralDestination", ToDbValue(referralDestination));
					cmd.Parameters.AddWithValue("@closureNotes", ToDbValue(closureNotes));
					cmd.Parameters.AddWithValue("@closedAt", (normalizedStatus == "CLOSED") ? ((object)DateTime.Now) : DBNull.Value);
					cmd.Parameters.AddWithValue("@closedBy", (normalizedStatus == "CLOSED" && UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
					cmd.Parameters.AddWithValue("@caseId", (object)caseId);
					if (await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) <= 0)
					{
						throw new InvalidOperationException("The blotter status update could not be saved.");
					}
				}
				finally
				{
					((IDisposable)cmd)?.Dispose();
				}
				CaseTimelineService.LogTransactional(conn, tx, caseId, "STATUS", "Status updated to " + normalizedStatus, BuildStatusDetails(normalizedStatus, resolutionDetails, referralDestination, closureNotes), WorkflowRules.NormalizeBlotterStatus(originalStatus), normalizedStatus, (UserSession.UserId > 0) ? new int?(UserSession.UserId) : ((int?)null));
				((DbTransaction)(object)tx).Commit();
				return new BlotterSaveResult
				{
					CaseId = caseId,
					Status = normalizedStatus
				};
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

	public async Task ScheduleMediationAsync(int caseId, DateTime scheduleAt, string venue, CancellationToken cancellationToken = default(CancellationToken))
	{
		MySqlConnection conn = DBConnection.GetConnection();
		try
		{
			await ((DbConnection)(object)conn).OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			MySqlTransaction tx = conn.BeginTransaction();
			try
			{
				MySqlCommand cmd = new MySqlCommand("\nINSERT INTO case_hearing\n    (case_id, schedule_at, venue, status, created_by_user_id)\nVALUES\n    (@caseId, @scheduleAt, @venue, 'SCHEDULED', @createdBy);", conn);
				try
				{
					cmd.Transaction = tx;
					cmd.Parameters.AddWithValue("@caseId", (object)caseId);
					cmd.Parameters.AddWithValue("@scheduleAt", (object)scheduleAt);
					cmd.Parameters.AddWithValue("@venue", (object)venue.Trim());
					cmd.Parameters.AddWithValue("@createdBy", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
					await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					((IDisposable)cmd)?.Dispose();
				}
				CaseTimelineService.LogTransactional(conn, tx, caseId, "MEDIATION", "Mediation scheduled", $"Schedule: {scheduleAt:MMM dd, yyyy hh:mm tt}\nVenue: {venue.Trim()}", null, null, (UserSession.UserId > 0) ? new int?(UserSession.UserId) : ((int?)null));
				((DbTransaction)(object)tx).Commit();
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

	private static async Task<int> ResolveDefaultCaseTypeIdAsync(MySqlConnection conn, MySqlTransaction tx, CancellationToken cancellationToken)
	{
		MySqlCommand cmd = new MySqlCommand("\nSELECT case_type_id\nFROM case_type\nORDER BY CASE WHEN UPPER(name) = 'GENERAL' THEN 0 ELSE 1 END,\n         case_type_id\nLIMIT 1;", conn);
		try
		{
			cmd.Transaction = tx;
			object obj = await ((DbCommand)(object)cmd).ExecuteScalarAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (obj == null || obj == DBNull.Value)
			{
				throw new InvalidOperationException("No case type is configured for blotter records.");
			}
			return Convert.ToInt32(obj);
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}

	private static async Task EnsureCaseNumberAsync(MySqlConnection conn, MySqlTransaction tx, int caseId, string caseNo, CancellationToken cancellationToken)
	{
		MySqlCommand cmd = new MySqlCommand("UPDATE case_record SET case_no = @caseNo WHERE case_id = @caseId;", conn);
		try
		{
			cmd.Transaction = tx;
			cmd.Parameters.AddWithValue("@caseNo", (object)caseNo);
			cmd.Parameters.AddWithValue("@caseId", (object)caseId);
			await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}

	private static MySqlCommand BuildSaveCommand(string sql, MySqlConnection conn, MySqlTransaction tx, BlotterDto dto, string normalizedStatus, bool isNewCase)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand(sql, conn)
		{
			Transaction = tx
		};
		int num = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		object obj = ((UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
		object obj2 = ((dto.RecordedBy > 0) ? ((object)dto.RecordedBy) : ((UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value));
		val.Parameters.AddWithValue("@barangayId", (object)num);
		if (isNewCase)
		{
			val.Parameters.AddWithValue("@caseTypeId", (object)DBNull.Value);
		}
		val.Parameters.AddWithValue("@dateFiled", (object)DateTime.Today);
		val.Parameters.AddWithValue("@incidentDate", (object)dto.IncidentDate.Date);
		val.Parameters.AddWithValue("@incidentLocation", ToDbValue(dto.IncidentLocation));
		val.Parameters.AddWithValue("@summary", ToDbValue(BuildSummary(dto)));
		val.Parameters.AddWithValue("@status", (object)normalizedStatus);
		val.Parameters.AddWithValue("@handledBy", obj);
		val.Parameters.AddWithValue("@complainantId", (dto.ComplainantId > 0) ? ((object)dto.ComplainantId) : DBNull.Value);
		val.Parameters.AddWithValue("@respondentResidentId", dto.RespondentResidentId.HasValue ? ((object)dto.RespondentResidentId.Value) : DBNull.Value);
		val.Parameters.AddWithValue("@respondentName", ToDbValue(dto.RespondentName));
		val.Parameters.AddWithValue("@incidentType", ToDbValue(dto.IncidentType));
		val.Parameters.AddWithValue("@incidentTime", dto.IncidentTime.HasValue ? ((object)dto.IncidentTime.Value) : DBNull.Value);
		val.Parameters.AddWithValue("@witnessNames", ToDbValue(dto.Witnesses));
		val.Parameters.AddWithValue("@actionTaken", ToDbValue(dto.ActionTaken));
		val.Parameters.AddWithValue("@resolutionDetails", ToDbValue(dto.ResolutionDetails));
		val.Parameters.AddWithValue("@incidentDetails", ToDbValue(dto.IncidentDetails));
		val.Parameters.AddWithValue("@recordedBy", obj2);
		return val;
	}

	private static string BuildSummary(BlotterDto dto)
	{
		string text = (dto.IncidentDetails ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return (dto.IncidentType ?? "Blotter case").Trim();
		}
		if (text.Length > 300)
		{
			return text.Substring(0, 300);
		}
		return text;
	}

	private static string BuildFiledDetails(BlotterDto dto)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(dto.ComplainantName))
		{
			list.Add("Complainant: " + dto.ComplainantName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(dto.RespondentName))
		{
			list.Add("Respondent: " + dto.RespondentName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(dto.IncidentType))
		{
			list.Add("Type: " + dto.IncidentType.Trim());
		}
		list.Add("Incident date: " + dto.IncidentDate.ToString("MMM dd, yyyy"));
		if (!string.IsNullOrWhiteSpace(dto.IncidentLocation))
		{
			list.Add("Location: " + dto.IncidentLocation.Trim());
		}
		return string.Join("\n", list);
	}

	private static string BuildUpdateDetails(BlotterDto dto)
	{
		List<string> list = new List<string>
		{
			"Respondent: " + (string.IsNullOrWhiteSpace(dto.RespondentName) ? "Not specified" : dto.RespondentName.Trim()),
			"Type: " + (string.IsNullOrWhiteSpace(dto.IncidentType) ? "Not specified" : dto.IncidentType.Trim()),
			"Incident date: " + dto.IncidentDate.ToString("MMM dd, yyyy")
		};
		if (!string.IsNullOrWhiteSpace(dto.IncidentLocation))
		{
			list.Add("Location: " + dto.IncidentLocation.Trim());
		}
		return string.Join("\n", list);
	}

	private static string BuildStatusDetails(string status, string? resolutionDetails, string? referralDestination, string? closureNotes)
	{
		List<string> list = new List<string> { "New status: " + status };
		if (!string.IsNullOrWhiteSpace(resolutionDetails))
		{
			list.Add("Resolution: " + resolutionDetails.Trim());
		}
		if (!string.IsNullOrWhiteSpace(referralDestination))
		{
			list.Add("Referral: " + referralDestination.Trim());
		}
		if (!string.IsNullOrWhiteSpace(closureNotes))
		{
			list.Add("Closure notes: " + closureNotes.Trim());
		}
		return string.Join("\n", list);
	}

	private static object ToDbValue(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static string ComposeCaseNumber(string? existingCaseNo, DateTime incidentDate, int caseId)
	{
		if (!string.IsNullOrWhiteSpace(existingCaseNo))
		{
			return existingCaseNo.Trim();
		}
		DateTime value = ((incidentDate == default(DateTime)) ? DateTime.Today : incidentDate);
		return $"BLT-{value:yyyy}-{caseId:D5}";
	}

	private static string BuildAddress(string? houseNo, string? street, string? subdivision, string? purokName, string? addressNote)
	{
		return string.Join(", ", from value in new string[5] { houseNo, street, subdivision, purokName, addressNote }
			where !string.IsNullOrWhiteSpace(value)
			select value.Trim());
	}

	public Task<DataTable> LoadCasesForResidentAsync(int residentId, CancellationToken cancellationToken = default(CancellationToken))
	{
		return DatabaseManagerAsync.LoadTableAsync("\nSELECT cr.case_id,\n       COALESCE(\n           NULLIF(TRIM(cr.case_no), ''),\n           CONCAT('BLT-', DATE_FORMAT(COALESCE(cr.date_filed, DATE(cr.created_at), CURDATE()), '%Y'), '-', LPAD(cr.case_id, 5, '0'))\n       ) AS case_no,\n       COALESCE(\n           NULLIF(TRIM(CONCAT_WS(' ', rc.first_name, rc.middle_name, rc.last_name)), ''),\n           CASE\n               WHEN cr.complainant_id IS NOT NULL THEN CONCAT('Resident #', cr.complainant_id)\n               ELSE 'Unassigned'\n           END\n       ) AS complainant_name,\n       COALESCE(NULLIF(TRIM(cr.respondent_name), ''), 'Unspecified') AS respondent_name,\n       COALESCE(NULLIF(TRIM(cr.incident_type), ''), 'General') AS incident_type,\n       DATE_FORMAT(COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)), '%Y-%m-%d') AS incident_date,\n       UPPER(COALESCE(cr.status, 'ONGOING')) AS status,\n       CASE\n           WHEN cr.complainant_id = @residentId AND cr.respondent_resident_id = @residentId THEN 'Both'\n           WHEN cr.complainant_id = @residentId THEN 'Complainant'\n           ELSE 'Respondent'\n       END AS involvement\nFROM case_record cr\nLEFT JOIN resident rc ON rc.resident_id = cr.complainant_id\nWHERE cr.complainant_id = @residentId\n   OR cr.respondent_resident_id = @residentId\nORDER BY COALESCE(cr.incident_date, cr.date_filed, DATE(cr.created_at)) DESC,\n         cr.case_id DESC\nLIMIT 200;", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
		}, cancellationToken);
	}

	private static async Task<(DateTime? scheduleAt, string venue)> LoadLatestHearingAsync(MySqlConnection conn, int caseId, CancellationToken cancellationToken)
	{
		MySqlCommand cmd = new MySqlCommand("\nSELECT schedule_at, COALESCE(venue, '') AS venue\nFROM case_hearing\nWHERE case_id = @caseId\nORDER BY schedule_at DESC, hearing_id DESC\nLIMIT 1;", conn);
		try
		{
			cmd.Parameters.AddWithValue("@caseId", (object)caseId);
			MySqlDataReader reader = (MySqlDataReader)(await ((DbCommand)(object)cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
			try
			{
				if (!(await ((DbDataReader)(object)reader).ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
				{
					return (scheduleAt: null, venue: string.Empty);
				}
				DateTime? item = ((((DbDataReader)(object)reader)["schedule_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(((DbDataReader)(object)reader)["schedule_at"])));
				string item2 = Convert.ToString(((DbDataReader)(object)reader)["venue"]) ?? string.Empty;
				return (scheduleAt: item, venue: item2);
			}
			finally
			{
				((IDisposable)reader)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}
}
