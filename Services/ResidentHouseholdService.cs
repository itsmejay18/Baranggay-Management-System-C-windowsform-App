using System;
using System.Collections.Generic;
using System.Data;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Service for managing resident-household relationships (transfers, assignments).
/// </summary>
public sealed class ResidentHouseholdService
{
    /// <summary>
    /// Transfers a resident to a different household.
    /// </summary>
    public void TransferResident(int residentId, int fromHouseholdId, int targetHouseholdId, string? reason = null)
    {
        DbHelper.ExecuteNonQuery(
            @"UPDATE resident SET household_id = @householdId, updated_at = NOW()
              WHERE resident_id = @residentId",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@householdId", targetHouseholdId);
                cmd.Parameters.AddWithValue("@residentId", residentId);
            });

        // Log the transfer in household history
        DbHelper.ExecuteNonQuery(
            @"INSERT INTO household_history (resident_id, from_household_id, to_household_id, reason, transferred_at)
              VALUES (@residentId, @fromId, @toId, @reason, NOW())",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@residentId", residentId);
                cmd.Parameters.AddWithValue("@fromId", fromHouseholdId);
                cmd.Parameters.AddWithValue("@toId", targetHouseholdId);
                cmd.Parameters.AddWithValue("@reason", reason ?? (object)DBNull.Value);
            });

        // Update timestamps on both households
        DbHelper.ExecuteNonQuery(
            "UPDATE household SET updated_at = NOW() WHERE household_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", fromHouseholdId));

        DbHelper.ExecuteNonQuery(
            "UPDATE household SET updated_at = NOW() WHERE household_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", targetHouseholdId));
    }

    /// <summary>
    /// Assigns a resident to a household (initial assignment, no transfer history).
    /// </summary>
    public void AssignResident(int residentId, int householdId)
    {
        DbHelper.ExecuteNonQuery(
            @"UPDATE resident SET household_id = @householdId, updated_at = NOW()
              WHERE resident_id = @residentId",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@householdId", householdId);
                cmd.Parameters.AddWithValue("@residentId", residentId);
            });

        DbHelper.ExecuteNonQuery(
            "UPDATE household SET updated_at = NOW() WHERE household_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", householdId));
    }

    /// <summary>
    /// Removes a resident from their current household.
    /// </summary>
    public void RemoveFromHousehold(int residentId)
    {
        DbHelper.ExecuteNonQuery(
            @"UPDATE resident SET household_id = NULL, updated_at = NOW()
              WHERE resident_id = @residentId",
            cmd => cmd.Parameters.AddWithValue("@residentId", residentId));
    }

    /// <summary>
    /// Gets the transfer history for a household.
    /// </summary>
    public DataTable GetTransferHistory(int householdId)
    {
        return DbHelper.LoadTable(
            @"SELECT hh.transferred_at, hh.reason,
                     CONCAT(r.first_name, ' ', r.last_name) AS resident_name,
                     CASE WHEN hh.to_household_id = @id THEN 'Transferred In' ELSE 'Transferred Out' END AS direction
              FROM household_history hh
              INNER JOIN resident r ON r.resident_id = hh.resident_id
              WHERE hh.from_household_id = @id OR hh.to_household_id = @id
              ORDER BY hh.transferred_at DESC",
            cmd => cmd.Parameters.AddWithValue("@id", householdId));
    }

    /// <summary>
    /// Adds an existing resident to a household (4-arg overload with barangayId and reason).
    /// </summary>
    public void AddExistingResidentToHousehold(int residentId, int householdId, int barangayId, string? reason = null)
    {
        AssignResident(residentId, householdId);
    }

    /// <summary>
    /// Adds an existing resident to a household (same as AssignResident).
    /// </summary>
    public void AddExistingResidentToHousehold(int residentId, int householdId)
    {
        AssignResident(residentId, householdId);
    }

    /// <summary>
    /// Registers a new resident (accepts a ResidentDto).
    /// </summary>
    internal void RegisterResident(ResidentDto resident)
    {
        // Placeholder: In a full implementation, this would persist the resident
        // and assign them to the household specified in the DTO.
    }

    /// <summary>
    /// Placeholder for registering a new resident into a household.
    /// </summary>
    public void RegisterResident(int householdId, int? purokId, int barangayId)
    {
        // Placeholder: opens registration form with household pre-selected.
    }

    /// <summary>
    /// Removes a resident from a specific household (3-arg overload with barangayId and reason).
    /// </summary>
    public void RemoveResidentFromHousehold(int residentId, int barangayId, string? reason)
    {
        DbHelper.ExecuteNonQuery(
            @"UPDATE resident SET household_id = NULL, updated_at = NOW()
              WHERE resident_id = @residentId",
            cmd => cmd.Parameters.AddWithValue("@residentId", residentId));
    }

    /// <summary>
    /// Removes a resident from a specific household.
    /// </summary>
    public void RemoveResidentFromHousehold(int residentId, int householdId)
    {
        DbHelper.ExecuteNonQuery(
            @"UPDATE resident SET household_id = NULL, updated_at = NOW()
              WHERE resident_id = @residentId AND household_id = @householdId",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@residentId", residentId);
                cmd.Parameters.AddWithValue("@householdId", householdId);
            });

        DbHelper.ExecuteNonQuery(
            "UPDATE household SET updated_at = NOW() WHERE household_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", householdId));
    }
}
