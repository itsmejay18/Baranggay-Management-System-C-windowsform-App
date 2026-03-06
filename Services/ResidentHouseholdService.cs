using System;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal sealed class ResidentHouseholdService
{
    private readonly HouseholdRepository _householdRepository;

    public ResidentHouseholdService()
        : this(new HouseholdRepository())
    {
    }

    public ResidentHouseholdService(HouseholdRepository householdRepository)
    {
        _householdRepository = householdRepository ?? throw new ArgumentNullException(nameof(householdRepository));
    }

    public void AddExistingResidentToHousehold(int residentId, int targetHouseholdId, int barangayId, string? reason = null)
    {
        string transferReason = string.IsNullOrWhiteSpace(reason) ? "Added to household record." : reason.Trim();
        TransferResident(residentId, targetHouseholdId, barangayId, transferReason);
    }

    public void TransferResident(int residentId, int targetHouseholdId, int barangayId, string? reason)
    {
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

        int targetBarangayId = HouseholdRepository.ResolveBarangayId(barangayId);
        string transferReason = string.IsNullOrWhiteSpace(reason) ? "Transferred from household module." : reason.Trim();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var residentInfo = LoadResidentIdentity(conn, tx, residentId, targetBarangayId);
        if (residentInfo == null)
        {
            throw new InvalidOperationException("Resident not found.");
        }

        ResidentLocationSnapshot beforeLocation = _householdRepository.GetResidentLocationSnapshot(conn, tx, residentId);
        TargetHouseholdSnapshot targetHousehold = LoadTargetHousehold(conn, tx, targetHouseholdId, targetBarangayId);

        if (beforeLocation.HouseholdId == targetHousehold.HouseholdId && beforeLocation.PurokId == targetHousehold.PurokId)
        {
            throw new InvalidOperationException("Resident is already assigned to the selected household.");
        }

        using (var updateCmd = new MySqlCommand(
                   @"UPDATE resident
                     SET household_id = @householdId,
                         purok_id = @purokId,
                         updated_at = NOW()
                     WHERE resident_id = @residentId",
                   conn,
                   tx))
        {
            updateCmd.Parameters.AddWithValue("@householdId", targetHousehold.HouseholdId);
            updateCmd.Parameters.AddWithValue("@purokId", targetHousehold.PurokId);
            updateCmd.Parameters.AddWithValue("@residentId", residentId);
            int rows = updateCmd.ExecuteNonQuery();
            if (rows <= 0)
            {
                throw new InvalidOperationException("Unable to update resident location.");
            }
        }

        InsertTransferHistory(
            conn,
            tx,
            residentId,
            beforeLocation,
            targetHousehold.PurokId,
            targetHousehold.HouseholdId,
            targetHousehold.AddressLabel,
            transferReason);

        AuditTrailService.LogTransactional(
            conn,
            tx,
            "Households",
            "resident",
            residentId,
            "TRANSFER",
            new
            {
                BeforeHouseholdId = beforeLocation.HouseholdId,
                BeforePurokId = beforeLocation.PurokId,
                BeforeAddress = beforeLocation.AddressLabel
            },
            new
            {
                AfterHouseholdId = targetHousehold.HouseholdId,
                AfterPurokId = targetHousehold.PurokId,
                AfterAddress = targetHousehold.AddressLabel
            },
            transferReason);

        tx.Commit();
    }

    public void RemoveResidentFromHousehold(int residentId, int barangayId, string? reason)
    {
        if (!Permissions.CanTransferHouseholds)
        {
            throw new UnauthorizedAccessException("You do not have permission to remove members from households.");
        }

        if (residentId <= 0)
        {
            throw new InvalidOperationException("Resident is required.");
        }

        int targetBarangayId = HouseholdRepository.ResolveBarangayId(barangayId);
        string transferReason = string.IsNullOrWhiteSpace(reason) ? "Removed from household." : reason.Trim();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var residentInfo = LoadResidentIdentity(conn, tx, residentId, targetBarangayId);
        if (residentInfo == null)
        {
            throw new InvalidOperationException("Resident not found.");
        }

        ResidentLocationSnapshot beforeLocation = _householdRepository.GetResidentLocationSnapshot(conn, tx, residentId);
        if (!beforeLocation.HouseholdId.HasValue)
        {
            throw new InvalidOperationException("Resident is not assigned to any household.");
        }

        using (var updateCmd = new MySqlCommand(
                   @"UPDATE resident
                     SET household_id = NULL,
                         updated_at = NOW()
                     WHERE resident_id = @residentId",
                   conn,
                   tx))
        {
            updateCmd.Parameters.AddWithValue("@residentId", residentId);
            int rows = updateCmd.ExecuteNonQuery();
            if (rows <= 0)
            {
                throw new InvalidOperationException("Unable to remove resident from household.");
            }
        }

        string purokName = ResolvePurokName(conn, tx, beforeLocation.PurokId);
        string newAddress = HouseholdRepository.BuildAddressLabel(string.Empty, string.Empty, string.Empty, purokName);

        InsertTransferHistory(
            conn,
            tx,
            residentId,
            beforeLocation,
            beforeLocation.PurokId,
            null,
            newAddress,
            transferReason);

        AuditTrailService.LogTransactional(
            conn,
            tx,
            "Households",
            "resident",
            residentId,
            "REMOVE_FROM_HOUSEHOLD",
            new
            {
                BeforeHouseholdId = beforeLocation.HouseholdId,
                BeforePurokId = beforeLocation.PurokId,
                BeforeAddress = beforeLocation.AddressLabel
            },
            new
            {
                AfterHouseholdId = (int?)null,
                AfterPurokId = beforeLocation.PurokId,
                AfterAddress = newAddress
            },
            transferReason);

        tx.Commit();
    }

    public int RegisterResident(ResidentDto resident)
    {
        if (!Permissions.CanCreateResidents)
        {
            throw new UnauthorizedAccessException("You do not have permission to register residents.");
        }

        if (resident == null)
        {
            throw new ArgumentNullException(nameof(resident));
        }

        resident.BarangayId = HouseholdRepository.ResolveBarangayId(resident.BarangayId ?? 0);

        if (resident.HouseholdId.HasValue && resident.HouseholdId.Value > 0 && (!resident.PurokId.HasValue || resident.PurokId.Value <= 0))
        {
            resident.PurokId = ResolveHouseholdPurokId(resident.HouseholdId.Value, resident.BarangayId.Value);
        }

        if (!resident.PurokId.HasValue || resident.PurokId.Value <= 0)
        {
            resident.PurokId = SchemaDefaults.DefaultPurokId;
        }

        var baseValidation = ValidationService.ValidateResidentFormSave(
            resident.FirstName,
            resident.LastName,
            resident.DateOfBirth);
        if (!baseValidation.IsValid)
        {
            throw new InvalidOperationException(baseValidation.Message);
        }

        var duplicateValidation = ValidationService.ValidateResidentDuplicate(resident, null);
        if (!duplicateValidation.IsValid)
        {
            throw new InvalidOperationException(duplicateValidation.Message);
        }

        var householdValidation = ValidationService.ValidateHouseholdConsistency(resident, null);
        if (!householdValidation.IsValid)
        {
            throw new InvalidOperationException(householdValidation.Message);
        }

        using var conn = DBConnection.GetConnection();
        conn.Open();
        SchemaBootstrap.EnsureCoreDefaults(conn);
        using var tx = conn.BeginTransaction();

        using var cmd = new MySqlCommand(
            @"INSERT INTO resident
                (barangay_id, purok_id, household_id, first_name, middle_name, last_name,
                 sex, birth_date, civil_status, contact_no, status, photo, created_at, updated_at)
              VALUES
                (@barangayId, @purokId, @householdId, @firstName, @middleName, @lastName,
                 @sex, @birthDate, @civilStatus, @contactNo, @status, @photo, NOW(), NOW())",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@barangayId", resident.BarangayId.Value);
        cmd.Parameters.AddWithValue("@purokId", resident.PurokId.Value);
        cmd.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? resident.HouseholdId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@firstName", resident.FirstName.Trim());
        cmd.Parameters.AddWithValue("@middleName", string.IsNullOrWhiteSpace(resident.MiddleName) ? DBNull.Value : resident.MiddleName.Trim());
        cmd.Parameters.AddWithValue("@lastName", resident.LastName.Trim());
        cmd.Parameters.AddWithValue("@sex", NormalizeSex(resident.Gender));
        cmd.Parameters.AddWithValue("@birthDate", resident.DateOfBirth.Date);
        cmd.Parameters.AddWithValue("@civilStatus", string.IsNullOrWhiteSpace(resident.CivilStatus) ? DBNull.Value : resident.CivilStatus.Trim());
        cmd.Parameters.AddWithValue("@contactNo", string.IsNullOrWhiteSpace(resident.ContactNo) ? DBNull.Value : resident.ContactNo.Trim());
        cmd.Parameters.AddWithValue("@status", NormalizeResidentStatus(resident.Status));
        cmd.Parameters.Add("@photo", MySqlDbType.LongBlob).Value = resident.PhotoBytes ?? (object)DBNull.Value;
        cmd.ExecuteNonQuery();

        int residentId = (int)cmd.LastInsertedId;
        AuditTrailService.LogTransactional(
            conn,
            tx,
            "Residents",
            "resident",
            residentId,
            "CREATE",
            null,
            new
            {
                ResidentId = residentId,
                resident.FirstName,
                resident.MiddleName,
                resident.LastName,
                resident.BarangayId,
                resident.PurokId,
                resident.HouseholdId
            },
            "Resident registered from household module.");

        tx.Commit();
        return residentId;
    }

    private static void InsertTransferHistory(
        MySqlConnection conn,
        MySqlTransaction tx,
        int residentId,
        ResidentLocationSnapshot beforeLocation,
        int? newPurokId,
        int? newHouseholdId,
        string? newAddress,
        string reason)
    {
        using var cmd = new MySqlCommand(
            @"INSERT INTO resident_transfer_history
                (resident_id, old_purok_id, old_household_id, old_address,
                 new_purok_id, new_household_id, new_address, transfer_reason,
                 transferred_by_user_id, transferred_at)
              VALUES
                (@residentId, @oldPurokId, @oldHouseholdId, @oldAddress,
                 @newPurokId, @newHouseholdId, @newAddress, @reason,
                 @userId, NOW())",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@residentId", residentId);
        cmd.Parameters.AddWithValue("@oldPurokId", beforeLocation.PurokId.HasValue ? beforeLocation.PurokId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@oldHouseholdId", beforeLocation.HouseholdId.HasValue ? beforeLocation.HouseholdId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@oldAddress", string.IsNullOrWhiteSpace(beforeLocation.AddressLabel) ? DBNull.Value : beforeLocation.AddressLabel);
        cmd.Parameters.AddWithValue("@newPurokId", newPurokId.HasValue ? newPurokId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@newHouseholdId", newHouseholdId.HasValue ? newHouseholdId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@newAddress", string.IsNullOrWhiteSpace(newAddress) ? DBNull.Value : newAddress);
        cmd.Parameters.AddWithValue("@reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason);
        cmd.Parameters.AddWithValue("@userId", UserSession.UserId > 0 ? UserSession.UserId : (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static ResidentIdentity? LoadResidentIdentity(MySqlConnection conn, MySqlTransaction tx, int residentId, int barangayId)
    {
        using var cmd = new MySqlCommand(
            @"SELECT resident_id, barangay_id
              FROM resident
              WHERE resident_id = @residentId
                AND barangay_id = @barangayId
                AND IFNULL(is_deleted,0) = 0
              LIMIT 1",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@residentId", residentId);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ResidentIdentity
        {
            ResidentId = Convert.ToInt32(reader["resident_id"]),
            BarangayId = Convert.ToInt32(reader["barangay_id"])
        };
    }

    private static TargetHouseholdSnapshot LoadTargetHousehold(MySqlConnection conn, MySqlTransaction tx, int householdId, int barangayId)
    {
        using var cmd = new MySqlCommand(
            @"SELECT h.household_id,
                     h.purok_id,
                     h.house_no,
                     h.street,
                     h.subdivision,
                     COALESCE(p.name, '') AS purok_name
              FROM household h
              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id
              WHERE h.household_id = @householdId
                AND h.barangay_id = @barangayId
              LIMIT 1",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Target household not found.");
        }

        string houseNo = Convert.ToString(reader["house_no"]) ?? string.Empty;
        string street = Convert.ToString(reader["street"]) ?? string.Empty;
        string subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty;
        string purokName = Convert.ToString(reader["purok_name"]) ?? string.Empty;
        return new TargetHouseholdSnapshot
        {
            HouseholdId = Convert.ToInt32(reader["household_id"]),
            PurokId = Convert.ToInt32(reader["purok_id"]),
            AddressLabel = HouseholdRepository.BuildAddressLabel(houseNo, street, subdivision, purokName)
        };
    }

    private static int ResolveHouseholdPurokId(int householdId, int barangayId)
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT purok_id
              FROM household
              WHERE household_id = @householdId
                AND barangay_id = @barangayId
              LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);
        object? value = cmd.ExecuteScalar();
        if (value == null || value == DBNull.Value)
        {
            throw new InvalidOperationException("Selected household is invalid.");
        }

        return Convert.ToInt32(value);
    }

    private static string ResolvePurokName(MySqlConnection conn, MySqlTransaction tx, int? purokId)
    {
        if (!purokId.HasValue || purokId.Value <= 0)
        {
            return string.Empty;
        }

        using var cmd = new MySqlCommand(
            "SELECT name FROM purok_sitio WHERE purok_id = @purokId LIMIT 1",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@purokId", purokId.Value);
        object? value = cmd.ExecuteScalar();
        return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value) ?? string.Empty;
    }

    private static string NormalizeSex(string? input)
    {
        string value = (input ?? string.Empty).Trim();
        if (value.Equals("Male", StringComparison.OrdinalIgnoreCase) || value.Equals("M", StringComparison.OrdinalIgnoreCase))
        {
            return "M";
        }
        if (value.Equals("Female", StringComparison.OrdinalIgnoreCase) || value.Equals("F", StringComparison.OrdinalIgnoreCase))
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
            _ => "ACTIVE"
        };
    }

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
}
