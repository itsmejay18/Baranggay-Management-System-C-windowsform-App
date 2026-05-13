using System;
using System.Data;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1.helper;

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
		int barangayId = resident.BarangayId ?? 1;
		int purokId = resident.PurokId ?? 1;
		if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
		{
			return null;
		}
		DataTable dataTable = DbHelper.LoadTable("\nSELECT\n    r.resident_id,\n    r.first_name,\n    r.middle_name,\n    r.last_name,\n    r.birth_date,\n    COALESCE(\n        NULLIF(TRIM(CONCAT_WS(' ', h.house_no, h.street, h.subdivision)), ''),\n        CONCAT('Purok #', r.purok_id)\n    ) AS address_label\nFROM resident r\nLEFT JOIN household h ON h.household_id = r.household_id\nWHERE\n    UPPER(TRIM(r.first_name)) = UPPER(TRIM(@firstName))\n    AND UPPER(TRIM(COALESCE(r.middle_name, ''))) = UPPER(TRIM(@middleName))\n    AND UPPER(TRIM(r.last_name)) = UPPER(TRIM(@lastName))\n    AND r.birth_date = @birthDate\n    AND (@excludeResidentId IS NULL OR r.resident_id <> @excludeResidentId)\n    AND\n    (\n        (@householdId IS NOT NULL AND r.household_id = @householdId)\n        OR\n        (r.barangay_id = @barangayId AND r.purok_id = @purokId)\n    )\nLIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@firstName", (object)firstName);
			cmd.Parameters.AddWithValue("@middleName", (object)middleName);
			cmd.Parameters.AddWithValue("@lastName", (object)lastName);
			cmd.Parameters.AddWithValue("@birthDate", (object)resident.DateOfBirth.Date);
			cmd.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? ((object)resident.HouseholdId.Value) : DBNull.Value);
			cmd.Parameters.AddWithValue("@barangayId", (object)barangayId);
			cmd.Parameters.AddWithValue("@purokId", (object)purokId);
			cmd.Parameters.AddWithValue("@excludeResidentId", excludeResidentId.HasValue ? ((object)excludeResidentId.Value) : DBNull.Value);
		});
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		DataRow dataRow = dataTable.Rows[0];
		return new ResidentDuplicateMatch
		{
			ResidentId = Convert.ToInt32(dataRow["resident_id"]),
			FullName = string.Join(" ", Convert.ToString(dataRow["first_name"]) ?? string.Empty, Convert.ToString(dataRow["middle_name"]) ?? string.Empty, Convert.ToString(dataRow["last_name"]) ?? string.Empty).Replace("  ", " ").Trim(),
			BirthDate = ((dataRow["birth_date"] == DBNull.Value) ? DateTime.MinValue : Convert.ToDateTime(dataRow["birth_date"])),
			AddressLabel = (Convert.ToString(dataRow["address_label"]) ?? string.Empty)
		};
	}
}
