using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1;

internal static class ReportsService
{
	public static ReportsDashboardData LoadDashboard(DateTime fromDate, DateTime toDate, ReportsFilters? filters = null)
	{
		if (fromDate > toDate)
		{
			DateTime dateTime = toDate;
			toDate = fromDate;
			fromDate = dateTime;
		}
		ReportsFilters reportsFilters = filters ?? new ReportsFilters();
		DateTime date = fromDate.Date;
		DateTime date2 = toDate.Date;
		DateTime toExclusive = date2.AddDays(1.0);
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			List<MonthlyTrendRow> trends = LoadMonthlyTrends(connection, date, date2, toExclusive, reportsFilters.PurokId, reportsFilters.CertificateStatus, reportsFilters.BlotterStatus);
			ReportsSummary summary = LoadSummary(connection, date, date2, toExclusive, reportsFilters.PurokId);
			ServiceTimeMetrics serviceTimes = LoadServiceTimes(connection, date, date2, toExclusive, reportsFilters.PurokId);
			List<StaffPerformanceRow> staffPerformance = LoadStaffPerformance(connection, date, toExclusive, reportsFilters.PurokId);
			List<HotspotPoint> hotspots = LoadHotspots(connection, date, toExclusive, reportsFilters.PurokId, reportsFilters.BlotterStatus);
			return new ReportsDashboardData
			{
				Trends = trends,
				Summary = summary,
				ServiceTimes = serviceTimes,
				StaffPerformance = staffPerformance,
				Hotspots = hotspots
			};
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static List<MonthlyTrendRow> LoadMonthlyTrends(MySqlConnection conn, DateTime from, DateTime to, DateTime toExclusive, int? purokId, CertificateStatusFilter certificateStatus, BlotterStatusFilter blotterStatus)
	{
		Dictionary<string, int> dictionary = LoadMonthlyCounts(conn, "SELECT DATE_FORMAT(date_registered, '%Y-%m') AS ym, COUNT(*) AS cnt\n              FROM resident\n              WHERE IFNULL(is_deleted,0)=0\n                AND date_registered BETWEEN @from AND @to\n                AND (@purokId IS NULL OR purok_id = @purokId)\n              GROUP BY ym\n              ORDER BY ym", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@from", (object)from);
			cmd.Parameters.AddWithValue("@to", (object)to);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
		});
		string text = BuildCertificateStatusClause(certificateStatus);
		Dictionary<string, int> dictionary2 = LoadMonthlyCounts(conn, "SELECT DATE_FORMAT(dr.requested_at, '%Y-%m') AS ym, COUNT(*) AS cnt\n              FROM document_request dr\n              INNER JOIN resident r ON r.resident_id = dr.resident_id\n              WHERE " + text + "\n                AND dr.requested_at >= @from\n                AND dr.requested_at < @toExcl\n                AND (@purokId IS NULL OR r.purok_id = @purokId)\n              GROUP BY ym\n              ORDER BY ym", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@from", (object)from);
			cmd.Parameters.AddWithValue("@toExcl", (object)toExclusive);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
		});
		string text2 = BuildBlotterStatusClause(blotterStatus);
		Dictionary<string, int> dictionary3 = LoadMonthlyCounts(conn, "SELECT DATE_FORMAT(cr.date_filed, '%Y-%m') AS ym, COUNT(*) AS cnt\n              FROM case_record cr\n              LEFT JOIN resident r ON r.resident_id = cr.complainant_id\n              WHERE cr.date_filed BETWEEN @from AND @to\n                " + text2 + "\n                AND (@purokId IS NULL OR r.purok_id = @purokId)\n              GROUP BY ym\n              ORDER BY ym", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@from", (object)from);
			cmd.Parameters.AddWithValue("@to", (object)to);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
		});
		DateTime dateTime = new DateTime(from.Year, from.Month, 1);
		DateTime dateTime2 = new DateTime(to.Year, to.Month, 1);
		List<MonthlyTrendRow> list = new List<MonthlyTrendRow>();
		while (dateTime <= dateTime2)
		{
			string text3 = dateTime.ToString("yyyy-MM");
			list.Add(new MonthlyTrendRow
			{
				MonthKey = text3,
				MonthLabel = dateTime.ToString("MMM yyyy"),
				Residents = (dictionary.TryGetValue(text3, out var value) ? value : 0),
				Certificates = (dictionary2.TryGetValue(text3, out var value2) ? value2 : 0),
				Blotters = (dictionary3.TryGetValue(text3, out var value3) ? value3 : 0)
			});
			dateTime = dateTime.AddMonths(1);
		}
		return list;
	}

	private static ReportsSummary LoadSummary(MySqlConnection conn, DateTime from, DateTime to, DateTime toExclusive, int? purokId)
	{
		return new ReportsSummary
		{
			NewResidents = ExecuteCount(conn, "SELECT COUNT(*)\n                  FROM resident\n                  WHERE IFNULL(is_deleted,0)=0\n                    AND date_registered BETWEEN @from AND @to\n                    AND (@purokId IS NULL OR purok_id = @purokId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@from", (object)from);
				cmd.Parameters.AddWithValue("@to", (object)to);
				cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			}),
			CertificateRequests = ExecuteCount(conn, "SELECT COUNT(*)\n                  FROM document_request dr\n                  INNER JOIN resident r ON r.resident_id = dr.resident_id\n                  WHERE UPPER(dr.status) <> 'DRAFT'\n                    AND dr.requested_at >= @from\n                    AND dr.requested_at < @toExcl\n                    AND (@purokId IS NULL OR r.purok_id = @purokId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@from", (object)from);
				cmd.Parameters.AddWithValue("@toExcl", (object)toExclusive);
				cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			}),
			CertificatesReleased = ExecuteCount(conn, "SELECT COUNT(*)\n                  FROM document_request dr\n                  INNER JOIN resident r ON r.resident_id = dr.resident_id\n                  WHERE dr.released_at IS NOT NULL\n                    AND dr.released_at >= @from\n                    AND dr.released_at < @toExcl\n                    AND (@purokId IS NULL OR r.purok_id = @purokId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@from", (object)from);
				cmd.Parameters.AddWithValue("@toExcl", (object)toExclusive);
				cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			}),
			BlottersFiled = ExecuteCount(conn, "SELECT COUNT(*)\n                  FROM case_record cr\n                  LEFT JOIN resident r ON r.resident_id = cr.complainant_id\n                  WHERE cr.date_filed BETWEEN @from AND @to\n                    AND (@purokId IS NULL OR r.purok_id = @purokId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@from", (object)from);
				cmd.Parameters.AddWithValue("@to", (object)to);
				cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			}),
			TotalResidents = ExecuteCount(conn, "SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND (@purokId IS NULL OR purok_id = @purokId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			}),
			PendingCertificates = ExecuteCount(conn, "SELECT COUNT(*)\n                  FROM document_request dr\n                  INNER JOIN resident r ON r.resident_id = dr.resident_id\n                  WHERE UPPER(dr.status) IN ('SUBMITTED','APPROVED','REQUESTED')\n                    AND (@purokId IS NULL OR r.purok_id = @purokId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			}),
			ActiveBlotters = ExecuteCount(conn, "SELECT COUNT(*)\n                  FROM case_record cr\n                  LEFT JOIN resident r ON r.resident_id = cr.complainant_id\n                  WHERE UPPER(cr.status) IN ('OPEN','ONGOING')\n                    AND (@purokId IS NULL OR r.purok_id = @purokId)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			})
		};
	}

	private static ServiceTimeMetrics LoadServiceTimes(MySqlConnection conn, DateTime from, DateTime to, DateTime toExclusive, int? purokId)
	{
		(int, double) tuple = ExecuteCountAndAverageSeconds(conn, "SELECT COUNT(*) AS n,\n                     AVG(TIMESTAMPDIFF(SECOND, requested_at, approved_at)) AS avg_seconds\n              FROM document_request dr\n              INNER JOIN resident r ON r.resident_id = dr.resident_id\n              WHERE dr.requested_at IS NOT NULL\n                AND dr.approved_at IS NOT NULL\n                AND dr.approved_at >= @from\n                AND dr.approved_at < @toExcl\n                AND dr.approved_at >= dr.requested_at\n                AND (@purokId IS NULL OR r.purok_id = @purokId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@from", (object)from);
			cmd.Parameters.AddWithValue("@toExcl", (object)toExclusive);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
		});
		(int, double) tuple2 = ExecuteCountAndAverageSeconds(conn, "SELECT COUNT(*) AS n,\n                     AVG(TIMESTAMPDIFF(SECOND, approved_at, released_at)) AS avg_seconds\n              FROM document_request dr\n              INNER JOIN resident r ON r.resident_id = dr.resident_id\n              WHERE dr.approved_at IS NOT NULL\n                AND dr.released_at IS NOT NULL\n                AND dr.released_at >= @from\n                AND dr.released_at < @toExcl\n                AND dr.released_at >= dr.approved_at\n                AND (@purokId IS NULL OR r.purok_id = @purokId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@from", (object)from);
			cmd.Parameters.AddWithValue("@toExcl", (object)toExclusive);
			cmd.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
		});
		ServiceTimeMetrics serviceTimeMetrics = new ServiceTimeMetrics();
		(serviceTimeMetrics.ApprovalSamples, serviceTimeMetrics.AvgRequestToApprovalSeconds) = tuple;
		(serviceTimeMetrics.ReleaseSamples, serviceTimeMetrics.AvgApprovalToReleaseSeconds) = tuple2;
		return serviceTimeMetrics;
	}

	private static List<StaffPerformanceRow> LoadStaffPerformance(MySqlConnection conn, DateTime from, DateTime toExclusive, int? purokId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Expected O, but got Unknown
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Expected O, but got Unknown
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Expected O, but got Unknown
		Dictionary<int, StaffPerformanceRow> dictionary = new Dictionary<int, StaffPerformanceRow>();
		MySqlCommand val = new MySqlCommand("SELECT user_id,\n                            username,\n                            COALESCE(NULLIF(full_name,''), NULLIF(CONCAT_WS(' ', first_name, last_name), ''), username) AS display_name,\n                            IFNULL(is_active,1) AS is_active\n                     FROM user_account\n                     ORDER BY IFNULL(is_active,1) DESC, username", conn);
		try
		{
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					int num = Convert.ToInt32(((DbDataReader)(object)val2)["user_id"]);
					string text = ((DbDataReader)(object)val2)["username"]?.ToString() ?? string.Empty;
					string displayName = ((DbDataReader)(object)val2)["display_name"]?.ToString() ?? text;
					bool isActive = false;
					if (((DbDataReader)(object)val2)["is_active"] != DBNull.Value)
					{
						isActive = Convert.ToInt32(((DbDataReader)(object)val2)["is_active"]) != 0;
					}
					dictionary[num] = new StaffPerformanceRow
					{
						UserId = num,
						Username = text,
						DisplayName = displayName,
						IsActive = isActive
					};
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
		MySqlCommand val3 = new MySqlCommand($"SELECT approved_by_user_id AS user_id,\n                             COUNT(*) AS completed,\n                             SUM(CASE\n                                   WHEN DATE(approved_at) > DATE_ADD(DATE(requested_at), INTERVAL {2} DAY) THEN 1\n                                   ELSE 0\n                                 END) AS overdue_completed,\n                             AVG(TIMESTAMPDIFF(SECOND, requested_at, approved_at)) AS avg_seconds\n                      FROM document_request dr\n                      INNER JOIN resident r ON r.resident_id = dr.resident_id\n                      WHERE dr.approved_by_user_id IS NOT NULL\n                        AND dr.requested_at IS NOT NULL\n                        AND dr.approved_at IS NOT NULL\n                        AND dr.approved_at >= @from\n                        AND dr.approved_at < @toExcl\n                        AND dr.approved_at >= dr.requested_at\n                        AND (@purokId IS NULL OR r.purok_id = @purokId)\n                      GROUP BY approved_by_user_id", conn);
		try
		{
			val3.Parameters.AddWithValue("@from", (object)from);
			val3.Parameters.AddWithValue("@toExcl", (object)toExclusive);
			val3.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			MySqlDataReader val4 = val3.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val4).Read())
				{
					int userId = Convert.ToInt32(((DbDataReader)(object)val4)["user_id"]);
					StaffPerformanceRow orCreateUser = GetOrCreateUser(dictionary, userId);
					orCreateUser.ApprovalsCompleted = ReadInt(val4, "completed");
					orCreateUser.ApprovalsOverdue = ReadInt(val4, "overdue_completed");
					orCreateUser.AvgRequestToApprovalSeconds = ReadDouble(val4, "avg_seconds");
				}
			}
			finally
			{
				((IDisposable)val4)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
		MySqlCommand val5 = new MySqlCommand($"SELECT released_by_user_id AS user_id,\n                             COUNT(*) AS completed,\n                             SUM(CASE\n                                   WHEN DATE(released_at) > DATE_ADD(DATE(approved_at), INTERVAL {1} DAY) THEN 1\n                                   ELSE 0\n                                 END) AS overdue_completed,\n                             AVG(TIMESTAMPDIFF(SECOND, approved_at, released_at)) AS avg_seconds\n                      FROM document_request dr\n                      INNER JOIN resident r ON r.resident_id = dr.resident_id\n                      WHERE dr.released_by_user_id IS NOT NULL\n                        AND dr.approved_at IS NOT NULL\n                        AND dr.released_at IS NOT NULL\n                        AND dr.released_at >= @from\n                        AND dr.released_at < @toExcl\n                        AND dr.released_at >= dr.approved_at\n                        AND (@purokId IS NULL OR r.purok_id = @purokId)\n                      GROUP BY released_by_user_id", conn);
		try
		{
			val5.Parameters.AddWithValue("@from", (object)from);
			val5.Parameters.AddWithValue("@toExcl", (object)toExclusive);
			val5.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			MySqlDataReader val6 = val5.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val6).Read())
				{
					int userId2 = Convert.ToInt32(((DbDataReader)(object)val6)["user_id"]);
					StaffPerformanceRow orCreateUser2 = GetOrCreateUser(dictionary, userId2);
					orCreateUser2.ReleasesCompleted = ReadInt(val6, "completed");
					orCreateUser2.ReleasesOverdue = ReadInt(val6, "overdue_completed");
					orCreateUser2.AvgApprovalToReleaseSeconds = ReadDouble(val6, "avg_seconds");
				}
			}
			finally
			{
				((IDisposable)val6)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val5)?.Dispose();
		}
		MySqlCommand val7 = new MySqlCommand($"SELECT ct.created_by_user_id AS user_id,\n                             COUNT(*) AS status_changes,\n                             SUM(CASE WHEN ct.to_status IN ('SETTLED','REFERRED','CLOSED') THEN 1 ELSE 0 END) AS resolutions,\n                             SUM(CASE\n                                   WHEN ct.to_status IN ('SETTLED','REFERRED','CLOSED')\n                                        AND DATE(ct.created_at) > DATE_ADD(DATE(cr.created_at), INTERVAL {15} DAY)\n                                   THEN 1\n                                   ELSE 0\n                                 END) AS resolutions_overdue,\n                             AVG(CASE WHEN ct.to_status IN ('SETTLED','REFERRED','CLOSED')\n                                      THEN TIMESTAMPDIFF(SECOND, cr.created_at, ct.created_at)\n                                      ELSE NULL\n                                 END) AS avg_resolution_seconds\n                      FROM case_timeline ct\n                      INNER JOIN case_record cr ON cr.case_id = ct.case_id\n                      LEFT JOIN resident r ON r.resident_id = cr.complainant_id\n                      WHERE ct.created_by_user_id IS NOT NULL\n                        AND ct.event_type = 'STATUS_CHANGE'\n                        AND ct.created_at >= @from\n                        AND ct.created_at < @toExcl\n                        AND (@purokId IS NULL OR r.purok_id = @purokId)\n                      GROUP BY ct.created_by_user_id", conn);
		try
		{
			val7.Parameters.AddWithValue("@from", (object)from);
			val7.Parameters.AddWithValue("@toExcl", (object)toExclusive);
			val7.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			MySqlDataReader val8 = val7.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val8).Read())
				{
					int userId3 = Convert.ToInt32(((DbDataReader)(object)val8)["user_id"]);
					StaffPerformanceRow orCreateUser3 = GetOrCreateUser(dictionary, userId3);
					orCreateUser3.BlotterStatusChanges = ReadInt(val8, "status_changes");
					orCreateUser3.BlotterResolutions = ReadInt(val8, "resolutions");
					orCreateUser3.BlotterResolutionsOverdue = ReadInt(val8, "resolutions_overdue");
					orCreateUser3.AvgBlotterResolutionSeconds = ReadDouble(val8, "avg_resolution_seconds");
				}
			}
			finally
			{
				((IDisposable)val8)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val7)?.Dispose();
		}
		return dictionary.Values.ToList();
	}

	private static List<HotspotPoint> LoadHotspots(MySqlConnection conn, DateTime from, DateTime toExclusive, int? purokId, BlotterStatusFilter blotterStatus)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		string text = BuildBlotterStatusClause(blotterStatus);
		MySqlCommand val = new MySqlCommand("SELECT p.purok_id,\n                      p.name AS purok_name,\n                      p.latitude,\n                      p.longitude,\n                      COUNT(cr.case_id) AS incident_count\n               FROM purok_sitio p\n               LEFT JOIN resident r\n                      ON r.purok_id = p.purok_id\n                     AND IFNULL(r.is_deleted,0) = 0\n               LEFT JOIN case_record cr\n                      ON cr.complainant_id = r.resident_id\n                     AND cr.date_filed >= @from\n                     AND cr.date_filed < @toExcl\n                     " + text + "\n               WHERE p.barangay_id = @barangayId\n                 AND (@purokId IS NULL OR p.purok_id = @purokId)\n               GROUP BY p.purok_id, p.name, p.latitude, p.longitude\n               ORDER BY incident_count DESC, p.name ASC", conn);
		try
		{
			val.Parameters.AddWithValue("@from", (object)from);
			val.Parameters.AddWithValue("@toExcl", (object)toExclusive);
			val.Parameters.AddWithValue("@barangayId", (object)1);
			val.Parameters.AddWithValue("@purokId", ((object)purokId) ?? DBNull.Value);
			List<HotspotPoint> list = new List<HotspotPoint>();
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					list.Add(new HotspotPoint
					{
						PurokId = Convert.ToInt32(((DbDataReader)(object)val2)["purok_id"]),
						PurokName = (Convert.ToString(((DbDataReader)(object)val2)["purok_name"]) ?? string.Empty),
						Latitude = ((((DbDataReader)(object)val2)["latitude"] == DBNull.Value) ? ((double?)null) : new double?(Convert.ToDouble(((DbDataReader)(object)val2)["latitude"]))),
						Longitude = ((((DbDataReader)(object)val2)["longitude"] == DBNull.Value) ? ((double?)null) : new double?(Convert.ToDouble(((DbDataReader)(object)val2)["longitude"]))),
						IncidentCount = ((((DbDataReader)(object)val2)["incident_count"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)val2)["incident_count"]) : 0)
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

	private static StaffPerformanceRow GetOrCreateUser(Dictionary<int, StaffPerformanceRow> users, int userId)
	{
		if (users.TryGetValue(userId, out StaffPerformanceRow value))
		{
			return value;
		}
		return users[userId] = new StaffPerformanceRow
		{
			UserId = userId,
			Username = $"#{userId}",
			DisplayName = $"User #{userId}",
			IsActive = false
		};
	}

	private static int ReadInt(MySqlDataReader reader, string column)
	{
		object obj = ((DbDataReader)(object)reader)[column];
		if (obj != DBNull.Value)
		{
			return Convert.ToInt32(obj);
		}
		return 0;
	}

	private static double ReadDouble(MySqlDataReader reader, string column)
	{
		object obj = ((DbDataReader)(object)reader)[column];
		if (obj != DBNull.Value)
		{
			return Convert.ToDouble(obj);
		}
		return 0.0;
	}

	private static string BuildCertificateStatusClause(CertificateStatusFilter filter)
	{
		return filter switch
		{
			CertificateStatusFilter.Pending => "UPPER(dr.status) IN ('SUBMITTED','APPROVED','REQUESTED')", 
			CertificateStatusFilter.Submitted => "UPPER(dr.status) IN ('SUBMITTED','REQUESTED')", 
			CertificateStatusFilter.Approved => "UPPER(dr.status) = 'APPROVED'", 
			CertificateStatusFilter.Released => "UPPER(dr.status) IN ('RELEASED','ISSUED')", 
			CertificateStatusFilter.Cancelled => "UPPER(dr.status) = 'CANCELLED'", 
			CertificateStatusFilter.Rejected => "UPPER(dr.status) = 'REJECTED'", 
			_ => "UPPER(dr.status) <> 'DRAFT'", 
		};
	}

	private static string BuildBlotterStatusClause(BlotterStatusFilter filter)
	{
		return filter switch
		{
			BlotterStatusFilter.Active => "AND UPPER(cr.status) IN ('OPEN','ONGOING')", 
			BlotterStatusFilter.Settled => "AND UPPER(cr.status) = 'SETTLED'", 
			BlotterStatusFilter.Referred => "AND UPPER(cr.status) = 'REFERRED'", 
			BlotterStatusFilter.Closed => "AND UPPER(cr.status) = 'CLOSED'", 
			_ => string.Empty, 
		};
	}

	private static Dictionary<string, int> LoadMonthlyCounts(MySqlConnection conn, string sql, Action<MySqlCommand> configure)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		MySqlCommand val = new MySqlCommand(sql, conn);
		try
		{
			configure(val);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					string text = ((DbDataReader)(object)val2)["ym"]?.ToString() ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(text))
					{
						int value = ((((DbDataReader)(object)val2)["cnt"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)val2)["cnt"]) : 0);
						dictionary[text] = value;
					}
				}
				return dictionary;
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

	private static (int Count, double AverageSeconds) ExecuteCountAndAverageSeconds(MySqlConnection conn, string sql, Action<MySqlCommand> configure)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand(sql, conn);
		try
		{
			configure(val);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					return (Count: 0, AverageSeconds: 0.0);
				}
				int num = ((((DbDataReader)(object)val2)["n"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)val2)["n"]) : 0);
				double num2 = ((((DbDataReader)(object)val2)["avg_seconds"] == DBNull.Value) ? 0.0 : Convert.ToDouble(((DbDataReader)(object)val2)["avg_seconds"]));
				if (num <= 0 || num2 < 0.0)
				{
					return (Count: 0, AverageSeconds: 0.0);
				}
				return (Count: num, AverageSeconds: num2);
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

	private static int ExecuteCount(MySqlConnection conn, string sql, Action<MySqlCommand>? configure = null)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand(sql, conn);
		try
		{
			configure?.Invoke(val);
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			if (obj == null || obj == DBNull.Value)
			{
				return 0;
			}
			return Convert.ToInt32(obj);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
