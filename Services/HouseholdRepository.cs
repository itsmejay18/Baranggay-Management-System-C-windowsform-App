using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Repository for household data access operations.
/// </summary>
public sealed class HouseholdRepository
{
    public static int ResolveBarangayId(int? sessionBarangayId)
    {
        return sessionBarangayId ?? 1;
    }

    /// <summary>
    /// Gets a paginated list of households with optional filters.
    /// </summary>
    public (IReadOnlyList<HouseholdListItem> Items, int TotalCount) GetHouseholds(
        int barangayId,
        int page,
        int pageSize,
        string? search = null,
        int? purokId = null,
        bool? withSeniors = null,
        bool? withPwd = null,
        bool? with4Ps = null,
        bool? emptyOnly = null,
        bool? activeCases = null)
    {
        var filters = new HouseholdListFilters
        {
            BarangayId = barangayId,
            Search = search,
            PurokId = purokId,
            WithSeniors = withSeniors,
            WithPwd = withPwd,
            With4Ps = with4Ps,
            EmptyOnly = emptyOnly,
            ActiveCases = activeCases,
            PageNumber = page,
            PageSize = pageSize
        };
        var result = Search(filters);
        return (result.Items, result.TotalCount);
    }

    /// <summary>
    /// Gets detailed information about a specific household.
    /// </summary>
    public HouseholdDetailsDto? GetHouseholdDetails(int householdId, int barangayId)
    {
        var table = DbHelper.LoadTable(
            @"SELECT h.household_id, h.purok_id, h.house_no, h.street, h.subdivision,
                     COALESCE(p.purok_name, '') AS purok_name,
                     h.latitude, h.longitude, h.updated_at,
                     (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0) AS member_count,
                     (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_senior_citizen = 1) AS senior_count,
                     (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_pwd = 1) AS pwd_count,
                     (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_4ps = 1) AS four_ps_count,
                     (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_registered_voter = 1) AS voter_count,
                     (SELECT COUNT(*) FROM case_record cr
                      INNER JOIN resident r ON r.resident_id = cr.complainant_id
                      WHERE r.household_id = h.household_id AND cr.status IN ('OPEN','ONGOING')) AS active_case_count
              FROM household h
              LEFT JOIN purok p ON p.purok_id = h.purok_id
              WHERE h.household_id = @id AND h.barangay_id = @barangayId",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@id", householdId);
                cmd.Parameters.AddWithValue("@barangayId", barangayId);
            });

        if (table.Rows.Count == 0) return null;

        var row = table.Rows[0];
        string houseNo = row["house_no"]?.ToString() ?? string.Empty;
        string street = row["street"]?.ToString() ?? string.Empty;
        string subdivision = row["subdivision"]?.ToString() ?? string.Empty;
        string fullAddress = string.Join(", ", new[] { houseNo, street, subdivision }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new HouseholdDetailsDto
        {
            HouseholdId = Convert.ToInt32(row["household_id"]),
            PurokId = row["purok_id"] != DBNull.Value ? Convert.ToInt32(row["purok_id"]) : null,
            HouseNo = houseNo,
            Street = street,
            Subdivision = subdivision,
            PurokName = row["purok_name"]?.ToString() ?? string.Empty,
            FullAddress = fullAddress,
            Latitude = row["latitude"] != DBNull.Value ? Convert.ToDouble(row["latitude"]) : null,
            Longitude = row["longitude"] != DBNull.Value ? Convert.ToDouble(row["longitude"]) : null,
            UpdatedAt = row["updated_at"] != DBNull.Value ? Convert.ToDateTime(row["updated_at"]) : null,
            MemberCount = Convert.ToInt32(row["member_count"]),
            SeniorCount = Convert.ToInt32(row["senior_count"]),
            PwdCount = Convert.ToInt32(row["pwd_count"]),
            FourPsCount = Convert.ToInt32(row["four_ps_count"]),
            VoterCount = Convert.ToInt32(row["voter_count"]),
            ActiveCaseCount = Convert.ToInt32(row["active_case_count"])
        };
    }

    /// <summary>Alias for GetHouseholdDetails.</summary>
    public HouseholdDetailsDto? GetDetails(int householdId, int barangayId) => GetHouseholdDetails(householdId, barangayId);

    /// <summary>Creates a new household record.</summary>
    public int CreateHousehold(string houseNo, string street, string subdivision, int? purokId, int barangayId)
    {
        DbHelper.ExecuteNonQuery(
            @"INSERT INTO household (house_no, street, subdivision, purok_id, barangay_id, updated_at)
              VALUES (@houseNo, @street, @subdivision, @purokId, @barangayId, NOW())",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@houseNo", houseNo);
                cmd.Parameters.AddWithValue("@street", street);
                cmd.Parameters.AddWithValue("@subdivision", subdivision);
                cmd.Parameters.AddWithValue("@purokId", purokId.HasValue ? purokId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@barangayId", barangayId);
            });
        return DbHelper.ExecuteScalar<int>("SELECT LAST_INSERT_ID()");
    }

    /// <summary>Updates an existing household record.</summary>
    public void UpdateHousehold(int householdId, string houseNo, string street, string subdivision, int? purokId)
    {
        DbHelper.ExecuteNonQuery(
            @"UPDATE household SET house_no = @houseNo, street = @street, subdivision = @subdivision,
                  purok_id = @purokId, updated_at = NOW() WHERE household_id = @id",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@id", householdId);
                cmd.Parameters.AddWithValue("@houseNo", houseNo);
                cmd.Parameters.AddWithValue("@street", street);
                cmd.Parameters.AddWithValue("@subdivision", subdivision);
                cmd.Parameters.AddWithValue("@purokId", purokId.HasValue ? purokId.Value : DBNull.Value);
            });
    }

    /// <summary>Gets the list of puroks for a barangay.</summary>
    internal List<LookupItem> GetPurokOptions(int barangayId)
    {
        var table = DbHelper.LoadTable(
            "SELECT purok_id, purok_name FROM purok WHERE barangay_id = @barangayId ORDER BY purok_name",
            cmd => cmd.Parameters.AddWithValue("@barangayId", barangayId));
        var items = new List<LookupItem>();
        foreach (DataRow row in table.Rows)
            items.Add(new LookupItem(Convert.ToInt32(row["purok_id"]), row["purok_name"]?.ToString() ?? string.Empty));
        return items;
    }

    /// <summary>Paginated search using filters object (single-arg overload used by callers).</summary>
    public HouseholdPageResult Search(HouseholdListFilters filters)
    {
        return Search(filters, filters.PageNumber, filters.PageSize, filters.BarangayId);
    }

    /// <summary>Paginated search for households with filters.</summary>
    public HouseholdPageResult Search(HouseholdListFilters filters, int page, int pageSize, int barangayId)
    {
        var conditions = new List<string> { "h.barangay_id = @barangayId" };
        var parameters = new Dictionary<string, object> { { "@barangayId", barangayId } };

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            conditions.Add("(h.house_no LIKE @search OR h.street LIKE @search OR h.subdivision LIKE @search)");
            parameters["@search"] = $"%{filters.Search.Trim()}%";
        }
        if (filters.PurokId.HasValue)
        {
            conditions.Add("h.purok_id = @purokId");
            parameters["@purokId"] = filters.PurokId.Value;
        }
        if (filters.EmptyOnly == true)
            conditions.Add("(SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0) = 0");
        if (filters.WithSeniors == true)
            conditions.Add("(SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_senior_citizen = 1) > 0");
        if (filters.WithPwd == true)
            conditions.Add("(SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_pwd = 1) > 0");
        if (filters.With4Ps == true)
            conditions.Add("(SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_4ps = 1) > 0");
        if (filters.ActiveCases == true)
            conditions.Add("(SELECT COUNT(*) FROM case_record cr INNER JOIN resident r ON r.resident_id = cr.complainant_id WHERE r.household_id = h.household_id AND cr.status IN ('OPEN','ONGOING')) > 0");

        string whereClause = string.Join(" AND ", conditions);
        int totalCount = DbHelper.ExecuteScalar<int>($"SELECT COUNT(*) FROM household h WHERE {whereClause}",
            cmd => { foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value); });

        string query = $@"SELECT h.household_id, h.house_no, h.street, h.subdivision,
                   COALESCE(p.purok_name, '') AS purok_name,
                   (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0) AS member_count,
                   (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_senior_citizen = 1) AS senior_count,
                   (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_pwd = 1) AS pwd_count,
                   (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_4ps = 1) AS four_ps_count,
                   (SELECT COUNT(*) FROM resident r WHERE r.household_id = h.household_id AND IFNULL(r.is_deleted,0)=0 AND r.is_registered_voter = 1) AS voter_count,
                   (SELECT COUNT(*) FROM case_record cr INNER JOIN resident r ON r.resident_id = cr.complainant_id WHERE r.household_id = h.household_id AND cr.status IN ('OPEN','ONGOING')) AS active_case_count,
                   h.updated_at
            FROM household h LEFT JOIN purok p ON p.purok_id = h.purok_id
            WHERE {whereClause} ORDER BY h.updated_at DESC LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";

        var table = DbHelper.LoadTable(query, cmd => { foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value); });
        var items = new List<HouseholdListItem>();
        foreach (DataRow row in table.Rows)
        {
            items.Add(new HouseholdListItem
            {
                HouseholdId = Convert.ToInt32(row["household_id"]),
                HouseNo = row["house_no"]?.ToString() ?? string.Empty,
                Street = row["street"]?.ToString() ?? string.Empty,
                Subdivision = row["subdivision"]?.ToString() ?? string.Empty,
                PurokName = row["purok_name"]?.ToString() ?? string.Empty,
                MemberCount = Convert.ToInt32(row["member_count"]),
                SeniorCount = Convert.ToInt32(row["senior_count"]),
                PwdCount = Convert.ToInt32(row["pwd_count"]),
                FourPsCount = Convert.ToInt32(row["four_ps_count"]),
                VoterCount = Convert.ToInt32(row["voter_count"]),
                ActiveCaseCount = Convert.ToInt32(row["active_case_count"]),
                UpdatedAt = row["updated_at"] != DBNull.Value ? Convert.ToDateTime(row["updated_at"]) : null
            });
        }
        return new HouseholdPageResult { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    /// <summary>Gets a household record for editing (2-arg overload).</summary>
    public HouseholdEditRecord? GetForEdit(int householdId, int barangayId)
    {
        return GetForEdit(householdId);
    }

    /// <summary>Gets a household record for editing.</summary>
    public HouseholdEditRecord? GetForEdit(int householdId)
    {
        var table = DbHelper.LoadTable(
            "SELECT household_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude FROM household WHERE household_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", householdId));
        if (table.Rows.Count == 0) return null;
        var row = table.Rows[0];
        return new HouseholdEditRecord
        {
            HouseholdId = Convert.ToInt32(row["household_id"]),
            PurokId = row["purok_id"] != DBNull.Value ? Convert.ToInt32(row["purok_id"]) : null,
            HouseNo = row["house_no"]?.ToString() ?? string.Empty,
            Street = row["street"]?.ToString() ?? string.Empty,
            Subdivision = row["subdivision"]?.ToString() ?? string.Empty,
            AddressNote = row["address_note"] != DBNull.Value ? row["address_note"]?.ToString() : null,
            Latitude = row["latitude"] != DBNull.Value ? Convert.ToDouble(row["latitude"]) : null,
            Longitude = row["longitude"] != DBNull.Value ? Convert.ToDouble(row["longitude"]) : null
        };
    }

    /// <summary>Gets the members of a household (2-arg overload).</summary>
    public List<HouseholdMemberRecord> GetMembers(int householdId, int barangayId)
    {
        return GetMembers(householdId);
    }

    /// <summary>Gets the members of a household.</summary>
    public List<HouseholdMemberRecord> GetMembers(int householdId)
    {
        var table = DbHelper.LoadTable(
            @"SELECT r.resident_id, CONCAT(r.first_name, ' ', r.last_name) AS full_name,
                     r.relationship, r.is_household_head, r.status,
                     (r.photo IS NOT NULL AND LENGTH(r.photo) > 0) AS has_photo,
                     TIMESTAMPDIFF(YEAR, r.date_of_birth, CURDATE()) AS age,
                     r.gender AS sex, r.civil_status, r.contact_number AS contact_no
              FROM resident r
              WHERE r.household_id = @householdId AND IFNULL(r.is_deleted, 0) = 0
              ORDER BY r.is_household_head DESC, r.last_name, r.first_name",
            cmd => cmd.Parameters.AddWithValue("@householdId", householdId));

        var items = new List<HouseholdMemberRecord>();
        foreach (DataRow row in table.Rows)
        {
            items.Add(new HouseholdMemberRecord
            {
                ResidentId = Convert.ToInt32(row["resident_id"]),
                FullName = row["full_name"]?.ToString() ?? string.Empty,
                Relationship = row["relationship"] != DBNull.Value ? row["relationship"]?.ToString() : null,
                IsHead = row["is_household_head"] != DBNull.Value && Convert.ToBoolean(row["is_household_head"]),
                Status = row["status"] != DBNull.Value ? row["status"]?.ToString() : null,
                HasPhoto = row["has_photo"] != DBNull.Value && Convert.ToBoolean(row["has_photo"]),
                Age = row["age"] != DBNull.Value ? Convert.ToInt32(row["age"]) : null,
                Sex = row["sex"] != DBNull.Value ? row["sex"]?.ToString() : null,
                CivilStatus = row["civil_status"] != DBNull.Value ? row["civil_status"]?.ToString() : null,
                ContactNo = row["contact_no"] != DBNull.Value ? row["contact_no"]?.ToString() : null
            });
        }
        return items;
    }

    /// <summary>Gets the transfer history for a household (2-arg overload).</summary>
    public IReadOnlyList<HouseholdTransferHistoryItem> GetTransferHistory(int householdId, int barangayId)
    {
        return GetTransferHistory(householdId);
    }

    /// <summary>Gets the transfer history for a household.</summary>
    public List<HouseholdTransferHistoryItem> GetTransferHistory(int householdId)
    {
        var table = DbHelper.LoadTable(
            @"SELECT hh.resident_id, CONCAT(r.first_name, ' ', r.last_name) AS resident_name,
                     hh.from_household_id, hh.to_household_id, hh.reason, hh.transferred_at,
                     CONCAT(COALESCE(hf.house_no,''), ' ', COALESCE(hf.street,'')) AS old_address,
                     CONCAT(COALESCE(ht.house_no,''), ' ', COALESCE(ht.street,'')) AS new_address,
                     COALESCE(u.username, '') AS transferred_by
              FROM household_history hh
              INNER JOIN resident r ON r.resident_id = hh.resident_id
              LEFT JOIN household hf ON hf.household_id = hh.from_household_id
              LEFT JOIN household ht ON ht.household_id = hh.to_household_id
              LEFT JOIN users u ON u.user_id = hh.transferred_by_user_id
              WHERE hh.from_household_id = @id OR hh.to_household_id = @id
              ORDER BY hh.transferred_at DESC",
            cmd => cmd.Parameters.AddWithValue("@id", householdId));

        var items = new List<HouseholdTransferHistoryItem>();
        foreach (DataRow row in table.Rows)
        {
            items.Add(new HouseholdTransferHistoryItem
            {
                ResidentId = Convert.ToInt32(row["resident_id"]),
                ResidentName = row["resident_name"]?.ToString() ?? string.Empty,
                FromHouseholdId = row["from_household_id"] != DBNull.Value ? Convert.ToInt32(row["from_household_id"]) : null,
                ToHouseholdId = row["to_household_id"] != DBNull.Value ? Convert.ToInt32(row["to_household_id"]) : null,
                OldAddress = row["old_address"] != DBNull.Value ? row["old_address"]?.ToString()?.Trim() : null,
                NewAddress = row["new_address"] != DBNull.Value ? row["new_address"]?.ToString()?.Trim() : null,
                Reason = row["reason"] != DBNull.Value ? row["reason"]?.ToString() : null,
                TransferredAt = row["transferred_at"] != DBNull.Value ? Convert.ToDateTime(row["transferred_at"]) : null,
                TransferredBy = row["transferred_by"] != DBNull.Value ? row["transferred_by"]?.ToString() : null
            });
        }
        return items;
    }

    /// <summary>Gets residents for the household picker (3-arg overload with search).</summary>
    public IReadOnlyList<ResidentPickerItem> GetResidentsForHouseholdPicker(int barangayId, int? excludeHouseholdId, string? search)
    {
        string sql = @"SELECT r.resident_id, CONCAT(r.first_name, ' ', r.last_name) AS full_name,
                              COALESCE(p.purok_name, '') AS purok, r.status,
                              r.contact_number AS contact_no,
                              CONCAT(COALESCE(h.house_no,''), ' ', COALESCE(h.street,'')) AS current_address
                       FROM resident r
                       LEFT JOIN purok p ON p.purok_id = r.purok_id
                       LEFT JOIN household h ON h.household_id = r.household_id
                       WHERE r.barangay_id = @barangayId AND IFNULL(r.is_deleted, 0) = 0";

        if (excludeHouseholdId.HasValue)
            sql += " AND (r.household_id IS NULL OR r.household_id != @excludeId)";
        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND CONCAT(r.first_name, ' ', r.last_name) LIKE @search";
        sql += " ORDER BY r.last_name, r.first_name LIMIT 100";

        var table = DbHelper.LoadTable(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@barangayId", barangayId);
            if (excludeHouseholdId.HasValue)
                cmd.Parameters.AddWithValue("@excludeId", excludeHouseholdId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("@search", $"%{search.Trim()}%");
        });

        var items = new List<ResidentPickerItem>();
        foreach (DataRow row in table.Rows)
        {
            items.Add(new ResidentPickerItem
            {
                ResidentId = Convert.ToInt32(row["resident_id"]),
                FullName = row["full_name"]?.ToString() ?? string.Empty,
                Purok = row["purok"] != DBNull.Value ? row["purok"]?.ToString() : null,
                Status = row["status"] != DBNull.Value ? row["status"]?.ToString() : null,
                ContactNo = row["contact_no"] != DBNull.Value ? row["contact_no"]?.ToString() : null,
                CurrentAddress = row["current_address"] != DBNull.Value ? row["current_address"]?.ToString()?.Trim() : null
            });
        }
        return items;
    }

    /// <summary>Gets residents for the household picker (2-arg overload).</summary>
    public List<ResidentPickerItem> GetResidentsForHouseholdPicker(int barangayId, int? excludeHouseholdId = null)
    {
        return new List<ResidentPickerItem>(GetResidentsForHouseholdPicker(barangayId, excludeHouseholdId, null));
    }

    /// <summary>Gets households for a purok (3-arg overload: barangayId, purokId, excludeHouseholdId).</summary>
    internal IReadOnlyList<LookupItem> GetHouseholdsForPurok(int barangayId, int? purokId, int? excludeHouseholdId)
    {
        string sql = @"SELECT household_id, CONCAT(house_no, ' ', street) AS label
              FROM household WHERE barangay_id = @barangayId";
        if (purokId.HasValue)
            sql += " AND purok_id = @purokId";
        if (excludeHouseholdId.HasValue)
            sql += " AND household_id != @excludeId";
        sql += " ORDER BY house_no";

        var table = DbHelper.LoadTable(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@barangayId", barangayId);
            if (purokId.HasValue)
                cmd.Parameters.AddWithValue("@purokId", purokId.Value);
            if (excludeHouseholdId.HasValue)
                cmd.Parameters.AddWithValue("@excludeId", excludeHouseholdId.Value);
        });

        var items = new List<LookupItem>();
        foreach (DataRow row in table.Rows)
            items.Add(new LookupItem(Convert.ToInt32(row["household_id"]), row["label"]?.ToString() ?? string.Empty));
        return items;
    }

    /// <summary>Gets households in a specific purok (2-arg overload).</summary>
    internal List<LookupItem> GetHouseholdsForPurok(int purokId, int barangayId)
    {
        return new List<LookupItem>(GetHouseholdsForPurok(barangayId, purokId, null));
    }

    /// <summary>Updates a household from a save request (2-arg: householdId + request).</summary>
    public void Update(int householdId, HouseholdSaveRequest request)
    {
        request.HouseholdId = householdId;
        Update(request);
    }

    /// <summary>Updates a household from a save request.</summary>
    public void Update(HouseholdSaveRequest request)
    {
        if (request.HouseholdId.HasValue)
        {
            DbHelper.ExecuteNonQuery(
                @"UPDATE household SET house_no = @houseNo, street = @street, subdivision = @subdivision,
                      purok_id = @purokId, address_note = @addressNote, latitude = @lat, longitude = @lng, updated_at = NOW()
                  WHERE household_id = @id",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@id", request.HouseholdId.Value);
                    cmd.Parameters.AddWithValue("@houseNo", request.HouseNo);
                    cmd.Parameters.AddWithValue("@street", request.Street);
                    cmd.Parameters.AddWithValue("@subdivision", request.Subdivision);
                    cmd.Parameters.AddWithValue("@purokId", request.PurokId.HasValue ? request.PurokId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@addressNote", request.AddressNote ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@lat", request.Latitude.HasValue ? (object)(double)request.Latitude.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lng", request.Longitude.HasValue ? (object)(double)request.Longitude.Value : DBNull.Value);
                });
        }
        else
        {
            Create(request);
        }
    }

    /// <summary>Creates a new household from a save request.</summary>
    public int Create(HouseholdSaveRequest request)
    {
        DbHelper.ExecuteNonQuery(
            @"INSERT INTO household (house_no, street, subdivision, purok_id, address_note, latitude, longitude, barangay_id, updated_at)
              VALUES (@houseNo, @street, @subdivision, @purokId, @addressNote, @lat, @lng, @barangayId, NOW())",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@houseNo", request.HouseNo);
                cmd.Parameters.AddWithValue("@street", request.Street);
                cmd.Parameters.AddWithValue("@subdivision", request.Subdivision);
                cmd.Parameters.AddWithValue("@purokId", request.PurokId.HasValue ? request.PurokId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@addressNote", request.AddressNote ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@lat", request.Latitude.HasValue ? (object)(double)request.Latitude.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@lng", request.Longitude.HasValue ? (object)(double)request.Longitude.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@barangayId", request.BarangayId);
            });
        return DbHelper.ExecuteScalar<int>("SELECT LAST_INSERT_ID()");
    }

    /// <summary>Checks if a duplicate address exists (5-arg overload: barangayId, purokId, houseNo, street, excludeId).</summary>
    public bool ExistsDuplicateAddress(int barangayId, int purokId, string houseNo, string street, int? excludeId)
    {
        string sql = @"SELECT COUNT(*) FROM household
                       WHERE house_no = @houseNo AND street = @street AND barangay_id = @barangayId AND purok_id = @purokId";
        if (excludeId.HasValue)
            sql += " AND household_id != @excludeId";

        int count = DbHelper.ExecuteScalar<int>(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@houseNo", houseNo);
            cmd.Parameters.AddWithValue("@street", street);
            cmd.Parameters.AddWithValue("@barangayId", barangayId);
            cmd.Parameters.AddWithValue("@purokId", purokId);
            if (excludeId.HasValue)
                cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
        });
        return count > 0;
    }

    /// <summary>Checks if a duplicate address exists (4-arg overload).</summary>
    public bool ExistsDuplicateAddress(string houseNo, string street, int barangayId, int? excludeId = null)
    {
        string sql = @"SELECT COUNT(*) FROM household WHERE house_no = @houseNo AND street = @street AND barangay_id = @barangayId";
        if (excludeId.HasValue)
            sql += " AND household_id != @excludeId";
        int count = DbHelper.ExecuteScalar<int>(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@houseNo", houseNo);
            cmd.Parameters.AddWithValue("@street", street);
            cmd.Parameters.AddWithValue("@barangayId", barangayId);
            if (excludeId.HasValue)
                cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
        });
        return count > 0;
    }

    /// <summary>Tries to delete a household (3-arg overload with out message).</summary>
    public bool TryDelete(int householdId, int barangayId, out string message)
    {
        int memberCount = DbHelper.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM resident WHERE household_id = @id AND IFNULL(is_deleted, 0) = 0",
            cmd => cmd.Parameters.AddWithValue("@id", householdId));

        if (memberCount > 0)
        {
            message = $"Cannot delete household: it still has {memberCount} member(s).";
            return false;
        }

        DbHelper.ExecuteNonQuery(
            "DELETE FROM household WHERE household_id = @id AND barangay_id = @barangayId",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@id", householdId);
                cmd.Parameters.AddWithValue("@barangayId", barangayId);
            });
        message = string.Empty;
        return true;
    }

    /// <summary>Tries to delete a household. Returns false if it has members.</summary>
    public bool TryDelete(int householdId)
    {
        return TryDelete(householdId, 0, out _);
    }
}
