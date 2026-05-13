using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class RepeatRespondentService
{
	public static string NormalizeName(string? respondentName)
	{
		return (respondentName ?? string.Empty).Trim().ToUpperInvariant();
	}

	public static RepeatRespondentCounts GetCountsForRespondent(int? respondentResidentId, string? respondentName)
	{
		string text = NormalizeName(respondentName);
		if (respondentResidentId.HasValue && respondentResidentId.Value > 0)
		{
			RepeatRespondentBatch repeatRespondentBatch = LoadCounts(new int[1] { respondentResidentId.Value }, Array.Empty<string>(), string.IsNullOrWhiteSpace(text) ? ((IEnumerable<string>)Array.Empty<string>()) : ((IEnumerable<string>)new string[1] { text }));
			RepeatRespondentCounts result = RepeatRespondentCounts.Zero;
			if (repeatRespondentBatch.ByResidentId.TryGetValue(respondentResidentId.Value, out var value))
			{
				result = result.Add(value);
			}
			if (!string.IsNullOrWhiteSpace(text) && repeatRespondentBatch.ByNameNullIdOnly.TryGetValue(text, out var value2))
			{
				result = result.Add(value2);
			}
			return result;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (!LoadCounts(Array.Empty<int>(), new string[1] { text }, Array.Empty<string>()).ByNameAll.TryGetValue(text, out var value3))
			{
				return RepeatRespondentCounts.Zero;
			}
			return value3;
		}
		return RepeatRespondentCounts.Zero;
	}

	public static RepeatRespondentBatch LoadCounts(IEnumerable<int> residentIds, IEnumerable<string> normalizedNamesAll, IEnumerable<string> normalizedNamesNullIdOnly)
	{
		RepeatRespondentBatch repeatRespondentBatch = new RepeatRespondentBatch();
		List<int> list = residentIds.Where((int id) => id > 0).Distinct().ToList();
		List<string> list2 = NormalizeDistinctNames(normalizedNamesAll);
		List<string> list3 = NormalizeDistinctNames(normalizedNamesNullIdOnly);
		if (list.Count == 0 && list2.Count == 0 && list3.Count == 0)
		{
			return repeatRespondentBatch;
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				if (list.Count > 0)
				{
					LoadCountsByResidentId(connection, list, repeatRespondentBatch.ByResidentId);
				}
				if (list2.Count > 0)
				{
					LoadCountsByName(connection, list2, includeLinked: true, repeatRespondentBatch.ByNameAll);
				}
				if (list3.Count > 0)
				{
					LoadCountsByName(connection, list3, includeLinked: false, repeatRespondentBatch.ByNameNullIdOnly);
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to load repeat-respondent counts.", ex);
		}
		return repeatRespondentBatch;
	}

	private static List<string> NormalizeDistinctNames(IEnumerable<string> names)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (string item in names ?? Array.Empty<string>())
		{
			string text = NormalizeName(item);
			if (!string.IsNullOrWhiteSpace(text))
			{
				hashSet.Add(text);
			}
		}
		return hashSet.ToList();
	}

	private static void LoadCountsByResidentId(MySqlConnection conn, IReadOnlyList<int> ids, Dictionary<int, RepeatRespondentCounts> target)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand();
		try
		{
			val.Connection = conn;
			string text = BuildInClause(val, ids, "@rid");
			((DbCommand)(object)val).CommandText = "SELECT respondent_resident_id AS rid,\n                      COUNT(*) AS total_cases,\n                      SUM(CASE WHEN UPPER(status) IN ('OPEN','ONGOING') THEN 1 ELSE 0 END) AS active_cases\n               FROM case_record\n               WHERE respondent_resident_id IN (" + text + ")\n               GROUP BY respondent_resident_id";
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					if (((DbDataReader)(object)val2)["rid"] != DBNull.Value)
					{
						int key = Convert.ToInt32(((DbDataReader)(object)val2)["rid"]);
						target[key] = ReadCounts(val2);
					}
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

	private static void LoadCountsByName(MySqlConnection conn, IReadOnlyList<string> normalizedNames, bool includeLinked, Dictionary<string, RepeatRespondentCounts> target)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		string value = (includeLinked ? string.Empty : " AND respondent_resident_id IS NULL");
		MySqlCommand val = new MySqlCommand();
		try
		{
			val.Connection = conn;
			string value2 = BuildInClause(val, normalizedNames, "@nm");
			((DbCommand)(object)val).CommandText = $"SELECT UPPER(TRIM(respondent_name)) AS norm_name,\n                      COUNT(*) AS total_cases,\n                      SUM(CASE WHEN UPPER(status) IN ('OPEN','ONGOING') THEN 1 ELSE 0 END) AS active_cases\n               FROM case_record\n               WHERE respondent_name IS NOT NULL\n                 AND respondent_name <> ''\n                 AND UPPER(TRIM(respondent_name)) IN ({value2})\n                 {value}\n               GROUP BY UPPER(TRIM(respondent_name))";
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					string text = ((DbDataReader)(object)val2)["norm_name"]?.ToString() ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(text))
					{
						text = NormalizeName(text);
						if (!string.IsNullOrWhiteSpace(text))
						{
							target[text] = ReadCounts(val2);
						}
					}
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

	private static string BuildInClause<T>(MySqlCommand cmd, IReadOnlyList<T> values, string prefix)
	{
		string[] array = new string[values.Count];
		for (int i = 0; i < values.Count; i++)
		{
			string text = (array[i] = prefix + i);
			cmd.Parameters.AddWithValue(text, (object)values[i]);
		}
		return string.Join(", ", array);
	}

	private static RepeatRespondentCounts ReadCounts(MySqlDataReader reader)
	{
		int totalCases = ((((DbDataReader)(object)reader)["total_cases"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)reader)["total_cases"]) : 0);
		int activeCases = ((((DbDataReader)(object)reader)["active_cases"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)reader)["active_cases"]) : 0);
		return new RepeatRespondentCounts(totalCases, activeCases);
	}
}
