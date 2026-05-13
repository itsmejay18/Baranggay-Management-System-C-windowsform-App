using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class ResidentsModuleDataService
{
	private sealed class ResidentPersistenceSnapshot
	{
		public int ResidentId { get; init; }

		public int? HouseholdId { get; init; }

		public int? PurokId { get; init; }

		public string FirstName { get; init; } = string.Empty;

		public string MiddleName { get; init; } = string.Empty;

		public string LastName { get; init; } = string.Empty;

		public string Suffix { get; init; } = string.Empty;

		public string Sex { get; init; } = string.Empty;

		public DateTime BirthDate { get; init; }

		public string CivilStatus { get; init; } = string.Empty;

		public string ContactNo { get; init; } = string.Empty;

		public bool IsPwd { get; init; }

		public bool IsSenior { get; init; }

		public bool Is4PsBeneficiary { get; init; }

		public bool IsRegisteredVoter { get; init; }

		public bool IsSoloParent { get; init; }

		public bool IsYouth { get; init; }

		public bool IsIndigent { get; init; }

		public string Status { get; init; } = string.Empty;

		public bool IsDeleted { get; init; }
	}

	public const string CategoryAll = "ALL";

	public const string CategoryPwd = "PWD";

	public const string CategorySenior = "SENIOR";

	public const string CategoryFourPs = "FOUR_PS";

	public const string CategoryVoter = "VOTER";

	public const string CategorySoloParent = "SOLO_PARENT";

	public const string CategoryYouth = "YOUTH";

	public const string CategoryIndigent = "INDIGENT";

	public const string CategoryMale = "MALE";

	public const string CategoryFemale = "FEMALE";

	public const string CategorySingle = "SINGLE";

	public const string CategoryMarried = "MARRIED";

	public const string CategoryWidowed = "WIDOWED";

	public const string CategoryActive = "ACTIVE";

	public const string CategoryDeceased = "DECEASED";

	public const string CategoryMovedOut = "MOVED_OUT";

	private readonly HouseholdRepository _householdRepository = new HouseholdRepository();

	private readonly int _barangayId;

	public int BarangayId => _barangayId;

	public ResidentsModuleDataService()
	{
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
	}

	public Task<IReadOnlyList<LookupItem>> GetPurokOptionsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return Task.Run(() => _householdRepository.GetPurokOptions(_barangayId), cancellationToken);
	}

	public async Task<int> GetActivePurokCountAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		int valueOrDefault = default(int);
		int num;
		try
		{
			valueOrDefault = new int?(await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\r\n                  FROM purok_sitio\r\n                  WHERE barangay_id = @barangayId\r\n                    AND COALESCE(is_active, 1) = 1", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).GetValueOrDefault();
			return valueOrDefault;
		}
		catch (Exception ex) when (IsMissingColumn(ex, "is_active"))
		{
			num = 1;
		}
		if (num != 1)
		{
			return valueOrDefault;
		}
		return new int?(await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\r\n                  FROM purok_sitio\r\n                  WHERE barangay_id = @barangayId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).GetValueOrDefault();
	}

	public async Task<DataTable> LoadHouseholdsAsync(string? searchText, int? purokId, CancellationToken cancellationToken = default(CancellationToken))
	{
		string trimmedSearch = (searchText ?? string.Empty).Trim();
		string searchLike = "%" + trimmedSearch + "%";
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("SELECT h.household_id,\r\n                     h.purok_id,\r\n                     COALESCE(h.house_no, '') AS house_no,\r\n                     COALESCE(h.street, '') AS street,\r\n                     COALESCE(h.subdivision, '') AS subdivision,\r\n                     COALESCE(h.address_note, '') AS address_note,\r\n                     h.latitude,\r\n                     h.longitude,\r\n                     h.created_at,\r\n                     h.updated_at,\r\n                     COALESCE(p.name, '') AS purok_name,\r\n                     COUNT(r.resident_id) AS member_count\r\n              FROM household h\r\n              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\r\n              LEFT JOIN resident r ON r.household_id = h.household_id\r\n                                   AND COALESCE(r.is_deleted, 0) = 0\r\n              WHERE h.barangay_id = @barangayId\r\n                AND (@purokId IS NULL OR h.purok_id = @purokId)\r\n                AND (@searchText = '' OR\r\n                     COALESCE(h.house_no, '') LIKE @searchLike OR\r\n                     COALESCE(h.street, '') LIKE @searchLike OR\r\n                     COALESCE(h.subdivision, '') LIKE @searchLike OR\r\n                     COALESCE(h.address_note, '') LIKE @searchLike)\r\n              GROUP BY h.household_id, h.purok_id, h.house_no, h.street, h.subdivision, h.address_note,\r\n                       h.latitude, h.longitude, h.created_at, h.updated_at, p.name\r\n              ORDER BY COALESCE(p.name, ''), COALESCE(h.house_no, ''), COALESCE(h.street, ''), h.household_id", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@searchText", (object)trimmedSearch);
			cmd.Parameters.AddWithValue("@searchLike", (object)searchLike);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		EnsureColumn(dataTable, "coordinates_display", typeof(string));
		EnsureColumn(dataTable, "house_no_display", typeof(string));
		EnsureColumn(dataTable, "street_display", typeof(string));
		EnsureColumn(dataTable, "subdivision_display", typeof(string));
		EnsureColumn(dataTable, "address_note_display", typeof(string));
		EnsureColumn(dataTable, "purok_display", typeof(string));
		EnsureColumn(dataTable, "updated_display", typeof(string));
		foreach (DataRow row in dataTable.Rows)
		{
			decimal? latitude = ReadNullableDecimal(row["latitude"]);
			decimal? longitude = ReadNullableDecimal(row["longitude"]);
			row["coordinates_display"] = FormatHelper.FormatCoordinates(latitude, longitude);
			row["house_no_display"] = FormatHelper.Fallback(Convert.ToString(row["house_no"]));
			row["street_display"] = FormatHelper.Fallback(Convert.ToString(row["street"]));
			row["subdivision_display"] = FormatHelper.Fallback(Convert.ToString(row["subdivision"]));
			row["address_note_display"] = FormatHelper.Fallback(Convert.ToString(row["address_note"]));
			row["purok_display"] = FormatHelper.Fallback(Convert.ToString(row["purok_name"]));
			row["updated_display"] = FormatHelper.FormatDateTime(ReadNullableDateTime(row["updated_at"]));
		}
		return dataTable;
	}

	public async Task<Dictionary<string, int>> LoadResidentCategoryCountsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT COALESCE(is_pwd, 0) AS is_pwd,\n                     COALESCE(is_senior, 0) AS is_senior,\n                     COALESCE(is_4ps_beneficiary, 0) AS is_4ps_beneficiary,\n                     COALESCE(is_registered_voter, 0) AS is_registered_voter,\n                     COALESCE(is_solo_parent, 0) AS is_solo_parent,\n                     COALESCE(is_youth, 0) AS is_youth,\n                     COALESCE(is_indigent, 0) AS is_indigent,\n                     COALESCE(sex, '') AS sex,\n                     COALESCE(civil_status, '') AS civil_status,\n                     COALESCE(status, 'ACTIVE') AS status\n              FROM resident\r\n              WHERE barangay_id = @barangayId\r\n                AND COALESCE(is_deleted, 0) = 0", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			["ALL"] = 0,
			["PWD"] = 0,
			["SENIOR"] = 0,
			["FOUR_PS"] = 0,
			["VOTER"] = 0,
			["SOLO_PARENT"] = 0,
			["YOUTH"] = 0,
			["INDIGENT"] = 0,
			["MALE"] = 0,
			["FEMALE"] = 0,
			["SINGLE"] = 0,
			["MARRIED"] = 0,
			["WIDOWED"] = 0,
			["ACTIVE"] = 0,
			["DECEASED"] = 0,
			["MOVED_OUT"] = 0
		};
		foreach (DataRow row in obj.Rows)
		{
			dictionary["ALL"]++;
			if (ReadBoolean(row["is_pwd"]))
			{
				dictionary["PWD"]++;
			}
			if (ReadBoolean(row["is_senior"]))
			{
				dictionary["SENIOR"]++;
			}
			if (ReadBoolean(row["is_4ps_beneficiary"]))
			{
				dictionary["FOUR_PS"]++;
			}
			if (ReadBoolean(row["is_registered_voter"]))
			{
				dictionary["VOTER"]++;
			}
			if (ReadBoolean(row["is_solo_parent"]))
			{
				dictionary["SOLO_PARENT"]++;
			}
			if (ReadBoolean(row["is_youth"]))
			{
				dictionary["YOUTH"]++;
			}
			if (ReadBoolean(row["is_indigent"]))
			{
				dictionary["INDIGENT"]++;
			}
			string a = NormalizeSex(Convert.ToString(row["sex"]));
			if (string.Equals(a, "MALE", StringComparison.OrdinalIgnoreCase))
			{
				dictionary["MALE"]++;
			}
			else if (string.Equals(a, "FEMALE", StringComparison.OrdinalIgnoreCase))
			{
				dictionary["FEMALE"]++;
			}
			string key = NormalizeCivilStatusKey(Convert.ToString(row["civil_status"]));
			if (dictionary.ContainsKey(key))
			{
				dictionary[key]++;
			}
			string key2 = NormalizeStatusKey(Convert.ToString(row["status"]));
			if (dictionary.ContainsKey(key2))
			{
				dictionary[key2]++;
			}
		}
		return dictionary;
	}

	public async Task<int> SaveResidentAsync(ResidentDto resident, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (resident == null)
		{
			throw new ArgumentNullException("resident");
		}
		bool isUpdate = resident.Id.HasValue && resident.Id.Value > 0;
		if (isUpdate)
		{
			if (!Permissions.CanUpdateResidents)
			{
				throw new UnauthorizedAccessException("You do not have permission to edit resident records.");
			}
		}
		else if (!Permissions.CanCreateResidents)
		{
			throw new UnauthorizedAccessException("You do not have permission to add resident records.");
		}
		resident.BarangayId = _barangayId;
		resident.FirstName = resident.FirstName?.Trim() ?? string.Empty;
		resident.MiddleName = resident.MiddleName?.Trim() ?? string.Empty;
		resident.LastName = resident.LastName?.Trim() ?? string.Empty;
		resident.Suffix = resident.Suffix?.Trim() ?? string.Empty;
		resident.ContactNo = resident.ContactNo?.Trim() ?? string.Empty;
		resident.DateOfBirth = resident.DateOfBirth.Date;
		resident.Gender = NormalizeResidentSexForSave(resident.Gender);
		resident.CivilStatus = NormalizeResidentCivilStatusForSave(resident.CivilStatus);
		resident.Status = NormalizeResidentStatusForSave(resident.Status);
		ValidationResult validationResult = ValidationService.ValidateResidentFormSave(resident.FirstName, resident.LastName, resident.DateOfBirth);
		if (!validationResult.IsValid)
		{
			throw new InvalidOperationException(validationResult.Message);
		}
		if (string.IsNullOrWhiteSpace(resident.Gender))
		{
			throw new InvalidOperationException("Please select a valid sex value.");
		}
		if (!resident.PurokId.HasValue || resident.PurokId.Value <= 0)
		{
			throw new InvalidOperationException("Please select a valid purok / zone.");
		}
		ValidationResult validationResult2 = ValidationService.ValidateResidentDuplicate(resident, resident.Id);
		if (!validationResult2.IsValid)
		{
			throw new InvalidOperationException(validationResult2.Message);
		}
		ValidationResult validationResult3 = ValidationService.ValidateHouseholdConsistency(resident, resident.Id);
		if (!validationResult3.IsValid)
		{
			throw new InvalidOperationException(validationResult3.Message);
		}
		bool useCompatibilitySave = OfflineDatabaseSupport.IsOffline || DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false);
		if (!useCompatibilitySave)
		{
			await Task.Run(delegate
			{
				SchemaGuard.EnsureDatabaseReady();
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
			{
				throw new InvalidOperationException("Offline resident saving is not available right now. Please reconnect and try again.");
			}
			if (!OfflineDatabaseSupport.IsOffline)
			{
				OfflineDatabaseSupport.ActivateOfflineMode();
			}
		}
		return await Task.Run(() => (!useCompatibilitySave) ? SaveResidentCore(resident, isUpdate) : SaveResidentCompatCore(resident, isUpdate), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<DataTable> LoadResidentsByCategoryAsync(string categoryKey, string? searchText, int? purokId, string? sexFilter, string? statusFilter, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!OfflineDatabaseSupport.IsOffline && !DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false))
		{
			await Task.Run(delegate
			{
				SchemaGuard.EnsureDatabaseReady();
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		string normalizedCategory = NormalizeCategoryKey(categoryKey);
		string trimmedSearch = (searchText ?? string.Empty).Trim();
		string searchLike = "%" + trimmedSearch + "%";
		StringBuilder stringBuilder = new StringBuilder("SELECT r.resident_id,\r\n                     COALESCE(r.photo_url, '') AS photo_url,\r\n                     COALESCE(r.first_name, '') AS first_name,\r\n                     COALESCE(r.middle_name, '') AS middle_name,\r\n                     COALESCE(r.last_name, '') AS last_name,\r\n                     COALESCE(r.suffix, '') AS suffix,\r\n                     COALESCE(r.sex, '') AS sex,\r\n                     r.birth_date,\r\n                     COALESCE(r.contact_no, '') AS contact_no,\r\n                     COALESCE(r.status, 'ACTIVE') AS status,\r\n                     COALESCE(r.civil_status, '') AS civil_status,\r\n                     r.purok_id,\r\n                     COALESCE(p.name, '') AS purok_name,\r\n                     COALESCE(r.is_pwd, 0) AS is_pwd,\n                     COALESCE(r.is_senior, 0) AS is_senior,\n                     COALESCE(r.is_4ps_beneficiary, 0) AS is_4ps_beneficiary,\n                     COALESCE(r.is_registered_voter, 0) AS is_registered_voter,\n                     COALESCE(r.is_solo_parent, 0) AS is_solo_parent,\n                     COALESCE(r.is_youth, 0) AS is_youth,\n                     COALESCE(r.is_indigent, 0) AS is_indigent,\n                     COALESCE(r.education_level, '') AS education_level,\n                     COALESCE(r.occupation, '') AS occupation,\n                     r.household_id\n              FROM resident r\r\n              LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id\r\n              WHERE r.barangay_id = @barangayId\r\n                AND COALESCE(r.is_deleted, 0) = 0\r\n                AND (@searchText = '' OR\r\n                     COALESCE(r.first_name, '') LIKE @searchLike OR\r\n                     COALESCE(r.middle_name, '') LIKE @searchLike OR\r\n                     COALESCE(r.last_name, '') LIKE @searchLike OR\r\n                     COALESCE(r.contact_no, '') LIKE @searchLike)\r\n                AND (@purokId IS NULL OR r.purok_id = @purokId)");
		AppendCategoryWhereClause(stringBuilder, normalizedCategory);
		AppendSexWhereClause(stringBuilder, sexFilter);
		AppendStatusWhereClause(stringBuilder, statusFilter);
		stringBuilder.Append(" ORDER BY COALESCE(r.last_name, ''), COALESCE(r.first_name, ''), COALESCE(r.middle_name, ''), r.resident_id");
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync(stringBuilder.ToString(), delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
			cmd.Parameters.AddWithValue("@searchText", (object)trimmedSearch);
			cmd.Parameters.AddWithValue("@searchLike", (object)searchLike);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		EnrichResidentTable(obj, normalizedCategory);
		return obj;
	}

	public async Task DeleteResidentAsync(int residentId, string? reason, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!Permissions.CanDeleteResidents)
		{
			throw new UnauthorizedAccessException("You do not have permission to delete resident records.");
		}
		if (residentId <= 0)
		{
			throw new InvalidOperationException("Resident is required.");
		}
		await Task.Run(delegate
		{
			SchemaGuard.EnsureDatabaseReady();
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("SELECT resident_id,\n                     COALESCE(first_name, '') AS first_name,\n                     COALESCE(middle_name, '') AS middle_name,\n                     COALESCE(last_name, '') AS last_name,\n                     COALESCE(status, 'ACTIVE') AS status,\n                     COALESCE(is_deleted, 0) AS is_deleted\n              FROM resident\n              WHERE resident_id = @residentId\n                AND barangay_id = @barangayId\n              LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (dataTable.Rows.Count == 0)
		{
			throw new InvalidOperationException("Resident not found.");
		}
		DataRow residentRow = dataTable.Rows[0];
		if (ReadBoolean(residentRow["is_deleted"]))
		{
			throw new InvalidOperationException("Resident is already archived.");
		}
		string deleteReason = (string.IsNullOrWhiteSpace(reason) ? "Archived from resident registry." : reason.Trim());
		DateTime deletedAt = DateTime.Now;
		string fullName = FormatHelper.FormatResidentName(Convert.ToString(residentRow["first_name"]), Convert.ToString(residentRow["middle_name"]), Convert.ToString(residentRow["last_name"]), null);
		if (await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE resident\n              SET is_deleted = 1,\n                  deleted_at = @deletedAt,\n                  deleted_by_user_id = @deletedByUserId,\n                  delete_reason = @deleteReason,\n                  updated_at = @updatedAt\n              WHERE resident_id = @residentId\n                AND barangay_id = @barangayId\n                AND COALESCE(is_deleted, 0) = 0", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@deletedAt", (object)deletedAt);
			cmd.Parameters.AddWithValue("@deletedByUserId", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
			cmd.Parameters.AddWithValue("@deleteReason", (object)deleteReason);
			cmd.Parameters.AddWithValue("@updatedAt", (object)deletedAt);
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false) <= 0)
		{
			throw new InvalidOperationException("The resident record could not be archived.");
		}
		AuditTrailService.Log("Residents", "resident", residentId, "DELETE", new
		{
			ResidentId = residentId,
			FullName = fullName,
			Status = (Convert.ToString(residentRow["status"]) ?? "ACTIVE"),
			IsDeleted = false
		}, new
		{
			ResidentId = residentId,
			FullName = fullName,
			Status = (Convert.ToString(residentRow["status"]) ?? "ACTIVE"),
			IsDeleted = true,
			DeletedAt = deletedAt
		}, deleteReason);
	}

	public async Task<IReadOnlyList<string>> GetDeceasedCivilStatusOptionsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT DISTINCT COALESCE(civil_status, '') AS civil_status\n              FROM resident\r\n              WHERE barangay_id = @barangayId\r\n                AND COALESCE(is_deleted, 0) = 0\r\n                AND UPPER(COALESCE(status, '')) = 'DECEASED'\r\n              ORDER BY civil_status", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		List<string> list = new List<string>();
		foreach (DataRow row in obj.Rows)
		{
			string text = FormatHelper.Fallback(Convert.ToString(row["civil_status"]), string.Empty);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	public async Task<DataTable> LoadDeceasedResidentsAsync(string? searchText, int? purokId, string? sexFilter, string? civilStatusFilter, CancellationToken cancellationToken = default(CancellationToken))
	{
		string trimmedSearch = (searchText ?? string.Empty).Trim();
		string searchLike = "%" + trimmedSearch + "%";
		string trimmedCivilStatus = (civilStatusFilter ?? string.Empty).Trim();
		StringBuilder stringBuilder = new StringBuilder("SELECT r.resident_id,\r\n                     r.household_id,\r\n                     COALESCE(r.photo_url, '') AS photo_url,\r\n                     COALESCE(r.first_name, '') AS first_name,\r\n                     COALESCE(r.middle_name, '') AS middle_name,\r\n                     COALESCE(r.last_name, '') AS last_name,\r\n                     COALESCE(r.suffix, '') AS suffix,\r\n                     COALESCE(r.sex, '') AS sex,\r\n                     r.birth_date,\r\n                     COALESCE(r.civil_status, '') AS civil_status,\r\n                     COALESCE(r.contact_no, '') AS contact_no,\r\n                     COALESCE(r.email, '') AS email,\r\n                     COALESCE(r.occupation, '') AS occupation,\r\n                     COALESCE(r.status, 'DECEASED') AS status,\r\n                     r.date_registered,\r\n                     COALESCE(r.is_pwd, 0) AS is_pwd,\r\n                     COALESCE(r.is_senior, 0) AS is_senior,\r\n                     COALESCE(r.is_4ps_beneficiary, 0) AS is_4ps_beneficiary,\r\n                     COALESCE(r.is_registered_voter, 0) AS is_registered_voter,\r\n                     COALESCE(p.name, '') AS purok_name,\r\n                     COALESCE(h.house_no, '') AS house_no,\r\n                     COALESCE(h.street, '') AS street,\r\n                     COALESCE(h.subdivision, '') AS subdivision,\r\n                     COALESCE(h.address_note, '') AS address_note\r\n              FROM resident r\r\n              LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id\r\n              LEFT JOIN household h ON h.household_id = r.household_id\r\n              WHERE r.barangay_id = @barangayId\r\n                AND COALESCE(r.is_deleted, 0) = 0\r\n                AND UPPER(COALESCE(r.status, '')) = 'DECEASED'\r\n                AND (@searchText = '' OR\r\n                     COALESCE(r.first_name, '') LIKE @searchLike OR\r\n                     COALESCE(r.middle_name, '') LIKE @searchLike OR\r\n                     COALESCE(r.last_name, '') LIKE @searchLike)\r\n                AND (@purokId IS NULL OR r.purok_id = @purokId)\r\n                AND (@civilStatus = '' OR UPPER(COALESCE(r.civil_status, '')) = UPPER(@civilStatus))");
		AppendSexWhereClause(stringBuilder, sexFilter);
		stringBuilder.Append(" ORDER BY COALESCE(r.last_name, ''), COALESCE(r.first_name, ''), COALESCE(r.middle_name, ''), r.resident_id");
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync(stringBuilder.ToString(), delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
			cmd.Parameters.AddWithValue("@searchText", (object)trimmedSearch);
			cmd.Parameters.AddWithValue("@searchLike", (object)searchLike);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@civilStatus", (object)trimmedCivilStatus);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		EnsureColumn(dataTable, "full_name", typeof(string));
		EnsureColumn(dataTable, "age_display", typeof(string));
		EnsureColumn(dataTable, "sex_display", typeof(string));
		EnsureColumn(dataTable, "civil_status_display", typeof(string));
		EnsureColumn(dataTable, "purok_display", typeof(string));
		EnsureColumn(dataTable, "contact_display", typeof(string));
		EnsureColumn(dataTable, "address_display", typeof(string));
		EnsureColumn(dataTable, "date_registered_display", typeof(string));
		EnsureColumn(dataTable, "status_display", typeof(string));
		foreach (DataRow row in dataTable.Rows)
		{
			DateTime? birthDate = ReadNullableDateTime(row["birth_date"]);
			row["full_name"] = FormatHelper.FormatResidentName(Convert.ToString(row["first_name"]), Convert.ToString(row["middle_name"]), Convert.ToString(row["last_name"]), Convert.ToString(row["suffix"]));
			row["age_display"] = FormatHelper.FormatAge(FormatHelper.ComputeAge(birthDate));
			row["sex_display"] = FormatHelper.Fallback(GetSexDisplay(Convert.ToString(row["sex"])));
			row["civil_status_display"] = FormatHelper.Fallback(ToTitleCase(Convert.ToString(row["civil_status"])));
			row["purok_display"] = FormatHelper.Fallback(Convert.ToString(row["purok_name"]));
			row["contact_display"] = FormatHelper.Fallback(Convert.ToString(row["contact_no"]));
			row["address_display"] = FormatHelper.FormatHouseholdAddress(Convert.ToString(row["house_no"]), Convert.ToString(row["street"]), Convert.ToString(row["subdivision"]), Convert.ToString(row["address_note"]), Convert.ToString(row["purok_name"]));
			row["date_registered_display"] = FormatHelper.FormatDate(ReadNullableDateTime(row["date_registered"]));
			row["status_display"] = "Deceased";
		}
		return dataTable;
	}

	private static void EnrichResidentTable(DataTable table, string selectedCategoryKey)
	{
		EnsureColumn(table, "full_name", typeof(string));
		EnsureColumn(table, "age_display", typeof(string));
		EnsureColumn(table, "sex_display", typeof(string));
		EnsureColumn(table, "purok_display", typeof(string));
		EnsureColumn(table, "contact_display", typeof(string));
		EnsureColumn(table, "matched_category_display", typeof(string));
		EnsureColumn(table, "status_display", typeof(string));
		EnsureColumn(table, "is_pwd", typeof(bool));
		EnsureColumn(table, "is_senior", typeof(bool));
		EnsureColumn(table, "is_4ps_beneficiary", typeof(bool));
		EnsureColumn(table, "is_registered_voter", typeof(bool));
		EnsureColumn(table, "is_solo_parent", typeof(bool));
		EnsureColumn(table, "is_youth", typeof(bool));
		EnsureColumn(table, "is_indigent", typeof(bool));
		EnsureColumn(table, "status", typeof(string));
		EnsureColumn(table, "sex", typeof(string));
		foreach (DataRow row in table.Rows)
		{
			DateTime? birthDate = ReadNullableDateTime(row["birth_date"]);
			row["full_name"] = FormatHelper.FormatResidentName(Convert.ToString(row["first_name"]), Convert.ToString(row["middle_name"]), Convert.ToString(row["last_name"]), Convert.ToString(row["suffix"]));
			row["age_display"] = FormatHelper.FormatAge(FormatHelper.ComputeAge(birthDate));
			row["sex_display"] = FormatHelper.Fallback(GetSexDisplay(Convert.ToString(row["sex"])));
			row["purok_display"] = FormatHelper.Fallback(Convert.ToString(row["purok_name"]));
			row["contact_display"] = FormatHelper.Fallback(Convert.ToString(row["contact_no"]));
			row["matched_category_display"] = GetMatchedCategoryLabel(selectedCategoryKey, row);
			row["status_display"] = ToTitleCase(NormalizeStatusDisplay(Convert.ToString(row["status"])));
		}
	}

	private static string GetMatchedCategoryLabel(string selectedCategoryKey, DataRow row)
	{
		string text = NormalizeCategoryKey(selectedCategoryKey);
		if (!string.Equals(text, "ALL", StringComparison.OrdinalIgnoreCase))
		{
			return GetCategoryDisplayLabel(text);
		}
		if (ReadBoolean(row["is_pwd"]))
		{
			return GetCategoryDisplayLabel("PWD");
		}
		if (ReadBoolean(row["is_senior"]))
		{
			return GetCategoryDisplayLabel("SENIOR");
		}
		if (ReadBoolean(row["is_4ps_beneficiary"]))
		{
			return GetCategoryDisplayLabel("FOUR_PS");
		}
		if (ReadBoolean(row["is_registered_voter"]))
		{
			return GetCategoryDisplayLabel("VOTER");
		}
		if (ReadBoolean(row["is_solo_parent"]))
		{
			return GetCategoryDisplayLabel("SOLO_PARENT");
		}
		if (ReadBoolean(row["is_youth"]))
		{
			return GetCategoryDisplayLabel("YOUTH");
		}
		if (ReadBoolean(row["is_indigent"]))
		{
			return GetCategoryDisplayLabel("INDIGENT");
		}
		string text2 = NormalizeStatusKey(Convert.ToString(row["status"]));
		if (!string.Equals(text2, "ACTIVE", StringComparison.OrdinalIgnoreCase))
		{
			return GetCategoryDisplayLabel(text2);
		}
		string a = NormalizeSex(Convert.ToString(row["sex"]));
		if (string.Equals(a, "FEMALE", StringComparison.OrdinalIgnoreCase))
		{
			return GetCategoryDisplayLabel("FEMALE");
		}
		if (string.Equals(a, "MALE", StringComparison.OrdinalIgnoreCase))
		{
			return GetCategoryDisplayLabel("MALE");
		}
		return GetCategoryDisplayLabel("ACTIVE");
	}

	private static bool IsMissingColumn(Exception exception, string columnName)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			string text = ex.Message ?? string.Empty;
			if (text.IndexOf("Unknown column", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf(columnName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private int SaveResidentCore(ResidentDto resident, bool isUpdate)
	{
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			SchemaBootstrap.EnsureCoreDefaults(connection);
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				if (isUpdate)
				{
					int value = resident.Id.Value;
					ResidentPersistenceSnapshot residentPersistenceSnapshot = LoadResidentSnapshot(connection, val, value);
					if (residentPersistenceSnapshot.IsDeleted)
					{
						throw new InvalidOperationException("Resident is already archived.");
					}
					MySqlCommand val2 = new MySqlCommand("UPDATE resident\n                  SET purok_id = @purokId,\n                      household_id = @householdId,\n                      first_name = @firstName,\n                      middle_name = @middleName,\n                      last_name = @lastName,\n                      suffix = @suffix,\n                      sex = @sex,\n                      birth_date = @birthDate,\n                      civil_status = @civilStatus,\n                      contact_no = @contactNo,\n                      is_pwd = @isPwd,\n                      is_senior = @isSenior,\n                      is_4ps_beneficiary = @is4PsBeneficiary,\n                      is_registered_voter = @isRegisteredVoter,\n                      is_solo_parent = @isSoloParent,\n                      is_youth = @isYouth,\n                      is_indigent = @isIndigent,\n                      status = @status,\n                      updated_at = NOW()\n                  WHERE resident_id = @residentId\n                    AND barangay_id = @barangayId\n                    AND COALESCE(is_deleted, 0) = 0", connection, val);
					try
					{
						FillResidentCommand(val2, resident, value);
						if (((DbCommand)(object)val2).ExecuteNonQuery() <= 0)
						{
							throw new InvalidOperationException("The resident record could not be updated.");
						}
						AuditTrailService.LogTransactional(connection, val, "Residents", "resident", value, "UPDATE", residentPersistenceSnapshot, CreateResidentSnapshot(value, resident, isDeleted: false), "Resident updated from resident registry.");
						((DbTransaction)(object)val).Commit();
						return value;
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				MySqlCommand val3 = new MySqlCommand("INSERT INTO resident\n                (barangay_id, purok_id, household_id, first_name, middle_name, last_name,\n                 suffix, sex, birth_date, civil_status, contact_no, is_pwd, is_senior, is_4ps_beneficiary,\n                 is_registered_voter, is_solo_parent, is_youth, is_indigent, status, photo, created_at, updated_at)\n              VALUES\n                (@barangayId, @purokId, @householdId, @firstName, @middleName, @lastName,\n                 @suffix, @sex, @birthDate, @civilStatus, @contactNo, @isPwd, @isSenior, @is4PsBeneficiary,\n                 @isRegisteredVoter, @isSoloParent, @isYouth, @isIndigent, @status, @photo, NOW(), NOW())", connection, val);
				try
				{
					FillResidentCommand(val3, resident, null);
					((DbParameter)(object)val3.Parameters.Add("@photo", (MySqlDbType)251)).Value = ((object)resident.PhotoBytes) ?? ((object)DBNull.Value);
					((DbCommand)(object)val3).ExecuteNonQuery();
					int num = (int)val3.LastInsertedId;
					AuditTrailService.LogTransactional(connection, val, "Residents", "resident", num, "CREATE", null, CreateResidentSnapshot(num, resident, isDeleted: false), "Resident added from resident registry.");
					((DbTransaction)(object)val).Commit();
					return num;
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
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

	private int SaveResidentCompatCore(ResidentDto resident, bool isUpdate)
	{
		if (isUpdate)
		{
			int residentId = resident.Id.Value;
			ResidentPersistenceSnapshot residentPersistenceSnapshot = LoadResidentSnapshotCompat(residentId);
			if (residentPersistenceSnapshot.IsDeleted)
			{
				throw new InvalidOperationException("Resident is already archived.");
			}
			if (DbHelper.ExecuteNonQuery("UPDATE resident\n                  SET purok_id = @purokId,\n                      household_id = @householdId,\n                      first_name = @firstName,\n                      middle_name = @middleName,\n                      last_name = @lastName,\n                      suffix = @suffix,\n                      sex = @sex,\n                      birth_date = @birthDate,\n                      civil_status = @civilStatus,\n                      contact_no = @contactNo,\n                      is_pwd = @isPwd,\n                      is_senior = @isSenior,\n                      is_4ps_beneficiary = @is4PsBeneficiary,\n                      is_registered_voter = @isRegisteredVoter,\n                      is_solo_parent = @isSoloParent,\n                      is_youth = @isYouth,\n                      is_indigent = @isIndigent,\n                      status = @status,\n                      updated_at = CURRENT_TIMESTAMP\n                  WHERE resident_id = @residentId\n                    AND barangay_id = @barangayId\n                    AND COALESCE(is_deleted, 0) = 0", delegate(MySqlCommand cmd)
			{
				FillResidentCommand(cmd, resident, residentId);
			}) <= 0)
			{
				throw new InvalidOperationException("The resident record could not be updated.");
			}
			AuditTrailService.Log("Residents", "resident", residentId, "UPDATE", residentPersistenceSnapshot, CreateResidentSnapshot(residentId, resident, isDeleted: false), "Resident updated from resident registry.");
			return residentId;
		}
		if (DbHelper.ExecuteNonQuery("INSERT INTO resident\n                (barangay_id, purok_id, household_id, first_name, middle_name, last_name,\n                 suffix, sex, birth_date, civil_status, contact_no, is_pwd, is_senior, is_4ps_beneficiary,\n                 is_registered_voter, is_solo_parent, is_youth, is_indigent, status, photo, created_at, updated_at)\n              VALUES\n                (@barangayId, @purokId, @householdId, @firstName, @middleName, @lastName,\n                 @suffix, @sex, @birthDate, @civilStatus, @contactNo, @isPwd, @isSenior, @is4PsBeneficiary,\n                 @isRegisteredVoter, @isSoloParent, @isYouth, @isIndigent, @status, @photo, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)", delegate(MySqlCommand cmd)
		{
			FillResidentCommand(cmd, resident, null);
			((DbParameter)(object)cmd.Parameters.Add("@photo", (MySqlDbType)251)).Value = ((object)resident.PhotoBytes) ?? ((object)DBNull.Value);
		}) <= 0)
		{
			throw new InvalidOperationException("The resident record could not be added.");
		}
		int num = ResolveInsertedResidentIdCompat(resident);
		if (num <= 0)
		{
			throw new InvalidOperationException("Resident was added locally, but the new resident ID could not be resolved.");
		}
		AuditTrailService.Log("Residents", "resident", num, "CREATE", null, CreateResidentSnapshot(num, resident, isDeleted: false), "Resident added from resident registry.");
		return num;
	}

	private void FillResidentCommand(MySqlCommand command, ResidentDto resident, int? residentId)
	{
		command.Parameters.AddWithValue("@barangayId", (object)_barangayId);
		command.Parameters.AddWithValue("@purokId", (object)resident.PurokId.Value);
		command.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? ((object)resident.HouseholdId.Value) : DBNull.Value);
		command.Parameters.AddWithValue("@firstName", (object)resident.FirstName);
		command.Parameters.AddWithValue("@middleName", (object)(string.IsNullOrWhiteSpace(resident.MiddleName) ? ((IConvertible)DBNull.Value) : ((IConvertible)resident.MiddleName)));
		command.Parameters.AddWithValue("@lastName", (object)resident.LastName);
		command.Parameters.AddWithValue("@suffix", (object)(string.IsNullOrWhiteSpace(resident.Suffix) ? ((IConvertible)DBNull.Value) : ((IConvertible)resident.Suffix)));
		command.Parameters.AddWithValue("@sex", (object)resident.Gender);
		command.Parameters.AddWithValue("@birthDate", (object)resident.DateOfBirth.Date);
		command.Parameters.AddWithValue("@civilStatus", (object)(string.IsNullOrWhiteSpace(resident.CivilStatus) ? ((IConvertible)DBNull.Value) : ((IConvertible)resident.CivilStatus)));
		command.Parameters.AddWithValue("@contactNo", (object)(string.IsNullOrWhiteSpace(resident.ContactNo) ? ((IConvertible)DBNull.Value) : ((IConvertible)resident.ContactNo)));
		command.Parameters.AddWithValue("@isPwd", (object)(resident.IsPwd ? 1 : 0));
		command.Parameters.AddWithValue("@isSenior", (object)(resident.IsSenior ? 1 : 0));
		command.Parameters.AddWithValue("@is4PsBeneficiary", (object)(resident.Is4PsBeneficiary ? 1 : 0));
		command.Parameters.AddWithValue("@isRegisteredVoter", (object)(resident.IsRegisteredVoter ? 1 : 0));
		command.Parameters.AddWithValue("@isSoloParent", (object)(resident.IsSoloParent ? 1 : 0));
		command.Parameters.AddWithValue("@isYouth", (object)(resident.IsYouth ? 1 : 0));
		command.Parameters.AddWithValue("@isIndigent", (object)(resident.IsIndigent ? 1 : 0));
		command.Parameters.AddWithValue("@status", (object)resident.Status);
		if (residentId.HasValue)
		{
			command.Parameters.AddWithValue("@residentId", (object)residentId.Value);
		}
	}

	private ResidentPersistenceSnapshot LoadResidentSnapshot(MySqlConnection conn, MySqlTransaction tx, int residentId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT resident_id,\n                     household_id,\n                     purok_id,\n                     COALESCE(first_name, '') AS first_name,\n                     COALESCE(middle_name, '') AS middle_name,\n                     COALESCE(last_name, '') AS last_name,\n                     COALESCE(suffix, '') AS suffix,\n                     COALESCE(sex, '') AS sex,\n                     birth_date,\n                     COALESCE(civil_status, '') AS civil_status,\n                     COALESCE(contact_no, '') AS contact_no,\n                     COALESCE(is_pwd, 0) AS is_pwd,\n                     COALESCE(is_senior, 0) AS is_senior,\n                     COALESCE(is_4ps_beneficiary, 0) AS is_4ps_beneficiary,\n                     COALESCE(is_registered_voter, 0) AS is_registered_voter,\n                     COALESCE(is_solo_parent, 0) AS is_solo_parent,\n                     COALESCE(is_youth, 0) AS is_youth,\n                     COALESCE(is_indigent, 0) AS is_indigent,\n                     COALESCE(status, 'ACTIVE') AS status,\n                     COALESCE(is_deleted, 0) AS is_deleted\n              FROM resident\n              WHERE resident_id = @residentId\n                AND barangay_id = @barangayId\n              LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@residentId", (object)residentId);
			val.Parameters.AddWithValue("@barangayId", (object)_barangayId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					throw new InvalidOperationException("Resident not found.");
				}
				return new ResidentPersistenceSnapshot
				{
					ResidentId = Convert.ToInt32(((DbDataReader)(object)val2)["resident_id"], CultureInfo.InvariantCulture),
					HouseholdId = ((((DbDataReader)(object)val2)["household_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)val2)["household_id"], CultureInfo.InvariantCulture))),
					PurokId = ((((DbDataReader)(object)val2)["purok_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"], CultureInfo.InvariantCulture))),
					FirstName = (Convert.ToString(((DbDataReader)(object)val2)["first_name"]) ?? string.Empty),
					MiddleName = (Convert.ToString(((DbDataReader)(object)val2)["middle_name"]) ?? string.Empty),
					LastName = (Convert.ToString(((DbDataReader)(object)val2)["last_name"]) ?? string.Empty),
					Suffix = (Convert.ToString(((DbDataReader)(object)val2)["suffix"]) ?? string.Empty),
					Sex = (Convert.ToString(((DbDataReader)(object)val2)["sex"]) ?? string.Empty),
					BirthDate = ((((DbDataReader)(object)val2)["birth_date"] == DBNull.Value) ? DateTime.Today : Convert.ToDateTime(((DbDataReader)(object)val2)["birth_date"], CultureInfo.InvariantCulture)),
					CivilStatus = (Convert.ToString(((DbDataReader)(object)val2)["civil_status"]) ?? string.Empty),
					ContactNo = (Convert.ToString(((DbDataReader)(object)val2)["contact_no"]) ?? string.Empty),
					IsPwd = (((DbDataReader)(object)val2)["is_pwd"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_pwd"], CultureInfo.InvariantCulture) == 1),
					IsSenior = (((DbDataReader)(object)val2)["is_senior"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_senior"], CultureInfo.InvariantCulture) == 1),
					Is4PsBeneficiary = (((DbDataReader)(object)val2)["is_4ps_beneficiary"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_4ps_beneficiary"], CultureInfo.InvariantCulture) == 1),
					IsRegisteredVoter = (((DbDataReader)(object)val2)["is_registered_voter"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_registered_voter"], CultureInfo.InvariantCulture) == 1),
					IsSoloParent = (((DbDataReader)(object)val2)["is_solo_parent"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_solo_parent"], CultureInfo.InvariantCulture) == 1),
					IsYouth = (((DbDataReader)(object)val2)["is_youth"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_youth"], CultureInfo.InvariantCulture) == 1),
					IsIndigent = (((DbDataReader)(object)val2)["is_indigent"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_indigent"], CultureInfo.InvariantCulture) == 1),
					Status = (Convert.ToString(((DbDataReader)(object)val2)["status"]) ?? "ACTIVE"),
					IsDeleted = (((DbDataReader)(object)val2)["is_deleted"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_deleted"], CultureInfo.InvariantCulture) == 1)
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

	private ResidentPersistenceSnapshot LoadResidentSnapshotCompat(int residentId)
	{
		DataTable dataTable = DbHelper.LoadTable("SELECT resident_id,\n                     household_id,\n                     purok_id,\n                     COALESCE(first_name, '') AS first_name,\n                     COALESCE(middle_name, '') AS middle_name,\n                     COALESCE(last_name, '') AS last_name,\n                     COALESCE(suffix, '') AS suffix,\n                     COALESCE(sex, '') AS sex,\n                     birth_date,\n                     COALESCE(civil_status, '') AS civil_status,\n                     COALESCE(contact_no, '') AS contact_no,\n                     COALESCE(is_pwd, 0) AS is_pwd,\n                     COALESCE(is_senior, 0) AS is_senior,\n                     COALESCE(is_4ps_beneficiary, 0) AS is_4ps_beneficiary,\n                     COALESCE(is_registered_voter, 0) AS is_registered_voter,\n                     COALESCE(is_solo_parent, 0) AS is_solo_parent,\n                     COALESCE(is_youth, 0) AS is_youth,\n                     COALESCE(is_indigent, 0) AS is_indigent,\n                     COALESCE(status, 'ACTIVE') AS status,\n                     COALESCE(is_deleted, 0) AS is_deleted\n              FROM resident\n              WHERE resident_id = @residentId\n                AND barangay_id = @barangayId\n              LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
		});
		if (dataTable.Rows.Count == 0)
		{
			throw new InvalidOperationException("Resident not found.");
		}
		DataRow dataRow = dataTable.Rows[0];
		return new ResidentPersistenceSnapshot
		{
			ResidentId = Convert.ToInt32(dataRow["resident_id"], CultureInfo.InvariantCulture),
			HouseholdId = ((dataRow["household_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(dataRow["household_id"], CultureInfo.InvariantCulture))),
			PurokId = ((dataRow["purok_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(dataRow["purok_id"], CultureInfo.InvariantCulture))),
			FirstName = (Convert.ToString(dataRow["first_name"]) ?? string.Empty),
			MiddleName = (Convert.ToString(dataRow["middle_name"]) ?? string.Empty),
			LastName = (Convert.ToString(dataRow["last_name"]) ?? string.Empty),
			Suffix = (Convert.ToString(dataRow["suffix"]) ?? string.Empty),
			Sex = (Convert.ToString(dataRow["sex"]) ?? string.Empty),
			BirthDate = ((dataRow["birth_date"] == DBNull.Value) ? DateTime.Today : Convert.ToDateTime(dataRow["birth_date"], CultureInfo.InvariantCulture)),
			CivilStatus = (Convert.ToString(dataRow["civil_status"]) ?? string.Empty),
			ContactNo = (Convert.ToString(dataRow["contact_no"]) ?? string.Empty),
			IsPwd = (dataRow["is_pwd"] != DBNull.Value && Convert.ToInt32(dataRow["is_pwd"], CultureInfo.InvariantCulture) == 1),
			IsSenior = (dataRow["is_senior"] != DBNull.Value && Convert.ToInt32(dataRow["is_senior"], CultureInfo.InvariantCulture) == 1),
			Is4PsBeneficiary = (dataRow["is_4ps_beneficiary"] != DBNull.Value && Convert.ToInt32(dataRow["is_4ps_beneficiary"], CultureInfo.InvariantCulture) == 1),
			IsRegisteredVoter = (dataRow["is_registered_voter"] != DBNull.Value && Convert.ToInt32(dataRow["is_registered_voter"], CultureInfo.InvariantCulture) == 1),
			IsSoloParent = (dataRow["is_solo_parent"] != DBNull.Value && Convert.ToInt32(dataRow["is_solo_parent"], CultureInfo.InvariantCulture) == 1),
			IsYouth = (dataRow["is_youth"] != DBNull.Value && Convert.ToInt32(dataRow["is_youth"], CultureInfo.InvariantCulture) == 1),
			IsIndigent = (dataRow["is_indigent"] != DBNull.Value && Convert.ToInt32(dataRow["is_indigent"], CultureInfo.InvariantCulture) == 1),
			Status = (Convert.ToString(dataRow["status"]) ?? "ACTIVE"),
			IsDeleted = (dataRow["is_deleted"] != DBNull.Value && Convert.ToInt32(dataRow["is_deleted"], CultureInfo.InvariantCulture) == 1)
		};
	}

	private int ResolveInsertedResidentIdCompat(ResidentDto resident)
	{
		return new int?(DbHelper.ExecuteScalar<int>("SELECT resident_id\n              FROM resident\n              WHERE barangay_id = @barangayId\n                AND purok_id = @purokId\n                AND ((@householdId IS NULL AND household_id IS NULL) OR household_id = @householdId)\n                AND COALESCE(first_name, '') = @firstName\n                AND COALESCE(middle_name, '') = @middleNameMatch\n                AND COALESCE(last_name, '') = @lastName\n                AND COALESCE(suffix, '') = @suffixMatch\n                AND birth_date = @birthDate\n                AND COALESCE(is_deleted, 0) = 0\n              ORDER BY resident_id DESC\n              LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)_barangayId);
			cmd.Parameters.AddWithValue("@purokId", (object)resident.PurokId.Value);
			cmd.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? ((object)resident.HouseholdId.Value) : DBNull.Value);
			cmd.Parameters.AddWithValue("@firstName", (object)resident.FirstName);
			cmd.Parameters.AddWithValue("@middleNameMatch", (object)resident.MiddleName);
			cmd.Parameters.AddWithValue("@lastName", (object)resident.LastName);
			cmd.Parameters.AddWithValue("@suffixMatch", (object)resident.Suffix);
			cmd.Parameters.AddWithValue("@birthDate", (object)resident.DateOfBirth.Date);
		})).GetValueOrDefault();
	}

	private static ResidentPersistenceSnapshot CreateResidentSnapshot(int residentId, ResidentDto resident, bool isDeleted)
	{
		return new ResidentPersistenceSnapshot
		{
			ResidentId = residentId,
			HouseholdId = resident.HouseholdId,
			PurokId = resident.PurokId,
			FirstName = resident.FirstName,
			MiddleName = resident.MiddleName,
			LastName = resident.LastName,
			Suffix = resident.Suffix,
			Sex = resident.Gender,
			BirthDate = resident.DateOfBirth.Date,
			CivilStatus = resident.CivilStatus,
			ContactNo = resident.ContactNo,
			IsPwd = resident.IsPwd,
			IsSenior = resident.IsSenior,
			Is4PsBeneficiary = resident.Is4PsBeneficiary,
			IsRegisteredVoter = resident.IsRegisteredVoter,
			IsSoloParent = resident.IsSoloParent,
			IsYouth = resident.IsYouth,
			IsIndigent = resident.IsIndigent,
			Status = resident.Status,
			IsDeleted = isDeleted
		};
	}

	public static string GetCategoryDisplayLabel(string categoryKey)
	{
		return NormalizeCategoryKey(categoryKey) switch
		{
			"PWD" => "PWD", 
			"SENIOR" => "Senior Citizen", 
			"FOUR_PS" => "4Ps Beneficiary", 
			"VOTER" => "Registered Voter", 
			"SOLO_PARENT" => "Solo Parent", 
			"YOUTH" => "Youth", 
			"INDIGENT" => "Indigent", 
			"MALE" => "Male", 
			"FEMALE" => "Female", 
			"SINGLE" => "Single", 
			"MARRIED" => "Married", 
			"WIDOWED" => "Widowed", 
			"DECEASED" => "Deceased", 
			"MOVED_OUT" => "Moved Out", 
			"ACTIVE" => "Active", 
			_ => "Resident", 
		};
	}

	public static string NormalizeCategoryKey(string? categoryKey)
	{
		return (categoryKey ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"PWD" => "PWD", 
			"SENIOR" => "SENIOR", 
			"FOUR_PS" => "FOUR_PS", 
			"VOTER" => "VOTER", 
			"SOLO_PARENT" => "SOLO_PARENT", 
			"YOUTH" => "YOUTH", 
			"INDIGENT" => "INDIGENT", 
			"MALE" => "MALE", 
			"FEMALE" => "FEMALE", 
			"SINGLE" => "SINGLE", 
			"MARRIED" => "MARRIED", 
			"WIDOWED" => "WIDOWED", 
			"DECEASED" => "DECEASED", 
			"MOVED_OUT" => "MOVED_OUT", 
			"ACTIVE" => "ACTIVE", 
			_ => "ALL", 
		};
	}

	private static void AppendCategoryWhereClause(StringBuilder sql, string categoryKey)
	{
		string text = NormalizeCategoryKey(categoryKey);
		if (text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 6:
			switch (text[0])
			{
			case 'S':
				if (!(text == "SENIOR"))
				{
					if (text == "SINGLE")
					{
						sql.Append(" AND UPPER(COALESCE(r.civil_status, '')) = 'SINGLE'");
					}
				}
				else
				{
					sql.Append(" AND COALESCE(r.is_senior, 0) = 1");
				}
				break;
			case 'F':
				if (text == "FEMALE")
				{
					sql.Append(" AND UPPER(COALESCE(r.sex, '')) IN ('F', 'FEMALE')");
				}
				break;
			case 'A':
				if (text == "ACTIVE")
				{
					sql.Append(" AND UPPER(COALESCE(r.status, 'ACTIVE')) = 'ACTIVE'");
				}
				break;
			}
			break;
		case 7:
			switch (text[0])
			{
			case 'F':
				if (text == "FOUR_PS")
				{
					sql.Append(" AND COALESCE(r.is_4ps_beneficiary, 0) = 1");
				}
				break;
			case 'M':
				if (text == "MARRIED")
				{
					sql.Append(" AND UPPER(COALESCE(r.civil_status, '')) = 'MARRIED'");
				}
				break;
			case 'W':
				if (text == "WIDOWED")
				{
					sql.Append(" AND UPPER(COALESCE(r.civil_status, '')) = 'WIDOWED'");
				}
				break;
			}
			break;
		case 5:
			switch (text[0])
			{
			case 'V':
				if (text == "VOTER")
				{
					sql.Append(" AND COALESCE(r.is_registered_voter, 0) = 1");
				}
				break;
			case 'Y':
				if (text == "YOUTH")
				{
					sql.Append(" AND COALESCE(r.is_youth, 0) = 1");
				}
				break;
			}
			break;
		case 8:
			switch (text[0])
			{
			case 'I':
				if (text == "INDIGENT")
				{
					sql.Append(" AND COALESCE(r.is_indigent, 0) = 1");
				}
				break;
			case 'D':
				if (text == "DECEASED")
				{
					sql.Append(" AND UPPER(COALESCE(r.status, '')) = 'DECEASED'");
				}
				break;
			}
			break;
		case 3:
			if (text == "PWD")
			{
				sql.Append(" AND COALESCE(r.is_pwd, 0) = 1");
			}
			break;
		case 11:
			if (text == "SOLO_PARENT")
			{
				sql.Append(" AND COALESCE(r.is_solo_parent, 0) = 1");
			}
			break;
		case 4:
			if (text == "MALE")
			{
				sql.Append(" AND UPPER(COALESCE(r.sex, '')) IN ('M', 'MALE')");
			}
			break;
		case 9:
			if (text == "MOVED_OUT")
			{
				sql.Append(" AND UPPER(COALESCE(r.status, '')) = 'MOVED_OUT'");
			}
			break;
		case 10:
			break;
		}
	}

	private static void AppendSexWhereClause(StringBuilder sql, string? sexFilter)
	{
		string a = NormalizeSex(sexFilter);
		if (string.Equals(a, "MALE", StringComparison.OrdinalIgnoreCase))
		{
			sql.Append(" AND UPPER(COALESCE(r.sex, '')) IN ('M', 'MALE')");
		}
		else if (string.Equals(a, "FEMALE", StringComparison.OrdinalIgnoreCase))
		{
			sql.Append(" AND UPPER(COALESCE(r.sex, '')) IN ('F', 'FEMALE')");
		}
	}

	private static void AppendStatusWhereClause(StringBuilder sql, string? statusFilter)
	{
		string a = NormalizeStatusKey(statusFilter);
		if (string.Equals(a, "ACTIVE", StringComparison.OrdinalIgnoreCase))
		{
			sql.Append(" AND UPPER(COALESCE(r.status, 'ACTIVE')) = 'ACTIVE'");
		}
		else if (string.Equals(a, "DECEASED", StringComparison.OrdinalIgnoreCase))
		{
			sql.Append(" AND UPPER(COALESCE(r.status, '')) = 'DECEASED'");
		}
		else if (string.Equals(a, "MOVED_OUT", StringComparison.OrdinalIgnoreCase))
		{
			sql.Append(" AND UPPER(COALESCE(r.status, '')) = 'MOVED_OUT'");
		}
	}

	private static string NormalizeStatusKey(string? status)
	{
		return (status ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"DECEASED" => "DECEASED", 
			"MOVED OUT" => "MOVED_OUT", 
			"MOVED_OUT" => "MOVED_OUT", 
			"INACTIVE" => "MOVED_OUT", 
			"ACTIVE" => "ACTIVE", 
			_ => string.Empty, 
		};
	}

	private static string NormalizeStatusDisplay(string? status)
	{
		string text = NormalizeStatusKey(status);
		if (!(text == "DECEASED"))
		{
			if (text == "MOVED_OUT")
			{
				return "Moved Out";
			}
			return "Active";
		}
		return "Deceased";
	}

	private static string NormalizeResidentStatusForSave(string? status)
	{
		return (status ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"DECEASED" => "DECEASED", 
			"MOVED OUT" => "MOVED_OUT", 
			"MOVED_OUT" => "MOVED_OUT", 
			"INACTIVE" => "MOVED_OUT", 
			_ => "ACTIVE", 
		};
	}

	private static string NormalizeResidentCivilStatusForSave(string? civilStatus)
	{
		return (civilStatus ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"MARRIED" => "Married", 
			"WIDOWED" => "Widowed", 
			"SEPARATED" => "Separated", 
			"SINGLE" => "Single", 
			_ => string.Empty, 
		};
	}

	private static string NormalizeResidentSexForSave(string? sex)
	{
		return (sex ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"M" => "M", 
			"MALE" => "M", 
			"F" => "F", 
			"FEMALE" => "F", 
			_ => string.Empty, 
		};
	}

	private static string NormalizeCivilStatusKey(string? civilStatus)
	{
		return (civilStatus ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"SINGLE" => "SINGLE", 
			"MARRIED" => "MARRIED", 
			"WIDOWED" => "WIDOWED", 
			_ => string.Empty, 
		};
	}

	private static string NormalizeSex(string? sex)
	{
		return (sex ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"M" => "MALE", 
			"MALE" => "MALE", 
			"F" => "FEMALE", 
			"FEMALE" => "FEMALE", 
			_ => string.Empty, 
		};
	}

	private static string GetSexDisplay(string? sex)
	{
		string text = NormalizeSex(sex);
		if (!(text == "MALE"))
		{
			if (text == "FEMALE")
			{
				return "Female";
			}
			return FormatHelper.Fallback(sex);
		}
		return "Male";
	}

	private static string ToTitleCase(string? value)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "—";
		}
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
	}

	private static bool ReadBoolean(object value)
	{
		if (value != DBNull.Value)
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
		}
		return false;
	}

	private static DateTime? ReadNullableDateTime(object value)
	{
		if (value != DBNull.Value && value != null)
		{
			return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
		}
		return null;
	}

	private static decimal? ReadNullableDecimal(object value)
	{
		if (value != DBNull.Value && value != null)
		{
			return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		}
		return null;
	}

	private static void EnsureColumn(DataTable table, string name, Type type)
	{
		if (!table.Columns.Contains(name))
		{
			table.Columns.Add(name, type);
		}
	}
}
