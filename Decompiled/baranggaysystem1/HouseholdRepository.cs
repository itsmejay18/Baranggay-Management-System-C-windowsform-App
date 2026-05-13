using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class HouseholdRepository
{
	private const int MaxPageSize = 200;

	private const string MemberStatsJoin = "\nLEFT JOIN (\n    SELECT r.household_id,\n           COUNT(*) AS total_members,\n           SUM(CASE WHEN IFNULL(r.is_senior,0) = 1 THEN 1 ELSE 0 END) AS seniors,\n           SUM(CASE WHEN IFNULL(r.is_pwd,0) = 1 THEN 1 ELSE 0 END) AS pwd_members,\n           SUM(CASE WHEN IFNULL(r.is_4ps_beneficiary,0) = 1 THEN 1 ELSE 0 END) AS four_ps_members,\n           SUM(CASE WHEN IFNULL(r.is_registered_voter,0) = 1 THEN 1 ELSE 0 END) AS voters\n    FROM resident r\n    WHERE IFNULL(r.is_deleted,0) = 0\n      AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n    GROUP BY r.household_id\n) ms ON ms.household_id = h.household_id";

	private const string ActiveCaseJoin = "\nLEFT JOIN (\n    SELECT rr.household_id,\n           COUNT(DISTINCT cr.case_id) AS active_cases\n    FROM resident rr\n    INNER JOIN case_record cr ON cr.complainant_id = rr.resident_id\n    WHERE IFNULL(rr.is_deleted,0) = 0\n      AND UPPER(cr.status) IN ('OPEN', 'ONGOING')\n    GROUP BY rr.household_id\n) cs ON cs.household_id = h.household_id";

	private const string ListWhereClause = "\nWHERE h.barangay_id = @barangayId\n  AND (@purokId IS NULL OR h.purok_id = @purokId)\n  AND (@searchText = '' OR\n       COALESCE(h.house_no, '') LIKE @searchLike OR\n       COALESCE(h.street, '') LIKE @searchLike OR\n       COALESCE(h.subdivision, '') LIKE @searchLike OR\n       EXISTS (\n           SELECT 1\n           FROM resident rs\n           WHERE rs.household_id = h.household_id\n             AND IFNULL(rs.is_deleted,0) = 0\n             AND (rs.status IS NULL OR UPPER(rs.status) = 'ACTIVE')\n             AND CONCAT_WS(' ', rs.first_name, rs.middle_name, rs.last_name) LIKE @searchLike\n       ))\n  AND (@withSeniors = 0 OR COALESCE(ms.seniors, 0) > 0)\n  AND (@withPwd = 0 OR COALESCE(ms.pwd_members, 0) > 0)\n  AND (@with4Ps = 0 OR COALESCE(ms.four_ps_members, 0) > 0)\n  AND (@emptyOnly = 0 OR COALESCE(ms.total_members, 0) = 0)\n  AND (@hasActiveCases = 0 OR COALESCE(cs.active_cases, 0) > 0)";

	public HouseholdPageResult Search(HouseholdListFilters filters)
	{
		HouseholdListFilters safeFilters = NormalizeFilters(filters);
		try
		{
			return SearchOnline(safeFilters);
		}
		catch (Exception exception) when (TryActivateOfflineFallback(exception, "Search"))
		{
			return SearchViaDbHelper(safeFilters);
		}
	}

	private static HouseholdPageResult SearchOnline(HouseholdListFilters safeFilters)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		List<HouseholdListItem> list = new List<HouseholdListItem>();
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n                     FROM household h\n                     LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n                     \nLEFT JOIN (\n    SELECT r.household_id,\n           COUNT(*) AS total_members,\n           SUM(CASE WHEN IFNULL(r.is_senior,0) = 1 THEN 1 ELSE 0 END) AS seniors,\n           SUM(CASE WHEN IFNULL(r.is_pwd,0) = 1 THEN 1 ELSE 0 END) AS pwd_members,\n           SUM(CASE WHEN IFNULL(r.is_4ps_beneficiary,0) = 1 THEN 1 ELSE 0 END) AS four_ps_members,\n           SUM(CASE WHEN IFNULL(r.is_registered_voter,0) = 1 THEN 1 ELSE 0 END) AS voters\n    FROM resident r\n    WHERE IFNULL(r.is_deleted,0) = 0\n      AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n    GROUP BY r.household_id\n) ms ON ms.household_id = h.household_id\nLEFT JOIN (\n    SELECT rr.household_id,\n           COUNT(DISTINCT cr.case_id) AS active_cases\n    FROM resident rr\n    INNER JOIN case_record cr ON cr.complainant_id = rr.resident_id\n    WHERE IFNULL(rr.is_deleted,0) = 0\n      AND UPPER(cr.status) IN ('OPEN', 'ONGOING')\n    GROUP BY rr.household_id\n) cs ON cs.household_id = h.household_id\nWHERE h.barangay_id = @barangayId\n  AND (@purokId IS NULL OR h.purok_id = @purokId)\n  AND (@searchText = '' OR\n       COALESCE(h.house_no, '') LIKE @searchLike OR\n       COALESCE(h.street, '') LIKE @searchLike OR\n       COALESCE(h.subdivision, '') LIKE @searchLike OR\n       EXISTS (\n           SELECT 1\n           FROM resident rs\n           WHERE rs.household_id = h.household_id\n             AND IFNULL(rs.is_deleted,0) = 0\n             AND (rs.status IS NULL OR UPPER(rs.status) = 'ACTIVE')\n             AND CONCAT_WS(' ', rs.first_name, rs.middle_name, rs.last_name) LIKE @searchLike\n       ))\n  AND (@withSeniors = 0 OR COALESCE(ms.seniors, 0) > 0)\n  AND (@withPwd = 0 OR COALESCE(ms.pwd_members, 0) > 0)\n  AND (@with4Ps = 0 OR COALESCE(ms.four_ps_members, 0) > 0)\n  AND (@emptyOnly = 0 OR COALESCE(ms.total_members, 0) = 0)\n  AND (@hasActiveCases = 0 OR COALESCE(cs.active_cases, 0) > 0)", connection);
			int totalRows;
			try
			{
				AddListParameters(val, safeFilters);
				totalRows = Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0));
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			MySqlCommand val2 = new MySqlCommand("SELECT h.household_id,\n                            h.house_no,\n                            h.street,\n                            h.subdivision,\n                            h.purok_id,\n                            COALESCE(p.name, '') AS purok_name,\n                            COALESCE(ms.total_members, 0) AS members,\n                            COALESCE(ms.seniors, 0) AS seniors,\n                            COALESCE(ms.pwd_members, 0) AS pwd_members,\n                            COALESCE(ms.four_ps_members, 0) AS four_ps_members,\n                            COALESCE(ms.voters, 0) AS voters,\n                            COALESCE(cs.active_cases, 0) AS active_cases,\n                            h.updated_at\n                     FROM household h\n                     LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n                     \nLEFT JOIN (\n    SELECT r.household_id,\n           COUNT(*) AS total_members,\n           SUM(CASE WHEN IFNULL(r.is_senior,0) = 1 THEN 1 ELSE 0 END) AS seniors,\n           SUM(CASE WHEN IFNULL(r.is_pwd,0) = 1 THEN 1 ELSE 0 END) AS pwd_members,\n           SUM(CASE WHEN IFNULL(r.is_4ps_beneficiary,0) = 1 THEN 1 ELSE 0 END) AS four_ps_members,\n           SUM(CASE WHEN IFNULL(r.is_registered_voter,0) = 1 THEN 1 ELSE 0 END) AS voters\n    FROM resident r\n    WHERE IFNULL(r.is_deleted,0) = 0\n      AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n    GROUP BY r.household_id\n) ms ON ms.household_id = h.household_id\nLEFT JOIN (\n    SELECT rr.household_id,\n           COUNT(DISTINCT cr.case_id) AS active_cases\n    FROM resident rr\n    INNER JOIN case_record cr ON cr.complainant_id = rr.resident_id\n    WHERE IFNULL(rr.is_deleted,0) = 0\n      AND UPPER(cr.status) IN ('OPEN', 'ONGOING')\n    GROUP BY rr.household_id\n) cs ON cs.household_id = h.household_id\nWHERE h.barangay_id = @barangayId\n  AND (@purokId IS NULL OR h.purok_id = @purokId)\n  AND (@searchText = '' OR\n       COALESCE(h.house_no, '') LIKE @searchLike OR\n       COALESCE(h.street, '') LIKE @searchLike OR\n       COALESCE(h.subdivision, '') LIKE @searchLike OR\n       EXISTS (\n           SELECT 1\n           FROM resident rs\n           WHERE rs.household_id = h.household_id\n             AND IFNULL(rs.is_deleted,0) = 0\n             AND (rs.status IS NULL OR UPPER(rs.status) = 'ACTIVE')\n             AND CONCAT_WS(' ', rs.first_name, rs.middle_name, rs.last_name) LIKE @searchLike\n       ))\n  AND (@withSeniors = 0 OR COALESCE(ms.seniors, 0) > 0)\n  AND (@withPwd = 0 OR COALESCE(ms.pwd_members, 0) > 0)\n  AND (@with4Ps = 0 OR COALESCE(ms.four_ps_members, 0) > 0)\n  AND (@emptyOnly = 0 OR COALESCE(ms.total_members, 0) = 0)\n  AND (@hasActiveCases = 0 OR COALESCE(cs.active_cases, 0) > 0)\n                     ORDER BY h.updated_at DESC, h.household_id DESC\n                     LIMIT @take OFFSET @skip", connection);
			try
			{
				AddListParameters(val2, safeFilters);
				val2.Parameters.AddWithValue("@take", (object)safeFilters.PageSize);
				val2.Parameters.AddWithValue("@skip", (object)((safeFilters.PageNumber - 1) * safeFilters.PageSize));
				MySqlDataReader val3 = val2.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val3).Read())
					{
						list.Add(new HouseholdListItem
						{
							HouseholdId = Convert.ToInt32(((DbDataReader)(object)val3)["household_id"]),
							HouseNo = (Convert.ToString(((DbDataReader)(object)val3)["house_no"]) ?? string.Empty),
							Street = (Convert.ToString(((DbDataReader)(object)val3)["street"]) ?? string.Empty),
							Subdivision = (Convert.ToString(((DbDataReader)(object)val3)["subdivision"]) ?? string.Empty),
							PurokId = Convert.ToInt32(((DbDataReader)(object)val3)["purok_id"]),
							PurokName = (Convert.ToString(((DbDataReader)(object)val3)["purok_name"]) ?? string.Empty),
							MemberCount = Convert.ToInt32(((DbDataReader)(object)val3)["members"]),
							SeniorCount = Convert.ToInt32(((DbDataReader)(object)val3)["seniors"]),
							PwdCount = Convert.ToInt32(((DbDataReader)(object)val3)["pwd_members"]),
							FourPsCount = Convert.ToInt32(((DbDataReader)(object)val3)["four_ps_members"]),
							VoterCount = Convert.ToInt32(((DbDataReader)(object)val3)["voters"]),
							ActiveCaseCount = Convert.ToInt32(((DbDataReader)(object)val3)["active_cases"]),
							UpdatedAt = ((((DbDataReader)(object)val3)["updated_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(((DbDataReader)(object)val3)["updated_at"])))
						});
					}
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
			return new HouseholdPageResult
			{
				Items = list,
				TotalRows = totalRows,
				PageNumber = safeFilters.PageNumber,
				PageSize = safeFilters.PageSize
			};
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static HouseholdPageResult SearchViaDbHelper(HouseholdListFilters safeFilters)
	{
		int totalRows = DbHelper.ExecuteScalar<int>("SELECT COUNT(*)\n              FROM household h\n              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n              \nLEFT JOIN (\n    SELECT r.household_id,\n           COUNT(*) AS total_members,\n           SUM(CASE WHEN IFNULL(r.is_senior,0) = 1 THEN 1 ELSE 0 END) AS seniors,\n           SUM(CASE WHEN IFNULL(r.is_pwd,0) = 1 THEN 1 ELSE 0 END) AS pwd_members,\n           SUM(CASE WHEN IFNULL(r.is_4ps_beneficiary,0) = 1 THEN 1 ELSE 0 END) AS four_ps_members,\n           SUM(CASE WHEN IFNULL(r.is_registered_voter,0) = 1 THEN 1 ELSE 0 END) AS voters\n    FROM resident r\n    WHERE IFNULL(r.is_deleted,0) = 0\n      AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n    GROUP BY r.household_id\n) ms ON ms.household_id = h.household_id\nLEFT JOIN (\n    SELECT rr.household_id,\n           COUNT(DISTINCT cr.case_id) AS active_cases\n    FROM resident rr\n    INNER JOIN case_record cr ON cr.complainant_id = rr.resident_id\n    WHERE IFNULL(rr.is_deleted,0) = 0\n      AND UPPER(cr.status) IN ('OPEN', 'ONGOING')\n    GROUP BY rr.household_id\n) cs ON cs.household_id = h.household_id\nWHERE h.barangay_id = @barangayId\n  AND (@purokId IS NULL OR h.purok_id = @purokId)\n  AND (@searchText = '' OR\n       COALESCE(h.house_no, '') LIKE @searchLike OR\n       COALESCE(h.street, '') LIKE @searchLike OR\n       COALESCE(h.subdivision, '') LIKE @searchLike OR\n       EXISTS (\n           SELECT 1\n           FROM resident rs\n           WHERE rs.household_id = h.household_id\n             AND IFNULL(rs.is_deleted,0) = 0\n             AND (rs.status IS NULL OR UPPER(rs.status) = 'ACTIVE')\n             AND CONCAT_WS(' ', rs.first_name, rs.middle_name, rs.last_name) LIKE @searchLike\n       ))\n  AND (@withSeniors = 0 OR COALESCE(ms.seniors, 0) > 0)\n  AND (@withPwd = 0 OR COALESCE(ms.pwd_members, 0) > 0)\n  AND (@with4Ps = 0 OR COALESCE(ms.four_ps_members, 0) > 0)\n  AND (@emptyOnly = 0 OR COALESCE(ms.total_members, 0) = 0)\n  AND (@hasActiveCases = 0 OR COALESCE(cs.active_cases, 0) > 0)", delegate(MySqlCommand cmd)
		{
			AddListParameters(cmd, safeFilters);
		});
		DataTable dataTable = DbHelper.LoadTable("SELECT h.household_id,\n                     h.house_no,\n                     h.street,\n                     h.subdivision,\n                     h.purok_id,\n                     COALESCE(p.name, '') AS purok_name,\n                     COALESCE(ms.total_members, 0) AS members,\n                     COALESCE(ms.seniors, 0) AS seniors,\n                     COALESCE(ms.pwd_members, 0) AS pwd_members,\n                     COALESCE(ms.four_ps_members, 0) AS four_ps_members,\n                     COALESCE(ms.voters, 0) AS voters,\n                     COALESCE(cs.active_cases, 0) AS active_cases,\n                     h.updated_at\n              FROM household h\n              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n              \nLEFT JOIN (\n    SELECT r.household_id,\n           COUNT(*) AS total_members,\n           SUM(CASE WHEN IFNULL(r.is_senior,0) = 1 THEN 1 ELSE 0 END) AS seniors,\n           SUM(CASE WHEN IFNULL(r.is_pwd,0) = 1 THEN 1 ELSE 0 END) AS pwd_members,\n           SUM(CASE WHEN IFNULL(r.is_4ps_beneficiary,0) = 1 THEN 1 ELSE 0 END) AS four_ps_members,\n           SUM(CASE WHEN IFNULL(r.is_registered_voter,0) = 1 THEN 1 ELSE 0 END) AS voters\n    FROM resident r\n    WHERE IFNULL(r.is_deleted,0) = 0\n      AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n    GROUP BY r.household_id\n) ms ON ms.household_id = h.household_id\nLEFT JOIN (\n    SELECT rr.household_id,\n           COUNT(DISTINCT cr.case_id) AS active_cases\n    FROM resident rr\n    INNER JOIN case_record cr ON cr.complainant_id = rr.resident_id\n    WHERE IFNULL(rr.is_deleted,0) = 0\n      AND UPPER(cr.status) IN ('OPEN', 'ONGOING')\n    GROUP BY rr.household_id\n) cs ON cs.household_id = h.household_id\nWHERE h.barangay_id = @barangayId\n  AND (@purokId IS NULL OR h.purok_id = @purokId)\n  AND (@searchText = '' OR\n       COALESCE(h.house_no, '') LIKE @searchLike OR\n       COALESCE(h.street, '') LIKE @searchLike OR\n       COALESCE(h.subdivision, '') LIKE @searchLike OR\n       EXISTS (\n           SELECT 1\n           FROM resident rs\n           WHERE rs.household_id = h.household_id\n             AND IFNULL(rs.is_deleted,0) = 0\n             AND (rs.status IS NULL OR UPPER(rs.status) = 'ACTIVE')\n             AND CONCAT_WS(' ', rs.first_name, rs.middle_name, rs.last_name) LIKE @searchLike\n       ))\n  AND (@withSeniors = 0 OR COALESCE(ms.seniors, 0) > 0)\n  AND (@withPwd = 0 OR COALESCE(ms.pwd_members, 0) > 0)\n  AND (@with4Ps = 0 OR COALESCE(ms.four_ps_members, 0) > 0)\n  AND (@emptyOnly = 0 OR COALESCE(ms.total_members, 0) = 0)\n  AND (@hasActiveCases = 0 OR COALESCE(cs.active_cases, 0) > 0)\n              ORDER BY h.updated_at DESC, h.household_id DESC\n              LIMIT @take OFFSET @skip", delegate(MySqlCommand cmd)
		{
			AddListParameters(cmd, safeFilters);
			cmd.Parameters.AddWithValue("@take", (object)safeFilters.PageSize);
			cmd.Parameters.AddWithValue("@skip", (object)((safeFilters.PageNumber - 1) * safeFilters.PageSize));
		});
		List<HouseholdListItem> list = new List<HouseholdListItem>(dataTable.Rows.Count);
		foreach (DataRow row in dataTable.Rows)
		{
			list.Add(new HouseholdListItem
			{
				HouseholdId = ReadInt(row, "household_id"),
				HouseNo = ReadString(row, "house_no"),
				Street = ReadString(row, "street"),
				Subdivision = ReadString(row, "subdivision"),
				PurokId = ReadInt(row, "purok_id"),
				PurokName = ReadString(row, "purok_name"),
				MemberCount = ReadInt(row, "members"),
				SeniorCount = ReadInt(row, "seniors"),
				PwdCount = ReadInt(row, "pwd_members"),
				FourPsCount = ReadInt(row, "four_ps_members"),
				VoterCount = ReadInt(row, "voters"),
				ActiveCaseCount = ReadInt(row, "active_cases"),
				UpdatedAt = ReadNullableDateTime(row, "updated_at")
			});
		}
		return new HouseholdPageResult
		{
			Items = list,
			TotalRows = totalRows,
			PageNumber = safeFilters.PageNumber,
			PageSize = safeFilters.PageSize
		};
	}

	public HouseholdDetailsDto? GetDetails(int householdId, int barangayId)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		if (householdId <= 0)
		{
			return null;
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT h.household_id,\n                     h.barangay_id,\n                     h.purok_id,\n                     COALESCE(p.name, '') AS purok_name,\n                     h.house_no,\n                     h.street,\n                     h.subdivision,\n                     h.address_note,\n                     h.latitude,\n                     h.longitude,\n                     COALESCE(ms.total_members, 0) AS members,\n                     COALESCE(ms.seniors, 0) AS seniors,\n                     COALESCE(ms.pwd_members, 0) AS pwd_members,\n                     COALESCE(ms.four_ps_members, 0) AS four_ps_members,\n                     COALESCE(ms.voters, 0) AS voters,\n                     COALESCE(cs.active_cases, 0) AS active_cases,\n                     h.updated_at\n              FROM household h\n              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n              \nLEFT JOIN (\n    SELECT r.household_id,\n           COUNT(*) AS total_members,\n           SUM(CASE WHEN IFNULL(r.is_senior,0) = 1 THEN 1 ELSE 0 END) AS seniors,\n           SUM(CASE WHEN IFNULL(r.is_pwd,0) = 1 THEN 1 ELSE 0 END) AS pwd_members,\n           SUM(CASE WHEN IFNULL(r.is_4ps_beneficiary,0) = 1 THEN 1 ELSE 0 END) AS four_ps_members,\n           SUM(CASE WHEN IFNULL(r.is_registered_voter,0) = 1 THEN 1 ELSE 0 END) AS voters\n    FROM resident r\n    WHERE IFNULL(r.is_deleted,0) = 0\n      AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n    GROUP BY r.household_id\n) ms ON ms.household_id = h.household_id\nLEFT JOIN (\n    SELECT rr.household_id,\n           COUNT(DISTINCT cr.case_id) AS active_cases\n    FROM resident rr\n    INNER JOIN case_record cr ON cr.complainant_id = rr.resident_id\n    WHERE IFNULL(rr.is_deleted,0) = 0\n      AND UPPER(cr.status) IN ('OPEN', 'ONGOING')\n    GROUP BY rr.household_id\n) cs ON cs.household_id = h.household_id\n              WHERE h.household_id = @householdId\n                AND h.barangay_id = @barangayId\n              LIMIT 1", connection);
			try
			{
				val.Parameters.AddWithValue("@householdId", (object)householdId);
				val.Parameters.AddWithValue("@barangayId", (object)barangayId);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					if (!((DbDataReader)(object)val2).Read())
					{
						return null;
					}
					return new HouseholdDetailsDto
					{
						HouseholdId = Convert.ToInt32(((DbDataReader)(object)val2)["household_id"]),
						BarangayId = Convert.ToInt32(((DbDataReader)(object)val2)["barangay_id"]),
						PurokId = Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"]),
						PurokName = (Convert.ToString(((DbDataReader)(object)val2)["purok_name"]) ?? string.Empty),
						HouseNo = (Convert.ToString(((DbDataReader)(object)val2)["house_no"]) ?? string.Empty),
						Street = (Convert.ToString(((DbDataReader)(object)val2)["street"]) ?? string.Empty),
						Subdivision = (Convert.ToString(((DbDataReader)(object)val2)["subdivision"]) ?? string.Empty),
						AddressNote = (Convert.ToString(((DbDataReader)(object)val2)["address_note"]) ?? string.Empty),
						Latitude = ((((DbDataReader)(object)val2)["latitude"] == DBNull.Value) ? ((decimal?)null) : new decimal?(Convert.ToDecimal(((DbDataReader)(object)val2)["latitude"]))),
						Longitude = ((((DbDataReader)(object)val2)["longitude"] == DBNull.Value) ? ((decimal?)null) : new decimal?(Convert.ToDecimal(((DbDataReader)(object)val2)["longitude"]))),
						MemberCount = Convert.ToInt32(((DbDataReader)(object)val2)["members"]),
						SeniorCount = Convert.ToInt32(((DbDataReader)(object)val2)["seniors"]),
						PwdCount = Convert.ToInt32(((DbDataReader)(object)val2)["pwd_members"]),
						FourPsCount = Convert.ToInt32(((DbDataReader)(object)val2)["four_ps_members"]),
						VoterCount = Convert.ToInt32(((DbDataReader)(object)val2)["voters"]),
						ActiveCaseCount = Convert.ToInt32(((DbDataReader)(object)val2)["active_cases"]),
						UpdatedAt = ((((DbDataReader)(object)val2)["updated_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(((DbDataReader)(object)val2)["updated_at"])))
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
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public HouseholdEditRecord? GetForEdit(int householdId, int barangayId)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		if (householdId <= 0)
		{
			return null;
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT household_id, barangay_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude\n              FROM household\n              WHERE household_id = @householdId\n                AND barangay_id = @barangayId\n              LIMIT 1", connection);
			try
			{
				val.Parameters.AddWithValue("@householdId", (object)householdId);
				val.Parameters.AddWithValue("@barangayId", (object)barangayId);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					if (!((DbDataReader)(object)val2).Read())
					{
						return null;
					}
					return new HouseholdEditRecord
					{
						HouseholdId = Convert.ToInt32(((DbDataReader)(object)val2)["household_id"]),
						BarangayId = Convert.ToInt32(((DbDataReader)(object)val2)["barangay_id"]),
						PurokId = Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"]),
						HouseNo = (Convert.ToString(((DbDataReader)(object)val2)["house_no"]) ?? string.Empty),
						Street = (Convert.ToString(((DbDataReader)(object)val2)["street"]) ?? string.Empty),
						Subdivision = (Convert.ToString(((DbDataReader)(object)val2)["subdivision"]) ?? string.Empty),
						AddressNote = (Convert.ToString(((DbDataReader)(object)val2)["address_note"]) ?? string.Empty),
						Latitude = ((((DbDataReader)(object)val2)["latitude"] == DBNull.Value) ? ((decimal?)null) : new decimal?(Convert.ToDecimal(((DbDataReader)(object)val2)["latitude"]))),
						Longitude = ((((DbDataReader)(object)val2)["longitude"] == DBNull.Value) ? ((decimal?)null) : new decimal?(Convert.ToDecimal(((DbDataReader)(object)val2)["longitude"])))
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
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public IReadOnlyList<LookupItem> GetPurokOptions(int barangayId)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		int targetBarangayId = ResolveBarangayId(barangayId);
		List<LookupItem> list = new List<LookupItem>();
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				MySqlCommand val = new MySqlCommand("SELECT purok_id, name\n                  FROM purok_sitio\n                  WHERE barangay_id = @barangayId\n                  ORDER BY name", connection);
				try
				{
					val.Parameters.AddWithValue("@barangayId", (object)targetBarangayId);
					MySqlDataReader val2 = val.ExecuteReader();
					try
					{
						while (((DbDataReader)(object)val2).Read())
						{
							list.Add(new LookupItem(Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"]), Convert.ToString(((DbDataReader)(object)val2)["name"]) ?? string.Empty));
						}
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
		catch (Exception exception) when (TryActivateOfflineFallback(exception, "GetPurokOptions"))
		{
			foreach (DataRow row in DbHelper.LoadTable("SELECT purok_id, name\n                  FROM purok_sitio\n                  WHERE barangay_id = @barangayId\n                  ORDER BY name", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)targetBarangayId);
			}).Rows)
			{
				list.Add(new LookupItem(ReadInt(row, "purok_id"), ReadString(row, "name")));
			}
		}
		return list;
	}

	public IReadOnlyList<LookupItem> GetHouseholdsForPurok(int barangayId, int? purokId, int? excludeHouseholdId = null)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		int num = ResolveBarangayId(barangayId);
		List<LookupItem> list = new List<LookupItem>();
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT h.household_id,\n                     h.house_no,\n                     h.street,\n                     h.subdivision,\n                     COALESCE(p.name, '') AS purok_name\n              FROM household h\n              LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n              WHERE h.barangay_id = @barangayId\n                AND (@purokId IS NULL OR h.purok_id = @purokId)\n                AND (@excludeHouseholdId IS NULL OR h.household_id <> @excludeHouseholdId)\n              ORDER BY COALESCE(h.street, ''), COALESCE(h.house_no, ''), h.household_id", connection);
			try
			{
				val.Parameters.AddWithValue("@barangayId", (object)num);
				val.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
				val.Parameters.AddWithValue("@excludeHouseholdId", ((object)excludeHouseholdId) ?? DBNull.Value);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						int num2 = Convert.ToInt32(((DbDataReader)(object)val2)["household_id"]);
						string? houseNo = Convert.ToString(((DbDataReader)(object)val2)["house_no"]) ?? string.Empty;
						string street = Convert.ToString(((DbDataReader)(object)val2)["street"]) ?? string.Empty;
						string subdivision = Convert.ToString(((DbDataReader)(object)val2)["subdivision"]) ?? string.Empty;
						string purokName = Convert.ToString(((DbDataReader)(object)val2)["purok_name"]) ?? string.Empty;
						string text = BuildAddressLabel(houseNo, street, subdivision, purokName);
						if (string.IsNullOrWhiteSpace(text))
						{
							text = $"Household #{num2}";
						}
						list.Add(new LookupItem(num2, text));
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
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public int Create(HouseholdSaveRequest request)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		int num = ResolveBarangayId(request.BarangayId);
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				MySqlCommand val2 = new MySqlCommand("INSERT INTO household\n                (barangay_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude, created_at, updated_at)\n              VALUES\n                (@barangayId, @purokId, @houseNo, @street, @subdivision, @addressNote, @latitude, @longitude, NOW(), NOW())", connection, val);
				try
				{
					val2.Parameters.AddWithValue("@barangayId", (object)num);
					val2.Parameters.AddWithValue("@purokId", (object)request.PurokId);
					val2.Parameters.AddWithValue("@houseNo", ToDbValue(request.HouseNo));
					val2.Parameters.AddWithValue("@street", ToDbValue(request.Street));
					val2.Parameters.AddWithValue("@subdivision", ToDbValue(request.Subdivision));
					val2.Parameters.AddWithValue("@addressNote", ToDbValue(request.AddressNote));
					val2.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? ((object)request.Latitude.Value) : DBNull.Value);
					val2.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? ((object)request.Longitude.Value) : DBNull.Value);
					((DbCommand)(object)val2).ExecuteNonQuery();
					int num2 = (int)val2.LastInsertedId;
					object afterState = ReadHouseholdAuditSnapshot(connection, val, num2);
					AuditTrailService.LogTransactional(connection, val, "Households", "household", num2, "CREATE", null, afterState, "Household created.");
					((DbTransaction)(object)val).Commit();
					return num2;
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

	public void Update(int householdId, HouseholdSaveRequest request)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		int num = ResolveBarangayId(request.BarangayId);
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				object beforeState = ReadHouseholdAuditSnapshot(connection, val, householdId);
				MySqlCommand val2 = new MySqlCommand("UPDATE household\n              SET purok_id = @purokId,\n                  house_no = @houseNo,\n                  street = @street,\n                  subdivision = @subdivision,\n                  address_note = @addressNote,\n                  latitude = @latitude,\n                  longitude = @longitude,\n                  updated_at = NOW()\n              WHERE household_id = @householdId\n                AND barangay_id = @barangayId", connection, val);
				try
				{
					val2.Parameters.AddWithValue("@purokId", (object)request.PurokId);
					val2.Parameters.AddWithValue("@houseNo", ToDbValue(request.HouseNo));
					val2.Parameters.AddWithValue("@street", ToDbValue(request.Street));
					val2.Parameters.AddWithValue("@subdivision", ToDbValue(request.Subdivision));
					val2.Parameters.AddWithValue("@addressNote", ToDbValue(request.AddressNote));
					val2.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? ((object)request.Latitude.Value) : DBNull.Value);
					val2.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? ((object)request.Longitude.Value) : DBNull.Value);
					val2.Parameters.AddWithValue("@householdId", (object)householdId);
					val2.Parameters.AddWithValue("@barangayId", (object)num);
					if (((DbCommand)(object)val2).ExecuteNonQuery() <= 0)
					{
						throw new InvalidOperationException("Household not found or no longer belongs to this barangay.");
					}
					object afterState = ReadHouseholdAuditSnapshot(connection, val, householdId);
					AuditTrailService.LogTransactional(connection, val, "Households", "household", householdId, "UPDATE", beforeState, afterState, "Household updated.");
					((DbTransaction)(object)val).Commit();
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

	public bool TryDelete(int householdId, int barangayId, out string message)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		message = string.Empty;
		int num = ResolveBarangayId(barangayId);
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n                     FROM resident\n                     WHERE household_id = @householdId\n                       AND IFNULL(is_deleted,0) = 0", connection);
			try
			{
				val.Parameters.AddWithValue("@householdId", (object)householdId);
				if (Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) > 0)
				{
					message = "Cannot delete a household with assigned members.";
					return false;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			MySqlTransaction val2 = connection.BeginTransaction();
			try
			{
				object beforeState = ReadHouseholdAuditSnapshot(connection, val2, householdId);
				MySqlCommand val3 = new MySqlCommand("DELETE FROM household\n              WHERE household_id = @householdId\n                AND barangay_id = @barangayId", connection, val2);
				try
				{
					val3.Parameters.AddWithValue("@householdId", (object)householdId);
					val3.Parameters.AddWithValue("@barangayId", (object)num);
					if (((DbCommand)(object)val3).ExecuteNonQuery() <= 0)
					{
						((DbTransaction)(object)val2).Rollback();
						message = "Household not found.";
						return false;
					}
					AuditTrailService.LogTransactional(connection, val2, "Households", "household", householdId, "DELETE", beforeState, null, "Household deleted.");
					((DbTransaction)(object)val2).Commit();
					return true;
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public bool ExistsDuplicateAddress(int barangayId, int purokId, string? houseNo, string? street, int? excludeHouseholdId = null)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		int num = ResolveBarangayId(barangayId);
		string text = (houseNo ?? string.Empty).Trim();
		string text2 = (street ?? string.Empty).Trim();
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT COUNT(*)\n              FROM household\n              WHERE barangay_id = @barangayId\n                AND purok_id = @purokId\n                AND UPPER(TRIM(COALESCE(house_no, ''))) = UPPER(@houseNo)\n                AND UPPER(TRIM(COALESCE(street, ''))) = UPPER(@street)\n                AND (@excludeId IS NULL OR household_id <> @excludeId)", connection);
			try
			{
				val.Parameters.AddWithValue("@barangayId", (object)num);
				val.Parameters.AddWithValue("@purokId", (object)purokId);
				val.Parameters.AddWithValue("@houseNo", (object)text);
				val.Parameters.AddWithValue("@street", (object)text2);
				val.Parameters.AddWithValue("@excludeId", ((object)excludeHouseholdId) ?? DBNull.Value);
				return Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar() ?? ((object)0)) > 0;
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

	public IReadOnlyList<HouseholdMemberRecord> GetMembers(int householdId, int barangayId)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		int num = ResolveBarangayId(barangayId);
		List<HouseholdMemberRecord> list = new List<HouseholdMemberRecord>();
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT r.resident_id,\n                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,\n                     CASE\n                       WHEN r.birth_date IS NULL THEN NULL\n                       ELSE TIMESTAMPDIFF(YEAR, r.birth_date, CURDATE())\n                     END AS age,\n                     COALESCE(r.sex, '') AS sex,\n                     COALESCE(r.civil_status, '') AS civil_status,\n                     COALESCE(r.contact_no, '') AS contact_no,\n                     COALESCE(r.status, 'ACTIVE') AS status,\n                     r.photo\n              FROM resident r\n              WHERE r.household_id = @householdId\n                AND r.barangay_id = @barangayId\n                AND IFNULL(r.is_deleted, 0) = 0\n              ORDER BY r.last_name, r.first_name, r.middle_name", connection);
			try
			{
				val.Parameters.AddWithValue("@householdId", (object)householdId);
				val.Parameters.AddWithValue("@barangayId", (object)num);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						list.Add(new HouseholdMemberRecord
						{
							ResidentId = Convert.ToInt32(((DbDataReader)(object)val2)["resident_id"]),
							FullName = (Convert.ToString(((DbDataReader)(object)val2)["full_name"]) ?? string.Empty),
							Age = ((((DbDataReader)(object)val2)["age"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)val2)["age"]))),
							Sex = (Convert.ToString(((DbDataReader)(object)val2)["sex"]) ?? string.Empty),
							CivilStatus = (Convert.ToString(((DbDataReader)(object)val2)["civil_status"]) ?? string.Empty),
							ContactNo = (Convert.ToString(((DbDataReader)(object)val2)["contact_no"]) ?? string.Empty),
							Status = (Convert.ToString(((DbDataReader)(object)val2)["status"]) ?? string.Empty),
							HasPhoto = (((DbDataReader)(object)val2)["photo"] != DBNull.Value)
						});
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
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public IReadOnlyList<HouseholdTransferHistoryItem> GetTransferHistory(int householdId, int barangayId)
	{
		int targetBarangayId = ResolveBarangayId(barangayId);
		DataTable dataTable = DbHelper.LoadTable("SELECT th.transfer_id,\n                     th.resident_id,\n                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS resident_name,\n                     COALESCE(th.old_address, '') AS old_address,\n                     COALESCE(th.new_address, '') AS new_address,\n                     COALESCE(th.transfer_reason, '') AS transfer_reason,\n                     COALESCE(NULLIF(ua.full_name, ''), ua.username, CONCAT('User #', th.transferred_by_user_id)) AS transferred_by,\n                     th.transferred_at\n              FROM resident_transfer_history th\n              INNER JOIN resident r ON r.resident_id = th.resident_id\n              LEFT JOIN user_account ua ON ua.user_id = th.transferred_by_user_id\n              WHERE r.barangay_id = @barangayId\n                AND (th.old_household_id = @householdId OR th.new_household_id = @householdId)\n              ORDER BY th.transferred_at DESC, th.transfer_id DESC", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@householdId", (object)householdId);
			cmd.Parameters.AddWithValue("@barangayId", (object)targetBarangayId);
		});
		List<HouseholdTransferHistoryItem> list = new List<HouseholdTransferHistoryItem>(dataTable.Rows.Count);
		foreach (DataRow row in dataTable.Rows)
		{
			list.Add(new HouseholdTransferHistoryItem
			{
				TransferId = Convert.ToInt64(row["transfer_id"]),
				ResidentId = Convert.ToInt32(row["resident_id"]),
				ResidentName = (Convert.ToString(row["resident_name"]) ?? string.Empty),
				OldAddress = (Convert.ToString(row["old_address"]) ?? string.Empty),
				NewAddress = (Convert.ToString(row["new_address"]) ?? string.Empty),
				Reason = (Convert.ToString(row["transfer_reason"]) ?? string.Empty),
				TransferredBy = (Convert.ToString(row["transferred_by"]) ?? string.Empty),
				TransferredAt = ((row["transferred_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(row["transferred_at"])))
			});
		}
		return list;
	}

	public IReadOnlyList<ResidentPickerItem> GetResidentsForHouseholdPicker(int barangayId, int targetHouseholdId, string? searchText)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		int num = ResolveBarangayId(barangayId);
		string text = (searchText ?? string.Empty).Trim();
		string text2 = "%" + text + "%";
		List<ResidentPickerItem> list = new List<ResidentPickerItem>();
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT r.resident_id,\n                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) AS full_name,\n                     COALESCE(r.contact_no, '') AS contact_no,\n                     r.household_id,\n                     r.purok_id,\n                     COALESCE(h.house_no, '') AS house_no,\n                     COALESCE(h.street, '') AS street,\n                     COALESCE(h.subdivision, '') AS subdivision,\n                     COALESCE(p.name, '') AS purok_name\n              FROM resident r\n              LEFT JOIN household h ON h.household_id = r.household_id\n              LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id\n              WHERE r.barangay_id = @barangayId\n                AND IFNULL(r.is_deleted, 0) = 0\n                AND (r.status IS NULL OR UPPER(r.status) = 'ACTIVE')\n                AND (r.household_id IS NULL OR r.household_id <> @targetHouseholdId)\n                AND (@searchText = '' OR\n                     CONCAT_WS(' ', r.first_name, r.middle_name, r.last_name) LIKE @searchLike OR\n                     COALESCE(r.contact_no, '') LIKE @searchLike)\n              ORDER BY r.last_name, r.first_name, r.middle_name\n              LIMIT 200", connection);
			try
			{
				val.Parameters.AddWithValue("@barangayId", (object)num);
				val.Parameters.AddWithValue("@targetHouseholdId", (object)targetHouseholdId);
				val.Parameters.AddWithValue("@searchText", (object)text);
				val.Parameters.AddWithValue("@searchLike", (object)text2);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						string houseNo = Convert.ToString(((DbDataReader)(object)val2)["house_no"]) ?? string.Empty;
						string street = Convert.ToString(((DbDataReader)(object)val2)["street"]) ?? string.Empty;
						string subdivision = Convert.ToString(((DbDataReader)(object)val2)["subdivision"]) ?? string.Empty;
						string purokName = Convert.ToString(((DbDataReader)(object)val2)["purok_name"]) ?? string.Empty;
						list.Add(new ResidentPickerItem
						{
							ResidentId = Convert.ToInt32(((DbDataReader)(object)val2)["resident_id"]),
							FullName = (Convert.ToString(((DbDataReader)(object)val2)["full_name"]) ?? string.Empty),
							ContactNo = (Convert.ToString(((DbDataReader)(object)val2)["contact_no"]) ?? string.Empty),
							CurrentHouseholdId = ((((DbDataReader)(object)val2)["household_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)val2)["household_id"]))),
							CurrentPurokId = ((((DbDataReader)(object)val2)["purok_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"]))),
							CurrentAddress = BuildAddressLabel(houseNo, street, subdivision, purokName)
						});
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
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public ResidentLocationSnapshot GetResidentLocationSnapshot(MySqlConnection conn, MySqlTransaction tx, int residentId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT r.purok_id,\n                     r.household_id,\n                     COALESCE(p.name, '') AS purok_name,\n                     COALESCE(h.house_no, '') AS house_no,\n                     COALESCE(h.street, '') AS street,\n                     COALESCE(h.subdivision, '') AS subdivision\n              FROM resident r\n              LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id\n              LEFT JOIN household h ON h.household_id = r.household_id\n              WHERE r.resident_id = @residentId\n              LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@residentId", (object)residentId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					return new ResidentLocationSnapshot();
				}
				int? purokId = ((((DbDataReader)(object)val2)["purok_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"])));
				int? householdId = ((((DbDataReader)(object)val2)["household_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(((DbDataReader)(object)val2)["household_id"])));
				string purokName = Convert.ToString(((DbDataReader)(object)val2)["purok_name"]) ?? string.Empty;
				string houseNo = Convert.ToString(((DbDataReader)(object)val2)["house_no"]) ?? string.Empty;
				string street = Convert.ToString(((DbDataReader)(object)val2)["street"]) ?? string.Empty;
				string subdivision = Convert.ToString(((DbDataReader)(object)val2)["subdivision"]) ?? string.Empty;
				return new ResidentLocationSnapshot
				{
					PurokId = purokId,
					HouseholdId = householdId,
					AddressLabel = BuildAddressLabel(houseNo, street, subdivision, purokName)
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
		return 1;
	}

	public static string BuildAddressLabel(string? houseNo, string? street, string? subdivision, string? purokName)
	{
		string text = (houseNo ?? string.Empty).Trim();
		string text2 = (street ?? string.Empty).Trim();
		string text3 = (subdivision ?? string.Empty).Trim();
		string text4 = (purokName ?? string.Empty).Trim();
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text);
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add(text2);
		}
		if (!string.IsNullOrWhiteSpace(text3))
		{
			list.Add(text3);
		}
		string text5 = string.Join(", ", list);
		if (!string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(text4))
		{
			return text5 + ", " + text4;
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			return text5;
		}
		return text4;
	}

	private static HouseholdListFilters NormalizeFilters(HouseholdListFilters filters)
	{
		HouseholdListFilters householdListFilters = filters ?? new HouseholdListFilters();
		int pageNumber = ((householdListFilters.PageNumber <= 0) ? 1 : householdListFilters.PageNumber);
		int val = ((householdListFilters.PageSize <= 0) ? 25 : householdListFilters.PageSize);
		val = Math.Min(val, 200);
		return new HouseholdListFilters
		{
			BarangayId = ResolveBarangayId(householdListFilters.BarangayId),
			SearchText = (householdListFilters.SearchText ?? string.Empty).Trim(),
			PurokId = householdListFilters.PurokId,
			WithSeniors = householdListFilters.WithSeniors,
			WithPwd = householdListFilters.WithPwd,
			With4Ps = householdListFilters.With4Ps,
			EmptyHouseholdOnly = householdListFilters.EmptyHouseholdOnly,
			HasActiveCasesOnly = householdListFilters.HasActiveCasesOnly,
			PageNumber = pageNumber,
			PageSize = val
		};
	}

	private static void AddListParameters(MySqlCommand cmd, HouseholdListFilters filters)
	{
		string text = filters.SearchText ?? string.Empty;
		cmd.Parameters.AddWithValue("@barangayId", (object)filters.BarangayId);
		cmd.Parameters.AddWithValue("@purokId", ((object)filters.PurokId) ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@searchText", (object)text);
		cmd.Parameters.AddWithValue("@searchLike", (object)("%" + text + "%"));
		cmd.Parameters.AddWithValue("@withSeniors", (object)(filters.WithSeniors ? 1 : 0));
		cmd.Parameters.AddWithValue("@withPwd", (object)(filters.WithPwd ? 1 : 0));
		cmd.Parameters.AddWithValue("@with4Ps", (object)(filters.With4Ps ? 1 : 0));
		cmd.Parameters.AddWithValue("@emptyOnly", (object)(filters.EmptyHouseholdOnly ? 1 : 0));
		cmd.Parameters.AddWithValue("@hasActiveCases", (object)(filters.HasActiveCasesOnly ? 1 : 0));
	}

	private static bool TryActivateOfflineFallback(Exception exception, string operationName)
	{
		if (!IsConnectivityFailure(exception))
		{
			return false;
		}
		if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
		{
			return false;
		}
		if (!OfflineDatabaseSupport.IsOffline)
		{
			OfflineDatabaseSupport.ActivateOfflineMode();
			AppLogger.LogWarning("[HouseholdRepository] Switched to offline mode during " + operationName + " after connectivity failure.", exception);
		}
		return true;
	}

	private static bool IsConnectivityFailure(Exception exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			if (ex is OperationCanceledException)
			{
				return false;
			}
			if (ex is TimeoutException)
			{
				return true;
			}
			MySqlException ex2 = (MySqlException)(object)((ex is MySqlException) ? ex : null);
			if (ex2 != null)
			{
				bool flag;
				switch (ex2.Number)
				{
				case -1:
				case 0:
				case 1042:
				case 2002:
				case 2003:
				case 2005:
				case 2013:
				case 2055:
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (flag)
				{
					return true;
				}
				if (ContainsConnectivityText(((Exception)(object)ex2).Message ?? string.Empty))
				{
					return true;
				}
			}
			if (ContainsConnectivityText(ex.Message ?? string.Empty))
			{
				return true;
			}
		}
		return ContainsConnectivityText(exception.Message ?? string.Empty);
	}

	private static bool ContainsConnectivityText(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return false;
		}
		if (message.IndexOf("Unable to connect", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("stream has failed", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("reading from the stream", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("fatal error encountered", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("server is not responding", StringComparison.OrdinalIgnoreCase) < 0 && message.IndexOf("network", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return message.IndexOf("connection from the pool", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static int ReadInt(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0;
		}
		return Convert.ToInt32(row[columnName]);
	}

	private static string ReadString(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(row[columnName]) ?? string.Empty;
	}

	private static DateTime? ReadNullableDateTime(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return null;
		}
		return Convert.ToDateTime(row[columnName]);
	}

	private static object ToDbValue(string? value)
	{
		string text = (value ?? string.Empty).Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return DBNull.Value;
	}

	private static object? ReadHouseholdAuditSnapshot(MySqlConnection conn, MySqlTransaction? tx, int householdId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT household_id, barangay_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude, updated_at\n              FROM household\n              WHERE household_id = @householdId\n              LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@householdId", (object)householdId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					return null;
				}
				return new
				{
					HouseholdId = Convert.ToInt32(((DbDataReader)(object)val2)["household_id"]),
					BarangayId = Convert.ToInt32(((DbDataReader)(object)val2)["barangay_id"]),
					PurokId = Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"]),
					HouseNo = (Convert.ToString(((DbDataReader)(object)val2)["house_no"]) ?? string.Empty),
					Street = (Convert.ToString(((DbDataReader)(object)val2)["street"]) ?? string.Empty),
					Subdivision = (Convert.ToString(((DbDataReader)(object)val2)["subdivision"]) ?? string.Empty),
					AddressNote = (Convert.ToString(((DbDataReader)(object)val2)["address_note"]) ?? string.Empty),
					Latitude = ((((DbDataReader)(object)val2)["latitude"] == DBNull.Value) ? ((decimal?)null) : new decimal?(Convert.ToDecimal(((DbDataReader)(object)val2)["latitude"]))),
					Longitude = ((((DbDataReader)(object)val2)["longitude"] == DBNull.Value) ? ((decimal?)null) : new decimal?(Convert.ToDecimal(((DbDataReader)(object)val2)["longitude"]))),
					UpdatedAt = ((((DbDataReader)(object)val2)["updated_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(((DbDataReader)(object)val2)["updated_at"])))
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
}
