using System;
using System.Data;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1.helper;

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
		int num = resident.BarangayId ?? 1;
		int num2 = resident.PurokId ?? 1;
		string a = NormalizeResidentStatus(resident.Status);
		DataTable dataTable = DbHelper.LoadTable("\nSELECT barangay_id, purok_id\nFROM household\nWHERE household_id = @householdId\nLIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@householdId", (object)householdId);
		});
		if (dataTable.Rows.Count == 0)
		{
			return new HouseholdConsistencyViolation
			{
				Message = $"Household #{householdId} does not exist.",
				Title = "Invalid household"
			};
		}
		DataRow dataRow = dataTable.Rows[0];
		int num3 = Convert.ToInt32(dataRow["barangay_id"]);
		int num4 = Convert.ToInt32(dataRow["purok_id"]);
		if (num3 != num || num4 != num2)
		{
			return new HouseholdConsistencyViolation
			{
				Message = "Selected household does not belong to the resident's barangay/purok.",
				Title = "Household mismatch"
			};
		}
		int num5 = DbHelper.ExecuteScalar<int>("\nSELECT COUNT(*)\nFROM resident\nWHERE household_id = @householdId\n  AND status = 'ACTIVE'\n  AND (@excludeResidentId IS NULL OR resident_id <> @excludeResidentId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@householdId", (object)householdId);
			cmd.Parameters.AddWithValue("@excludeResidentId", excludeResidentId.HasValue ? ((object)excludeResidentId.Value) : DBNull.Value);
		});
		if (!string.Equals(a, "ACTIVE", StringComparison.OrdinalIgnoreCase) && num5 <= 0)
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
		string text = status.Trim();
		if (text.Equals("Active", StringComparison.OrdinalIgnoreCase))
		{
			return "ACTIVE";
		}
		if (text.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
		{
			return "MOVED_OUT";
		}
		if (text.Equals("Deceased", StringComparison.OrdinalIgnoreCase))
		{
			return "DECEASED";
		}
		return text.ToUpperInvariant();
	}
}
