using System;
using System.Collections.Generic;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal sealed class HouseholdListFilters
{
    public int BarangayId { get; init; }
    public string SearchText { get; init; } = string.Empty;
    public int? PurokId { get; init; }
    public bool WithSeniors { get; init; }
    public bool WithPwd { get; init; }
    public bool With4Ps { get; init; }
    public bool EmptyHouseholdOnly { get; init; }
    public bool HasActiveCasesOnly { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

internal sealed class HouseholdListItem
{
    public int HouseholdId { get; init; }
    public string HouseNo { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Subdivision { get; init; } = string.Empty;
    public int PurokId { get; init; }
    public string PurokName { get; init; } = string.Empty;
    public int MemberCount { get; init; }
    public int SeniorCount { get; init; }
    public int PwdCount { get; init; }
    public int FourPsCount { get; init; }
    public int VoterCount { get; init; }
    public int ActiveCaseCount { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

internal sealed class HouseholdPageResult
{
    public IReadOnlyList<HouseholdListItem> Items { get; init; } = Array.Empty<HouseholdListItem>();
    public int TotalRows { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRows / (double)PageSize);
}

internal sealed class HouseholdDetailsDto
{
    public int HouseholdId { get; init; }
    public int BarangayId { get; init; }
    public int PurokId { get; init; }
    public string PurokName { get; init; } = string.Empty;
    public string HouseNo { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Subdivision { get; init; } = string.Empty;
    public string AddressNote { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public int MemberCount { get; init; }
    public int SeniorCount { get; init; }
    public int PwdCount { get; init; }
    public int FourPsCount { get; init; }
    public int VoterCount { get; init; }
    public int ActiveCaseCount { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string FullAddress => HouseholdRepository.BuildAddressLabel(HouseNo, Street, Subdivision, PurokName);
}

internal sealed class HouseholdEditRecord
{
    public int HouseholdId { get; init; }
    public int BarangayId { get; init; }
    public int PurokId { get; init; }
    public string HouseNo { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Subdivision { get; init; } = string.Empty;
    public string AddressNote { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

internal sealed class HouseholdMemberRecord
{
    public int ResidentId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public int? Age { get; init; }
    public string Sex { get; init; } = string.Empty;
    public string CivilStatus { get; init; } = string.Empty;
    public string ContactNo { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool HasPhoto { get; init; }
}

internal sealed class HouseholdTransferHistoryItem
{
    public long TransferId { get; init; }
    public int ResidentId { get; init; }
    public string ResidentName { get; init; } = string.Empty;
    public string OldAddress { get; init; } = string.Empty;
    public string NewAddress { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string TransferredBy { get; init; } = string.Empty;
    public DateTime? TransferredAt { get; init; }
}

internal sealed class ResidentPickerItem
{
    public int ResidentId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string ContactNo { get; init; } = string.Empty;
    public string CurrentAddress { get; init; } = string.Empty;
    public int? CurrentHouseholdId { get; init; }
    public int? CurrentPurokId { get; init; }
}

internal sealed class HouseholdSaveRequest
{
    public int BarangayId { get; init; }
    public int PurokId { get; init; }
    public string HouseNo { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Subdivision { get; init; } = string.Empty;
    public string AddressNote { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

internal sealed class ResidentLocationSnapshot
{
    public int? PurokId { get; init; }
    public int? HouseholdId { get; init; }
    public string AddressLabel { get; init; } = string.Empty;
}

internal sealed class HouseholdRepository
{
    private const int MaxPageSize = 200;

    private const string MemberStatsJoin = @"
LEFT JOIN (
    SELECT r.household_id,
           COUNT(*) AS total_members,
           SUM(CASE WHEN IFNULL(r.is_senior,0) = 1 THEN 1 ELSE 0 END) AS seniors,
           SUM(CASE WHEN IFNULL(r.is_pwd,0) = 1 THEN 1 ELSE 0 END) AS pwd_members,
           SUM(CASE WHEN IFNULL(r.is_4ps_beneficiary,0) = 1 THEN 1 ELSE 0 END) AS four_ps_members,
           SUM(CASE WHEN IFNULL(r.is_registered_voter,0) = 1 THEN 1 ELSE 0 END) AS voters
    FROM resident r
    WHERE IFNULL(r.is_deleted,0) = 0
      AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')
    GROUP BY r.household_id
) ms ON ms.household_id = h.household_id";

    private const string ActiveCaseJoin = @"
LEFT JOIN (
    SELECT rr.household_id,
           COUNT(DISTINCT cr.case_id) AS active_cases
    FROM resident rr
    INNER JOIN case_record cr ON cr.complainant_id = rr.resident_id
    WHERE IFNULL(rr.is_deleted,0) = 0
      AND UPPER(cr.status) IN ('OPEN', 'ONGOING')
    GROUP BY rr.household_id
) cs ON cs.household_id = h.household_id";

    private const string ListWhereClause = @"
WHERE h.barangay_id = @barangayId
  AND (@purokId IS NULL OR h.purok_id = @purokId)
  AND (@searchText = '' OR
       COALESCE(h.house_no, '') LIKE @searchLike OR
       COALESCE(h.street, '') LIKE @searchLike OR
       COALESCE(h.subdivision, '') LIKE @searchLike OR
       EXISTS (
           SELECT 1
           FROM resident rs
           WHERE rs.household_id = h.household_id
             AND IFNULL(rs.is_deleted,0) = 0
             AND (rs.status IS NULL OR UPPER(rs.status) = 'ACTIVE')
             AND CONCAT_WS(' ', rs.first_name, rs.middle_name, rs.last_name) LIKE @searchLike
       ))
  AND (@withSeniors = 0 OR COALESCE(ms.seniors, 0) > 0)
  AND (@withPwd = 0 OR COALESCE(ms.pwd_members, 0) > 0)
  AND (@with4Ps = 0 OR COALESCE(ms.four_ps_members, 0) > 0)
  AND (@emptyOnly = 0 OR COALESCE(ms.total_members, 0) = 0)
  AND (@hasActiveCases = 0 OR COALESCE(cs.active_cases, 0) > 0)";

    public HouseholdPageResult Search(HouseholdListFilters filters)
    {
        HouseholdListFilters safeFilters = NormalizeFilters(filters);
        var items = new List<HouseholdListItem>();
        int totalRows;

        using var conn = DBConnection.GetConnection();
        conn.Open();

        using (var countCmd = new MySqlCommand(
                   @"SELECT COUNT(*)
                     FROM household h
                     LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id
                     " + MemberStatsJoin + ActiveCaseJoin + ListWhereClause, conn))
        {
            AddListParameters(countCmd, safeFilters);
            totalRows = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);
        }

        using (var cmd = new MySqlCommand(
                   @"SELECT h.household_id,
                            h.house_no,
                            h.street,
                            h.subdivision,
                            h.purok_id,
                            COALESCE(p.name, '') AS purok_name,
                            COALESCE(ms.total_members, 0) AS members,
                            COALESCE(ms.seniors, 0) AS seniors,
                            COALESCE(ms.pwd_members, 0) AS pwd_members,
                            COALESCE(ms.four_ps_members, 0) AS four_ps_members,
                            COALESCE(ms.voters, 0) AS voters,
                            COALESCE(cs.active_cases, 0) AS active_cases,
                            h.updated_at
                     FROM household h
                     LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id
                     " + MemberStatsJoin + ActiveCaseJoin + ListWhereClause + @"
                     ORDER BY h.updated_at DESC, h.household_id DESC
                     LIMIT @take OFFSET @skip", conn))
        {
            AddListParameters(cmd, safeFilters);
            cmd.Parameters.AddWithValue("@take", safeFilters.PageSize);
            cmd.Parameters.AddWithValue("@skip", (safeFilters.PageNumber - 1) * safeFilters.PageSize);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new HouseholdListItem
                {
                    HouseholdId = Convert.ToInt32(reader["household_id"]),
                    HouseNo = Convert.ToString(reader["house_no"]) ?? string.Empty,
                    Street = Convert.ToString(reader["street"]) ?? string.Empty,
                    Subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty,
                    PurokId = Convert.ToInt32(reader["purok_id"]),
                    PurokName = Convert.ToString(reader["purok_name"]) ?? string.Empty,
                    MemberCount = Convert.ToInt32(reader["members"]),
                    SeniorCount = Convert.ToInt32(reader["seniors"]),
                    PwdCount = Convert.ToInt32(reader["pwd_members"]),
                    FourPsCount = Convert.ToInt32(reader["four_ps_members"]),
                    VoterCount = Convert.ToInt32(reader["voters"]),
                    ActiveCaseCount = Convert.ToInt32(reader["active_cases"]),
                    UpdatedAt = reader["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["updated_at"])
                });
            }
        }

        return new HouseholdPageResult
        {
            Items = items,
            TotalRows = totalRows,
            PageNumber = safeFilters.PageNumber,
            PageSize = safeFilters.PageSize
        };
    }

    public HouseholdDetailsDto? GetDetails(int householdId, int barangayId)
    {
        if (householdId <= 0)
        {
            return null;
        }

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT h.household_id,
                     h.barangay_id,
                     h.purok_id,
                     COALESCE(p.name, '') AS purok_name,
                     h.house_no,
                     h.street,
                     h.subdivision,
                     h.address_note,
                     h.latitude,
                     h.longitude,
                     COALESCE(ms.total_members, 0) AS members,
                     COALESCE(ms.seniors, 0) AS seniors,
                     COALESCE(ms.pwd_members, 0) AS pwd_members,
                     COALESCE(ms.four_ps_members, 0) AS four_ps_members,
                     COALESCE(ms.voters, 0) AS voters,
                     COALESCE(cs.active_cases, 0) AS active_cases,
                     h.updated_at
              FROM household h
              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id
              " + MemberStatsJoin + ActiveCaseJoin + @"
              WHERE h.household_id = @householdId
                AND h.barangay_id = @barangayId
              LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new HouseholdDetailsDto
        {
            HouseholdId = Convert.ToInt32(reader["household_id"]),
            BarangayId = Convert.ToInt32(reader["barangay_id"]),
            PurokId = Convert.ToInt32(reader["purok_id"]),
            PurokName = Convert.ToString(reader["purok_name"]) ?? string.Empty,
            HouseNo = Convert.ToString(reader["house_no"]) ?? string.Empty,
            Street = Convert.ToString(reader["street"]) ?? string.Empty,
            Subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty,
            AddressNote = Convert.ToString(reader["address_note"]) ?? string.Empty,
            Latitude = reader["latitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["latitude"]),
            Longitude = reader["longitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["longitude"]),
            MemberCount = Convert.ToInt32(reader["members"]),
            SeniorCount = Convert.ToInt32(reader["seniors"]),
            PwdCount = Convert.ToInt32(reader["pwd_members"]),
            FourPsCount = Convert.ToInt32(reader["four_ps_members"]),
            VoterCount = Convert.ToInt32(reader["voters"]),
            ActiveCaseCount = Convert.ToInt32(reader["active_cases"]),
            UpdatedAt = reader["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["updated_at"])
        };
    }

    public HouseholdEditRecord? GetForEdit(int householdId, int barangayId)
    {
        if (householdId <= 0)
        {
            return null;
        }

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT household_id, barangay_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude
              FROM household
              WHERE household_id = @householdId
                AND barangay_id = @barangayId
              LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new HouseholdEditRecord
        {
            HouseholdId = Convert.ToInt32(reader["household_id"]),
            BarangayId = Convert.ToInt32(reader["barangay_id"]),
            PurokId = Convert.ToInt32(reader["purok_id"]),
            HouseNo = Convert.ToString(reader["house_no"]) ?? string.Empty,
            Street = Convert.ToString(reader["street"]) ?? string.Empty,
            Subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty,
            AddressNote = Convert.ToString(reader["address_note"]) ?? string.Empty,
            Latitude = reader["latitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["latitude"]),
            Longitude = reader["longitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["longitude"])
        };
    }

    public IReadOnlyList<LookupItem> GetPurokOptions(int barangayId)
    {
        int targetBarangayId = ResolveBarangayId(barangayId);
        var options = new List<LookupItem>();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT purok_id, name
              FROM purok_sitio
              WHERE barangay_id = @barangayId
              ORDER BY name", conn);
        cmd.Parameters.AddWithValue("@barangayId", targetBarangayId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            options.Add(new LookupItem(
                Convert.ToInt32(reader["purok_id"]),
                Convert.ToString(reader["name"]) ?? string.Empty));
        }

        return options;
    }

    public IReadOnlyList<LookupItem> GetHouseholdsForPurok(int barangayId, int? purokId, int? excludeHouseholdId = null)
    {
        int targetBarangayId = ResolveBarangayId(barangayId);
        var options = new List<LookupItem>();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT h.household_id,
                     h.house_no,
                     h.street,
                     h.subdivision,
                     COALESCE(p.name, '') AS purok_name
              FROM household h
              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id
              WHERE h.barangay_id = @barangayId
                AND (@purokId IS NULL OR h.purok_id = @purokId)
                AND (@excludeHouseholdId IS NULL OR h.household_id <> @excludeHouseholdId)
              ORDER BY COALESCE(h.street, ''), COALESCE(h.house_no, ''), h.household_id", conn);
        cmd.Parameters.AddWithValue("@barangayId", targetBarangayId);
        cmd.Parameters.AddWithValue("@purokId", (object?)purokId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@excludeHouseholdId", (object?)excludeHouseholdId ?? DBNull.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int householdId = Convert.ToInt32(reader["household_id"]);
            string houseNo = Convert.ToString(reader["house_no"]) ?? string.Empty;
            string street = Convert.ToString(reader["street"]) ?? string.Empty;
            string subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty;
            string purokName = Convert.ToString(reader["purok_name"]) ?? string.Empty;
            string label = BuildAddressLabel(houseNo, street, subdivision, purokName);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = $"Household #{householdId}";
            }

            options.Add(new LookupItem(householdId, label));
        }

        return options;
    }

    public int Create(HouseholdSaveRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        int barangayId = ResolveBarangayId(request.BarangayId);
        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        using var cmd = new MySqlCommand(
            @"INSERT INTO household
                (barangay_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude, created_at, updated_at)
              VALUES
                (@barangayId, @purokId, @houseNo, @street, @subdivision, @addressNote, @latitude, @longitude, NOW(), NOW())",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);
        cmd.Parameters.AddWithValue("@purokId", request.PurokId);
        cmd.Parameters.AddWithValue("@houseNo", ToDbValue(request.HouseNo));
        cmd.Parameters.AddWithValue("@street", ToDbValue(request.Street));
        cmd.Parameters.AddWithValue("@subdivision", ToDbValue(request.Subdivision));
        cmd.Parameters.AddWithValue("@addressNote", ToDbValue(request.AddressNote));
        cmd.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? request.Latitude.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? request.Longitude.Value : (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        int householdId = (int)cmd.LastInsertedId;
        object? afterSnapshot = ReadHouseholdAuditSnapshot(conn, tx, householdId);
        AuditTrailService.LogTransactional(
            conn,
            tx,
            "Households",
            "household",
            householdId,
            "CREATE",
            null,
            afterSnapshot,
            "Household created.");

        tx.Commit();
        return householdId;
    }

    public void Update(int householdId, HouseholdSaveRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        int barangayId = ResolveBarangayId(request.BarangayId);

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        object? beforeSnapshot = ReadHouseholdAuditSnapshot(conn, tx, householdId);

        using var cmd = new MySqlCommand(
            @"UPDATE household
              SET purok_id = @purokId,
                  house_no = @houseNo,
                  street = @street,
                  subdivision = @subdivision,
                  address_note = @addressNote,
                  latitude = @latitude,
                  longitude = @longitude,
                  updated_at = NOW()
              WHERE household_id = @householdId
                AND barangay_id = @barangayId",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@purokId", request.PurokId);
        cmd.Parameters.AddWithValue("@houseNo", ToDbValue(request.HouseNo));
        cmd.Parameters.AddWithValue("@street", ToDbValue(request.Street));
        cmd.Parameters.AddWithValue("@subdivision", ToDbValue(request.Subdivision));
        cmd.Parameters.AddWithValue("@addressNote", ToDbValue(request.AddressNote));
        cmd.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? request.Latitude.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? request.Longitude.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);

        int rows = cmd.ExecuteNonQuery();
        if (rows <= 0)
        {
            throw new InvalidOperationException("Household not found or no longer belongs to this barangay.");
        }

        object? afterSnapshot = ReadHouseholdAuditSnapshot(conn, tx, householdId);
        AuditTrailService.LogTransactional(
            conn,
            tx,
            "Households",
            "household",
            householdId,
            "UPDATE",
            beforeSnapshot,
            afterSnapshot,
            "Household updated.");

        tx.Commit();
    }

    public bool TryDelete(int householdId, int barangayId, out string message)
    {
        message = string.Empty;
        int targetBarangayId = ResolveBarangayId(barangayId);

        using var conn = DBConnection.GetConnection();
        conn.Open();

        using (var membersCmd = new MySqlCommand(
                   @"SELECT COUNT(*)
                     FROM resident
                     WHERE household_id = @householdId
                       AND IFNULL(is_deleted,0) = 0",
                   conn))
        {
            membersCmd.Parameters.AddWithValue("@householdId", householdId);
            int members = Convert.ToInt32(membersCmd.ExecuteScalar() ?? 0);
            if (members > 0)
            {
                message = "Cannot delete a household with assigned members.";
                return false;
            }
        }

        using var tx = conn.BeginTransaction();
        object? beforeSnapshot = ReadHouseholdAuditSnapshot(conn, tx, householdId);
        using var deleteCmd = new MySqlCommand(
            @"DELETE FROM household
              WHERE household_id = @householdId
                AND barangay_id = @barangayId",
            conn,
            tx);
        deleteCmd.Parameters.AddWithValue("@householdId", householdId);
        deleteCmd.Parameters.AddWithValue("@barangayId", targetBarangayId);

        int rows = deleteCmd.ExecuteNonQuery();
        if (rows <= 0)
        {
            tx.Rollback();
            message = "Household not found.";
            return false;
        }

        AuditTrailService.LogTransactional(
            conn,
            tx,
            "Households",
            "household",
            householdId,
            "DELETE",
            beforeSnapshot,
            null,
            "Household deleted.");
        tx.Commit();

        return true;
    }

    public bool ExistsDuplicateAddress(int barangayId, int purokId, string? houseNo, string? street, int? excludeHouseholdId = null)
    {
        int targetBarangayId = ResolveBarangayId(barangayId);
        string normalizedHouseNo = (houseNo ?? string.Empty).Trim();
        string normalizedStreet = (street ?? string.Empty).Trim();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT COUNT(*)
              FROM household
              WHERE barangay_id = @barangayId
                AND purok_id = @purokId
                AND UPPER(TRIM(COALESCE(house_no, ''))) = UPPER(@houseNo)
                AND UPPER(TRIM(COALESCE(street, ''))) = UPPER(@street)
                AND (@excludeId IS NULL OR household_id <> @excludeId)",
            conn);
        cmd.Parameters.AddWithValue("@barangayId", targetBarangayId);
        cmd.Parameters.AddWithValue("@purokId", purokId);
        cmd.Parameters.AddWithValue("@houseNo", normalizedHouseNo);
        cmd.Parameters.AddWithValue("@street", normalizedStreet);
        cmd.Parameters.AddWithValue("@excludeId", (object?)excludeHouseholdId ?? DBNull.Value);

        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
    }

    public IReadOnlyList<HouseholdMemberRecord> GetMembers(int householdId, int barangayId)
    {
        int targetBarangayId = ResolveBarangayId(barangayId);
        var members = new List<HouseholdMemberRecord>();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT r.resident_id,
                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,
                     CASE
                       WHEN r.birth_date IS NULL THEN NULL
                       ELSE TIMESTAMPDIFF(YEAR, r.birth_date, CURDATE())
                     END AS age,
                     COALESCE(r.sex, '') AS sex,
                     COALESCE(r.civil_status, '') AS civil_status,
                     COALESCE(r.contact_no, '') AS contact_no,
                     COALESCE(r.status, 'ACTIVE') AS status,
                     r.photo
              FROM resident r
              WHERE r.household_id = @householdId
                AND r.barangay_id = @barangayId
                AND IFNULL(r.is_deleted, 0) = 0
              ORDER BY r.last_name, r.first_name, r.middle_name",
            conn);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        cmd.Parameters.AddWithValue("@barangayId", targetBarangayId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            members.Add(new HouseholdMemberRecord
            {
                ResidentId = Convert.ToInt32(reader["resident_id"]),
                FullName = Convert.ToString(reader["full_name"]) ?? string.Empty,
                Age = reader["age"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["age"]),
                Sex = Convert.ToString(reader["sex"]) ?? string.Empty,
                CivilStatus = Convert.ToString(reader["civil_status"]) ?? string.Empty,
                ContactNo = Convert.ToString(reader["contact_no"]) ?? string.Empty,
                Status = Convert.ToString(reader["status"]) ?? string.Empty,
                HasPhoto = reader["photo"] != DBNull.Value
            });
        }

        return members;
    }

    public IReadOnlyList<HouseholdTransferHistoryItem> GetTransferHistory(int householdId, int barangayId)
    {
        int targetBarangayId = ResolveBarangayId(barangayId);
        var history = new List<HouseholdTransferHistoryItem>();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT th.transfer_id,
                     th.resident_id,
                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS resident_name,
                     COALESCE(th.old_address, '') AS old_address,
                     COALESCE(th.new_address, '') AS new_address,
                     COALESCE(th.transfer_reason, '') AS transfer_reason,
                     COALESCE(NULLIF(ua.full_name, ''), ua.username, CONCAT('User #', th.transferred_by_user_id)) AS transferred_by,
                     th.transferred_at
              FROM resident_transfer_history th
              INNER JOIN resident r ON r.resident_id = th.resident_id
              LEFT JOIN user_account ua ON ua.user_id = th.transferred_by_user_id
              WHERE r.barangay_id = @barangayId
                AND (th.old_household_id = @householdId OR th.new_household_id = @householdId)
              ORDER BY th.transferred_at DESC, th.transfer_id DESC",
            conn);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        cmd.Parameters.AddWithValue("@barangayId", targetBarangayId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            history.Add(new HouseholdTransferHistoryItem
            {
                TransferId = Convert.ToInt64(reader["transfer_id"]),
                ResidentId = Convert.ToInt32(reader["resident_id"]),
                ResidentName = Convert.ToString(reader["resident_name"]) ?? string.Empty,
                OldAddress = Convert.ToString(reader["old_address"]) ?? string.Empty,
                NewAddress = Convert.ToString(reader["new_address"]) ?? string.Empty,
                Reason = Convert.ToString(reader["transfer_reason"]) ?? string.Empty,
                TransferredBy = Convert.ToString(reader["transferred_by"]) ?? string.Empty,
                TransferredAt = reader["transferred_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["transferred_at"])
            });
        }

        return history;
    }

    public IReadOnlyList<ResidentPickerItem> GetResidentsForHouseholdPicker(int barangayId, int targetHouseholdId, string? searchText)
    {
        int targetBarangayId = ResolveBarangayId(barangayId);
        string search = (searchText ?? string.Empty).Trim();
        string searchLike = $"%{search}%";
        var residents = new List<ResidentPickerItem>();

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            @"SELECT r.resident_id,
                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,
                     COALESCE(r.contact_no, '') AS contact_no,
                     r.household_id,
                     r.purok_id,
                     COALESCE(h.house_no, '') AS house_no,
                     COALESCE(h.street, '') AS street,
                     COALESCE(h.subdivision, '') AS subdivision,
                     COALESCE(p.name, '') AS purok_name
              FROM resident r
              LEFT JOIN household h ON h.household_id = r.household_id
              LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id
              WHERE r.barangay_id = @barangayId
                AND IFNULL(r.is_deleted, 0) = 0
                AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')
                AND (r.household_id IS NULL OR r.household_id <> @targetHouseholdId)
                AND (@searchText = '' OR
                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @searchLike OR
                     COALESCE(r.contact_no, '') LIKE @searchLike)
              ORDER BY r.last_name, r.first_name, r.middle_name
              LIMIT 200",
            conn);
        cmd.Parameters.AddWithValue("@barangayId", targetBarangayId);
        cmd.Parameters.AddWithValue("@targetHouseholdId", targetHouseholdId);
        cmd.Parameters.AddWithValue("@searchText", search);
        cmd.Parameters.AddWithValue("@searchLike", searchLike);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string houseNo = Convert.ToString(reader["house_no"]) ?? string.Empty;
            string street = Convert.ToString(reader["street"]) ?? string.Empty;
            string subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty;
            string purokName = Convert.ToString(reader["purok_name"]) ?? string.Empty;
            residents.Add(new ResidentPickerItem
            {
                ResidentId = Convert.ToInt32(reader["resident_id"]),
                FullName = Convert.ToString(reader["full_name"]) ?? string.Empty,
                ContactNo = Convert.ToString(reader["contact_no"]) ?? string.Empty,
                CurrentHouseholdId = reader["household_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["household_id"]),
                CurrentPurokId = reader["purok_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["purok_id"]),
                CurrentAddress = BuildAddressLabel(houseNo, street, subdivision, purokName)
            });
        }

        return residents;
    }

    public ResidentLocationSnapshot GetResidentLocationSnapshot(MySqlConnection conn, MySqlTransaction tx, int residentId)
    {
        using var cmd = new MySqlCommand(
            @"SELECT r.purok_id,
                     r.household_id,
                     COALESCE(p.name, '') AS purok_name,
                     COALESCE(h.house_no, '') AS house_no,
                     COALESCE(h.street, '') AS street,
                     COALESCE(h.subdivision, '') AS subdivision
              FROM resident r
              LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id
              LEFT JOIN household h ON h.household_id = r.household_id
              WHERE r.resident_id = @residentId
              LIMIT 1",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@residentId", residentId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return new ResidentLocationSnapshot();
        }

        int? purokId = reader["purok_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["purok_id"]);
        int? householdId = reader["household_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["household_id"]);
        string purokName = Convert.ToString(reader["purok_name"]) ?? string.Empty;
        string houseNo = Convert.ToString(reader["house_no"]) ?? string.Empty;
        string street = Convert.ToString(reader["street"]) ?? string.Empty;
        string subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty;

        return new ResidentLocationSnapshot
        {
            PurokId = purokId,
            HouseholdId = householdId,
            AddressLabel = BuildAddressLabel(houseNo, street, subdivision, purokName)
        };
    }

    public static int ResolveBarangayId(int barangayId)
    {
        if (barangayId > 0)
        {
            return barangayId;
        }

        if (UserSession.BarangayId > 0)
        {
            return UserSession.BarangayId;
        }

        return SchemaDefaults.DefaultBarangayId;
    }

    public static string BuildAddressLabel(string? houseNo, string? street, string? subdivision, string? purokName)
    {
        string h = (houseNo ?? string.Empty).Trim();
        string s = (street ?? string.Empty).Trim();
        string sub = (subdivision ?? string.Empty).Trim();
        string purok = (purokName ?? string.Empty).Trim();

        var left = new List<string>();
        if (!string.IsNullOrWhiteSpace(h))
        {
            left.Add(h);
        }
        if (!string.IsNullOrWhiteSpace(s))
        {
            left.Add(s);
        }
        if (!string.IsNullOrWhiteSpace(sub))
        {
            left.Add(sub);
        }

        string address = string.Join(", ", left);
        if (!string.IsNullOrWhiteSpace(address) && !string.IsNullOrWhiteSpace(purok))
        {
            return address + ", " + purok;
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            return address;
        }

        return purok;
    }

    private static HouseholdListFilters NormalizeFilters(HouseholdListFilters filters)
    {
        HouseholdListFilters source = filters ?? new HouseholdListFilters();
        int page = source.PageNumber <= 0 ? 1 : source.PageNumber;
        int size = source.PageSize <= 0 ? 25 : source.PageSize;
        size = Math.Min(size, MaxPageSize);

        return new HouseholdListFilters
        {
            BarangayId = ResolveBarangayId(source.BarangayId),
            SearchText = (source.SearchText ?? string.Empty).Trim(),
            PurokId = source.PurokId,
            WithSeniors = source.WithSeniors,
            WithPwd = source.WithPwd,
            With4Ps = source.With4Ps,
            EmptyHouseholdOnly = source.EmptyHouseholdOnly,
            HasActiveCasesOnly = source.HasActiveCasesOnly,
            PageNumber = page,
            PageSize = size
        };
    }

    private static void AddListParameters(MySqlCommand cmd, HouseholdListFilters filters)
    {
        string searchText = filters.SearchText ?? string.Empty;
        cmd.Parameters.AddWithValue("@barangayId", filters.BarangayId);
        cmd.Parameters.AddWithValue("@purokId", (object?)filters.PurokId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@searchText", searchText);
        cmd.Parameters.AddWithValue("@searchLike", $"%{searchText}%");
        cmd.Parameters.AddWithValue("@withSeniors", filters.WithSeniors ? 1 : 0);
        cmd.Parameters.AddWithValue("@withPwd", filters.WithPwd ? 1 : 0);
        cmd.Parameters.AddWithValue("@with4Ps", filters.With4Ps ? 1 : 0);
        cmd.Parameters.AddWithValue("@emptyOnly", filters.EmptyHouseholdOnly ? 1 : 0);
        cmd.Parameters.AddWithValue("@hasActiveCases", filters.HasActiveCasesOnly ? 1 : 0);
    }

    private static object ToDbValue(string? value)
    {
        string cleaned = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? DBNull.Value : cleaned;
    }

    private static object? ReadHouseholdAuditSnapshot(MySqlConnection conn, MySqlTransaction? tx, int householdId)
    {
        using var cmd = new MySqlCommand(
            @"SELECT household_id, barangay_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude, updated_at
              FROM household
              WHERE household_id = @householdId
              LIMIT 1",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@householdId", householdId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new
        {
            HouseholdId = Convert.ToInt32(reader["household_id"]),
            BarangayId = Convert.ToInt32(reader["barangay_id"]),
            PurokId = Convert.ToInt32(reader["purok_id"]),
            HouseNo = Convert.ToString(reader["house_no"]) ?? string.Empty,
            Street = Convert.ToString(reader["street"]) ?? string.Empty,
            Subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty,
            AddressNote = Convert.ToString(reader["address_note"]) ?? string.Empty,
            Latitude = reader["latitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["latitude"]),
            Longitude = reader["longitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["longitude"]),
            UpdatedAt = reader["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["updated_at"])
        };
    }
}
