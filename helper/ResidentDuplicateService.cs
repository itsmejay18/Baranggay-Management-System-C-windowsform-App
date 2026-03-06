using System;
using baranggaysystem1.Database;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.helper;

internal sealed class ResidentDuplicateMatch
{
    internal int ResidentId { get; init; }
    internal string FullName { get; init; } = string.Empty;
    internal DateTime BirthDate { get; init; }
    internal string AddressLabel { get; init; } = string.Empty;
}

internal static class ResidentDuplicateService
{
    internal static ResidentDuplicateMatch? FindDuplicate(ResidentDto resident, int? excludeResidentId = null)
    {
        if (resident == null)
        {
            return null;
        }

        string firstName = resident.FirstName?.Trim() ?? string.Empty;
        string middleName = resident.MiddleName?.Trim() ?? string.Empty;
        string lastName = resident.LastName?.Trim() ?? string.Empty;
        int barangayId = resident.BarangayId ?? SchemaDefaults.DefaultBarangayId;
        int purokId = resident.PurokId ?? SchemaDefaults.DefaultPurokId;

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return null;
        }

        using MySqlConnection conn = DBConnection.GetConnection();
        conn.Open();

        const string sql = @"
SELECT
    r.resident_id,
    r.first_name,
    r.middle_name,
    r.last_name,
    r.birth_date,
    COALESCE(
        NULLIF(TRIM(CONCAT_WS(' ', h.house_no, h.street, h.subdivision)), ''),
        CONCAT('Purok #', r.purok_id)
    ) AS address_label
FROM resident r
LEFT JOIN household h ON h.household_id = r.household_id
WHERE
    UPPER(TRIM(r.first_name)) = UPPER(TRIM(@firstName))
    AND UPPER(TRIM(COALESCE(r.middle_name, ''))) = UPPER(TRIM(@middleName))
    AND UPPER(TRIM(r.last_name)) = UPPER(TRIM(@lastName))
    AND r.birth_date = @birthDate
    AND (@excludeResidentId IS NULL OR r.resident_id <> @excludeResidentId)
    AND
    (
        (@householdId IS NOT NULL AND r.household_id = @householdId)
        OR
        (r.barangay_id = @barangayId AND r.purok_id = @purokId)
    )
LIMIT 1";

        using MySqlCommand cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@firstName", firstName);
        cmd.Parameters.AddWithValue("@middleName", middleName);
        cmd.Parameters.AddWithValue("@lastName", lastName);
        cmd.Parameters.AddWithValue("@birthDate", resident.DateOfBirth.Date);
        cmd.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? resident.HouseholdId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@barangayId", barangayId);
        cmd.Parameters.AddWithValue("@purokId", purokId);
        cmd.Parameters.AddWithValue("@excludeResidentId", excludeResidentId.HasValue ? excludeResidentId.Value : (object)DBNull.Value);

        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ResidentDuplicateMatch
        {
            ResidentId = Convert.ToInt32(reader["resident_id"]),
            FullName = string.Join(" ", new[]
            {
                Convert.ToString(reader["first_name"]) ?? string.Empty,
                Convert.ToString(reader["middle_name"]) ?? string.Empty,
                Convert.ToString(reader["last_name"]) ?? string.Empty
            }).Replace("  ", " ").Trim(),
            BirthDate = reader["birth_date"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["birth_date"]),
            AddressLabel = Convert.ToString(reader["address_label"]) ?? string.Empty
        };
    }
}
