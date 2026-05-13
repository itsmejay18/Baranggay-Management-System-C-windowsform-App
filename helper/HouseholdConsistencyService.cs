using System;
using System.Data;
using baranggaysystem1.Database;

namespace baranggaysystem1.helper;

internal sealed class HouseholdConsistencyViolation
{
    internal string Message { get; init; } = string.Empty;
    internal string Title { get; init; } = "Household consistency";
}

internal static class HouseholdConsistencyService
{
    internal static HouseholdConsistencyViolation? Validate(ResidentDto resident, int? excludeResidentId = null)
    {
        if (resident == null)
        {
            return null;
        }

        if (!resident.HouseholdId.HasValue || resident.HouseholdId.Value <= 0)
        {
            return null;
        }

        int householdId = resident.HouseholdId.Value;
        int barangayId = resident.BarangayId ?? SchemaDefaults.DefaultBarangayId;
        int purokId = resident.PurokId ?? SchemaDefaults.DefaultPurokId;
        string normalizedStatus = NormalizeResidentStatus(resident.Status);

        const string householdSql = @"
SELECT barangay_id, purok_id
FROM household
WHERE household_id = @householdId
LIMIT 1";

        DataTable householdTable = DbHelper.LoadTable(
            householdSql,
            cmd => cmd.Parameters.AddWithValue("@householdId", householdId));
        if (householdTable.Rows.Count == 0)
        {
            return new HouseholdConsistencyViolation
            {
                Message = $"Household #{householdId} does not exist.",
                Title = "Invalid household"
            };
        }

        DataRow householdRow = householdTable.Rows[0];
        int householdBarangayId = Convert.ToInt32(householdRow["barangay_id"]);
        int householdPurokId = Convert.ToInt32(householdRow["purok_id"]);

        if (householdBarangayId != barangayId || householdPurokId != purokId)
        {
            return new HouseholdConsistencyViolation
            {
                Message = "Selected household does not belong to the resident's barangay/purok.",
                Title = "Household mismatch"
            };
        }

        const string activeMembersSql = @"
SELECT COUNT(*)
FROM resident
WHERE household_id = @householdId
  AND status = 'ACTIVE'
  AND (@excludeResidentId IS NULL OR resident_id <> @excludeResidentId)";

        int activeMembersExcludingCurrent = DbHelper.ExecuteScalar<int>(
            activeMembersSql,
            cmd =>
            {
                cmd.Parameters.AddWithValue("@householdId", householdId);
                cmd.Parameters.AddWithValue(
                    "@excludeResidentId",
                    excludeResidentId.HasValue ? excludeResidentId.Value : (object)DBNull.Value);
            });

        bool residentIsActive = string.Equals(normalizedStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase);
        if (!residentIsActive && activeMembersExcludingCurrent <= 0)
        {
            return new HouseholdConsistencyViolation
            {
                Message = "Household must have at least one active member.",
                Title = "Invalid active status"
            };
        }

        return null;
    }

    private static string NormalizeResidentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "ACTIVE";
        }

        string value = status.Trim();
        if (value.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return "ACTIVE";
        }

        if (value.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
        {
            return "MOVED_OUT";
        }

        if (value.Equals("Deceased", StringComparison.OrdinalIgnoreCase))
        {
            return "DECEASED";
        }

        return value.ToUpperInvariant();
    }
}
