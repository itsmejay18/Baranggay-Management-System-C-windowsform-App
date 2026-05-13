using System;
using System.Collections.Generic;
using System.Data.Common;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class ResidentHouseholdService
{
	private sealed class ResidentIdentity
	{
		public int ResidentId { get; init; }

		public int BarangayId { get; init; }
	}

	private sealed class TargetHouseholdSnapshot
	{
		public int HouseholdId { get; init; }

		public int PurokId { get; init; }

		public string AddressLabel { get; init; } = string.Empty;
	}

	private readonly HouseholdRepository _householdRepository;

	public ResidentHouseholdService()
		: this(new HouseholdRepository())
	{
	}

	public ResidentHouseholdService(HouseholdRepository householdRepository)
	{
		_householdRepository = householdRepository ?? throw new ArgumentNullException("householdRepository");
	}

	public void AddExistingResidentToHousehold(int residentId, int targetHouseholdId, int barangayId, string? reason = null)
	{
		string reason2 = (string.IsNullOrWhiteSpace(reason) ? "Added to household record." : reason.Trim());
		TransferResident(residentId, targetHouseholdId, barangayId, reason2);
	}

	public int TransferEntireHousehold(int sourceHouseholdId, int targetHouseholdId, int barangayId, string? reason)
	{
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		if (!Permissions.CanTransferHouseholds)
		{
			throw new UnauthorizedAccessException("You do not have permission to transfer household members.");
		}
		if (sourceHouseholdId <= 0)
		{
			throw new InvalidOperationException("Source household is required.");
		}
		if (targetHouseholdId <= 0)
		{
			throw new InvalidOperationException("Target household is required.");
		}
		if (sourceHouseholdId == targetHouseholdId)
		{
			throw new InvalidOperationException("Select a different destination household.");
		}
		int barangayId2 = HouseholdRepository.ResolveBarangayId(barangayId);
		string text = (string.IsNullOrWhiteSpace(reason) ? "Transferred from household registry." : reason.Trim());
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				TargetHouseholdSnapshot targetHouseholdSnapshot = LoadTargetHousehold(connection, val, targetHouseholdId, barangayId2);
				if (CountHouseholdMembers(connection, val, targetHouseholdId, barangayId2) > 0)
				{
					throw new InvalidOperationException("Target household already has assigned members. Select an empty household.");
				}
				List<int> list = LoadHouseholdResidentIds(connection, val, sourceHouseholdId, barangayId2);
				if (list.Count == 0)
				{
					throw new InvalidOperationException("Selected household has no members to transfer.");
				}
				foreach (int item in list)
				{
					ResidentLocationSnapshot residentLocationSnapshot = _householdRepository.GetResidentLocationSnapshot(connection, val, item);
					MySqlCommand val2 = new MySqlCommand("UPDATE resident\n                         SET household_id = @householdId,\n                             purok_id = @purokId,\n                             updated_at = NOW()\n                         WHERE resident_id = @residentId", connection, val);
					try
					{
						val2.Parameters.AddWithValue("@householdId", (object)targetHouseholdSnapshot.HouseholdId);
						val2.Parameters.AddWithValue("@purokId", (object)targetHouseholdSnapshot.PurokId);
						val2.Parameters.AddWithValue("@residentId", (object)item);
						if (((DbCommand)(object)val2).ExecuteNonQuery() <= 0)
						{
							throw new InvalidOperationException("Unable to update resident household assignment.");
						}
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
					InsertTransferHistory(connection, val, item, residentLocationSnapshot, targetHouseholdSnapshot.PurokId, targetHouseholdSnapshot.HouseholdId, targetHouseholdSnapshot.AddressLabel, text);
					AuditTrailService.LogTransactional(connection, val, "Households", "resident", item, "TRANSFER_FAMILY", new
					{
						BeforeHouseholdId = residentLocationSnapshot.HouseholdId,
						BeforePurokId = residentLocationSnapshot.PurokId,
						BeforeAddress = residentLocationSnapshot.AddressLabel
					}, new
					{
						AfterHouseholdId = targetHouseholdSnapshot.HouseholdId,
						AfterPurokId = targetHouseholdSnapshot.PurokId,
						AfterAddress = targetHouseholdSnapshot.AddressLabel
					}, text);
				}
				((DbTransaction)(object)val).Commit();
				return list.Count;
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

	public void TransferResident(int residentId, int targetHouseholdId, int barangayId, string? reason)
	{
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Expected O, but got Unknown
		if (!Permissions.CanTransferHouseholds)
		{
			throw new UnauthorizedAccessException("You do not have permission to transfer household members.");
		}
		if (residentId <= 0)
		{
			throw new InvalidOperationException("Resident is required.");
		}
		if (targetHouseholdId <= 0)
		{
			throw new InvalidOperationException("Target household is required.");
		}
		int barangayId2 = HouseholdRepository.ResolveBarangayId(barangayId);
		string text = (string.IsNullOrWhiteSpace(reason) ? "Transferred from household module." : reason.Trim());
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				if (LoadResidentIdentity(connection, val, residentId, barangayId2) == null)
				{
					throw new InvalidOperationException("Resident not found.");
				}
				ResidentLocationSnapshot residentLocationSnapshot = _householdRepository.GetResidentLocationSnapshot(connection, val, residentId);
				TargetHouseholdSnapshot targetHouseholdSnapshot = LoadTargetHousehold(connection, val, targetHouseholdId, barangayId2);
				if (residentLocationSnapshot.HouseholdId == targetHouseholdSnapshot.HouseholdId && residentLocationSnapshot.PurokId == targetHouseholdSnapshot.PurokId)
				{
					throw new InvalidOperationException("Resident is already assigned to the selected household.");
				}
				MySqlCommand val2 = new MySqlCommand("UPDATE resident\n                     SET household_id = @householdId,\n                         purok_id = @purokId,\n                         updated_at = NOW()\n                     WHERE resident_id = @residentId", connection, val);
				try
				{
					val2.Parameters.AddWithValue("@householdId", (object)targetHouseholdSnapshot.HouseholdId);
					val2.Parameters.AddWithValue("@purokId", (object)targetHouseholdSnapshot.PurokId);
					val2.Parameters.AddWithValue("@residentId", (object)residentId);
					if (((DbCommand)(object)val2).ExecuteNonQuery() <= 0)
					{
						throw new InvalidOperationException("Unable to update resident location.");
					}
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
				InsertTransferHistory(connection, val, residentId, residentLocationSnapshot, targetHouseholdSnapshot.PurokId, targetHouseholdSnapshot.HouseholdId, targetHouseholdSnapshot.AddressLabel, text);
				AuditTrailService.LogTransactional(connection, val, "Households", "resident", residentId, "TRANSFER", new
				{
					BeforeHouseholdId = residentLocationSnapshot.HouseholdId,
					BeforePurokId = residentLocationSnapshot.PurokId,
					BeforeAddress = residentLocationSnapshot.AddressLabel
				}, new
				{
					AfterHouseholdId = targetHouseholdSnapshot.HouseholdId,
					AfterPurokId = targetHouseholdSnapshot.PurokId,
					AfterAddress = targetHouseholdSnapshot.AddressLabel
				}, text);
				((DbTransaction)(object)val).Commit();
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

	public void RemoveResidentFromHousehold(int residentId, int barangayId, string? reason)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		if (!Permissions.CanTransferHouseholds)
		{
			throw new UnauthorizedAccessException("You do not have permission to remove members from households.");
		}
		if (residentId <= 0)
		{
			throw new InvalidOperationException("Resident is required.");
		}
		int barangayId2 = HouseholdRepository.ResolveBarangayId(barangayId);
		string text = (string.IsNullOrWhiteSpace(reason) ? "Removed from household." : reason.Trim());
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				if (LoadResidentIdentity(connection, val, residentId, barangayId2) == null)
				{
					throw new InvalidOperationException("Resident not found.");
				}
				ResidentLocationSnapshot residentLocationSnapshot = _householdRepository.GetResidentLocationSnapshot(connection, val, residentId);
				if (!residentLocationSnapshot.HouseholdId.HasValue)
				{
					throw new InvalidOperationException("Resident is not assigned to any household.");
				}
				MySqlCommand val2 = new MySqlCommand("UPDATE resident\n                     SET household_id = NULL,\n                         updated_at = NOW()\n                     WHERE resident_id = @residentId", connection, val);
				try
				{
					val2.Parameters.AddWithValue("@residentId", (object)residentId);
					if (((DbCommand)(object)val2).ExecuteNonQuery() <= 0)
					{
						throw new InvalidOperationException("Unable to remove resident from household.");
					}
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
				string purokName = ResolvePurokName(connection, val, residentLocationSnapshot.PurokId);
				string text2 = HouseholdRepository.BuildAddressLabel(string.Empty, string.Empty, string.Empty, purokName);
				InsertTransferHistory(connection, val, residentId, residentLocationSnapshot, residentLocationSnapshot.PurokId, null, text2, text);
				AuditTrailService.LogTransactional(connection, val, "Households", "resident", residentId, "REMOVE_FROM_HOUSEHOLD", new
				{
					BeforeHouseholdId = residentLocationSnapshot.HouseholdId,
					BeforePurokId = residentLocationSnapshot.PurokId,
					BeforeAddress = residentLocationSnapshot.AddressLabel
				}, new
				{
					AfterHouseholdId = (int?)null,
					AfterPurokId = residentLocationSnapshot.PurokId,
					AfterAddress = text2
				}, text);
				((DbTransaction)(object)val).Commit();
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

	public int RegisterResident(ResidentDto resident)
	{
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Expected O, but got Unknown
		if (!Permissions.CanCreateResidents)
		{
			throw new UnauthorizedAccessException("You do not have permission to register residents.");
		}
		if (resident == null)
		{
			throw new ArgumentNullException("resident");
		}
		resident.BarangayId = HouseholdRepository.ResolveBarangayId(resident.BarangayId.GetValueOrDefault());
		if (resident.HouseholdId.HasValue && resident.HouseholdId.Value > 0 && (!resident.PurokId.HasValue || resident.PurokId.Value <= 0))
		{
			resident.PurokId = ResolveHouseholdPurokId(resident.HouseholdId.Value, resident.BarangayId.Value);
		}
		if (!resident.PurokId.HasValue || resident.PurokId.Value <= 0)
		{
			resident.PurokId = 1;
		}
		ValidationResult validationResult = ValidationService.ValidateResidentFormSave(resident.FirstName, resident.LastName, resident.DateOfBirth);
		if (!validationResult.IsValid)
		{
			throw new InvalidOperationException(validationResult.Message);
		}
		ValidationResult validationResult2 = ValidationService.ValidateResidentDuplicate(resident);
		if (!validationResult2.IsValid)
		{
			throw new InvalidOperationException(validationResult2.Message);
		}
		ValidationResult validationResult3 = ValidationService.ValidateHouseholdConsistency(resident);
		if (!validationResult3.IsValid)
		{
			throw new InvalidOperationException(validationResult3.Message);
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			SchemaBootstrap.EnsureCoreDefaults(connection);
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				MySqlCommand val2 = new MySqlCommand("INSERT INTO resident\n                (barangay_id, purok_id, household_id, first_name, middle_name, last_name,\n                 sex, birth_date, civil_status, contact_no, status, photo, created_at, updated_at)\n              VALUES\n                (@barangayId, @purokId, @householdId, @firstName, @middleName, @lastName,\n                 @sex, @birthDate, @civilStatus, @contactNo, @status, @photo, NOW(), NOW())", connection, val);
				try
				{
					val2.Parameters.AddWithValue("@barangayId", (object)resident.BarangayId.Value);
					val2.Parameters.AddWithValue("@purokId", (object)resident.PurokId.Value);
					val2.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? ((object)resident.HouseholdId.Value) : DBNull.Value);
					val2.Parameters.AddWithValue("@firstName", (object)resident.FirstName.Trim());
					val2.Parameters.AddWithValue("@middleName", (object)(string.IsNullOrWhiteSpace(resident.MiddleName) ? ((IConvertible)DBNull.Value) : ((IConvertible)resident.MiddleName.Trim())));
					val2.Parameters.AddWithValue("@lastName", (object)resident.LastName.Trim());
					val2.Parameters.AddWithValue("@sex", (object)NormalizeSex(resident.Gender));
					val2.Parameters.AddWithValue("@birthDate", (object)resident.DateOfBirth.Date);
					val2.Parameters.AddWithValue("@civilStatus", (object)(string.IsNullOrWhiteSpace(resident.CivilStatus) ? ((IConvertible)DBNull.Value) : ((IConvertible)resident.CivilStatus.Trim())));
					val2.Parameters.AddWithValue("@contactNo", (object)(string.IsNullOrWhiteSpace(resident.ContactNo) ? ((IConvertible)DBNull.Value) : ((IConvertible)resident.ContactNo.Trim())));
					val2.Parameters.AddWithValue("@status", (object)NormalizeResidentStatus(resident.Status));
					((DbParameter)(object)val2.Parameters.Add("@photo", (MySqlDbType)251)).Value = ((object)resident.PhotoBytes) ?? ((object)DBNull.Value);
					((DbCommand)(object)val2).ExecuteNonQuery();
					int num = (int)val2.LastInsertedId;
					AuditTrailService.LogTransactional(connection, val, "Residents", "resident", num, "CREATE", null, new
					{
						ResidentId = num,
						FirstName = resident.FirstName,
						MiddleName = resident.MiddleName,
						LastName = resident.LastName,
						BarangayId = resident.BarangayId,
						PurokId = resident.PurokId,
						HouseholdId = resident.HouseholdId
					}, "Resident registered from household module.");
					((DbTransaction)(object)val).Commit();
					return num;
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

	private static void InsertTransferHistory(MySqlConnection conn, MySqlTransaction tx, int residentId, ResidentLocationSnapshot beforeLocation, int? newPurokId, int? newHouseholdId, string? newAddress, string reason)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("INSERT INTO resident_transfer_history\n                (resident_id, old_purok_id, old_household_id, old_address,\n                 new_purok_id, new_household_id, new_address, transfer_reason,\n                 transferred_by_user_id, transferred_at)\n              VALUES\n                (@residentId, @oldPurokId, @oldHouseholdId, @oldAddress,\n                 @newPurokId, @newHouseholdId, @newAddress, @reason,\n                 @userId, NOW())", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@residentId", (object)residentId);
			val.Parameters.AddWithValue("@oldPurokId", beforeLocation.PurokId.HasValue ? ((object)beforeLocation.PurokId.Value) : DBNull.Value);
			val.Parameters.AddWithValue("@oldHouseholdId", beforeLocation.HouseholdId.HasValue ? ((object)beforeLocation.HouseholdId.Value) : DBNull.Value);
			val.Parameters.AddWithValue("@oldAddress", (object)(string.IsNullOrWhiteSpace(beforeLocation.AddressLabel) ? ((IConvertible)DBNull.Value) : ((IConvertible)beforeLocation.AddressLabel)));
			val.Parameters.AddWithValue("@newPurokId", newPurokId.HasValue ? ((object)newPurokId.Value) : DBNull.Value);
			val.Parameters.AddWithValue("@newHouseholdId", newHouseholdId.HasValue ? ((object)newHouseholdId.Value) : DBNull.Value);
			val.Parameters.AddWithValue("@newAddress", (object)(string.IsNullOrWhiteSpace(newAddress) ? ((IConvertible)DBNull.Value) : ((IConvertible)newAddress)));
			val.Parameters.AddWithValue("@reason", (object)(string.IsNullOrWhiteSpace(reason) ? ((IConvertible)DBNull.Value) : ((IConvertible)reason)));
			val.Parameters.AddWithValue("@userId", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static ResidentIdentity? LoadResidentIdentity(MySqlConnection conn, MySqlTransaction tx, int residentId, int barangayId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT resident_id, barangay_id\n              FROM resident\n              WHERE resident_id = @residentId\n                AND barangay_id = @barangayId\n                AND IFNULL(is_deleted,0) = 0\n              LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@residentId", (object)residentId);
			val.Parameters.AddWithValue("@barangayId", (object)barangayId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					return null;
				}
				return new ResidentIdentity
				{
					ResidentId = Convert.ToInt32(((DbDataReader)(object)val2)["resident_id"]),
					BarangayId = Convert.ToInt32(((DbDataReader)(object)val2)["barangay_id"])
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

	private static TargetHouseholdSnapshot LoadTargetHousehold(MySqlConnection conn, MySqlTransaction tx, int householdId, int barangayId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT h.household_id,\n                     h.purok_id,\n                     h.house_no,\n                     h.street,\n                     h.subdivision,\n                     COALESCE(p.name, '') AS purok_name\n              FROM household h\n              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n              WHERE h.household_id = @householdId\n                AND h.barangay_id = @barangayId\n              LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@householdId", (object)householdId);
			val.Parameters.AddWithValue("@barangayId", (object)barangayId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					throw new InvalidOperationException("Target household not found.");
				}
				string houseNo = Convert.ToString(((DbDataReader)(object)val2)["house_no"]) ?? string.Empty;
				string street = Convert.ToString(((DbDataReader)(object)val2)["street"]) ?? string.Empty;
				string subdivision = Convert.ToString(((DbDataReader)(object)val2)["subdivision"]) ?? string.Empty;
				string purokName = Convert.ToString(((DbDataReader)(object)val2)["purok_name"]) ?? string.Empty;
				return new TargetHouseholdSnapshot
				{
					HouseholdId = Convert.ToInt32(((DbDataReader)(object)val2)["household_id"]),
					PurokId = Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"]),
					AddressLabel = HouseholdRepository.BuildAddressLabel(houseNo, street, subdivision, purokName)
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

	private static int ResolveHouseholdPurokId(int householdId, int barangayId)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT purok_id\n              FROM household\n              WHERE household_id = @householdId\n                AND barangay_id = @barangayId\n              LIMIT 1", connection);
			try
			{
				val.Parameters.AddWithValue("@householdId", (object)householdId);
				val.Parameters.AddWithValue("@barangayId", (object)barangayId);
				object obj = ((DbCommand)(object)val).ExecuteScalar();
				if (obj == null || obj == DBNull.Value)
				{
					throw new InvalidOperationException("Selected household is invalid.");
				}
				return Convert.ToInt32(obj);
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

	private static string ResolvePurokName(MySqlConnection conn, MySqlTransaction tx, int? purokId)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		if (!purokId.HasValue || purokId.Value <= 0)
		{
			return string.Empty;
		}
		MySqlCommand val = new MySqlCommand("SELECT name FROM purok_sitio WHERE purok_id = @purokId LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@purokId", (object)purokId.Value);
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			return (obj == null || obj == DBNull.Value) ? string.Empty : (Convert.ToString(obj) ?? string.Empty);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static int CountHouseholdMembers(MySqlConnection conn, MySqlTransaction tx, int householdId, int barangayId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n              FROM resident\n              WHERE household_id = @householdId\n                AND barangay_id = @barangayId\n                AND IFNULL(is_deleted,0) = 0", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@householdId", (object)householdId);
			val.Parameters.AddWithValue("@barangayId", (object)barangayId);
			return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0));
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static List<int> LoadHouseholdResidentIds(MySqlConnection conn, MySqlTransaction tx, int householdId, int barangayId)
	{
		try
		{
			return LoadHouseholdResidentIdsCore(conn, tx, householdId, barangayId, includeHeadOfFamily: true);
		}
		catch (Exception ex) when (IsMissingColumn(ex, "is_head_of_family"))
		{
			AppLogger.LogWarning("resident.is_head_of_family was not available while loading household transfer members. Retrying with compatibility query.", ex);
			return LoadHouseholdResidentIdsCore(conn, tx, householdId, barangayId, includeHeadOfFamily: false);
		}
	}

	private static List<int> LoadHouseholdResidentIdsCore(MySqlConnection conn, MySqlTransaction tx, int householdId, int barangayId, bool includeHeadOfFamily)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		List<int> list = new List<int>();
		string text = (includeHeadOfFamily ? "is_head_of_family DESC, " : string.Empty);
		MySqlCommand val = new MySqlCommand("SELECT resident_id\n               FROM resident\n               WHERE household_id = @householdId\n                 AND barangay_id = @barangayId\n                 AND IFNULL(is_deleted,0) = 0\n               ORDER BY " + text + "last_name, first_name, middle_name", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@householdId", (object)householdId);
			val.Parameters.AddWithValue("@barangayId", (object)barangayId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					list.Add(Convert.ToInt32(((DbDataReader)(object)val2)["resident_id"]));
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

	private static bool IsMissingColumn(Exception ex, string columnName)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			MySqlException ex3 = (MySqlException)(object)((ex2 is MySqlException) ? ex2 : null);
			if (ex3 != null && ex3.Number == 1054)
			{
				return true;
			}
			if ((ex2.Message ?? string.Empty).IndexOf(columnName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string NormalizeSex(string? input)
	{
		string text = (input ?? string.Empty).Trim();
		if (text.Equals("Male", StringComparison.OrdinalIgnoreCase) || text.Equals("M", StringComparison.OrdinalIgnoreCase))
		{
			return "M";
		}
		if (text.Equals("Female", StringComparison.OrdinalIgnoreCase) || text.Equals("F", StringComparison.OrdinalIgnoreCase))
		{
			return "F";
		}
		return "M";
	}

	private static string NormalizeResidentStatus(string? input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return "ACTIVE";
		}
		return input.Trim().ToUpperInvariant() switch
		{
			"ACTIVE" => "ACTIVE", 
			"DECEASED" => "DECEASED", 
			"MOVED_OUT" => "MOVED_OUT", 
			"INACTIVE" => "MOVED_OUT", 
			_ => "ACTIVE", 
		};
	}
}
