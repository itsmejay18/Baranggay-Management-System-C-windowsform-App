using System;
using System.Collections.Generic;
using System.Linq;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal readonly struct RepeatRespondentCounts
{
    public int TotalCases { get; }
    public int ActiveCases { get; }

    public RepeatRespondentCounts(int totalCases, int activeCases)
    {
        TotalCases = totalCases < 0 ? 0 : totalCases;
        ActiveCases = activeCases < 0 ? 0 : activeCases;
    }

    public static RepeatRespondentCounts Zero => new(0, 0);

    public RepeatRespondentCounts Add(RepeatRespondentCounts other)
        => new(TotalCases + other.TotalCases, ActiveCases + other.ActiveCases);
}

internal sealed class RepeatRespondentBatch
{
    public Dictionary<int, RepeatRespondentCounts> ByResidentId { get; } = new();

    public Dictionary<string, RepeatRespondentCounts> ByNameAll { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<string, RepeatRespondentCounts> ByNameNullIdOnly { get; } =
        new(StringComparer.Ordinal);
}

internal static class RepeatRespondentService
{
    public static string NormalizeName(string? respondentName)
        => (respondentName ?? string.Empty).Trim().ToUpperInvariant();

    public static RepeatRespondentCounts GetCountsForRespondent(int? respondentResidentId, string? respondentName)
    {
        string normalized = NormalizeName(respondentName);

        if (respondentResidentId.HasValue && respondentResidentId.Value > 0)
        {
            var batch = LoadCounts(
                new[] { respondentResidentId.Value },
                Array.Empty<string>(),
                string.IsNullOrWhiteSpace(normalized) ? Array.Empty<string>() : new[] { normalized });

            RepeatRespondentCounts counts = RepeatRespondentCounts.Zero;
            if (batch.ByResidentId.TryGetValue(respondentResidentId.Value, out RepeatRespondentCounts byId))
            {
                counts = counts.Add(byId);
            }

            if (!string.IsNullOrWhiteSpace(normalized) &&
                batch.ByNameNullIdOnly.TryGetValue(normalized, out RepeatRespondentCounts byLegacyName))
            {
                counts = counts.Add(byLegacyName);
            }

            return counts;
        }

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            var batch = LoadCounts(Array.Empty<int>(), new[] { normalized }, Array.Empty<string>());
            return batch.ByNameAll.TryGetValue(normalized, out RepeatRespondentCounts byName)
                ? byName
                : RepeatRespondentCounts.Zero;
        }

        return RepeatRespondentCounts.Zero;
    }

    public static RepeatRespondentBatch LoadCounts(
        IEnumerable<int> residentIds,
        IEnumerable<string> normalizedNamesAll,
        IEnumerable<string> normalizedNamesNullIdOnly)
    {
        var batch = new RepeatRespondentBatch();

        List<int> ids = residentIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        List<string> namesAll = NormalizeDistinctNames(normalizedNamesAll);
        List<string> namesNullId = NormalizeDistinctNames(normalizedNamesNullIdOnly);

        if (ids.Count == 0 && namesAll.Count == 0 && namesNullId.Count == 0)
        {
            return batch;
        }

        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();

            if (ids.Count > 0)
            {
                LoadCountsByResidentId(conn, ids, batch.ByResidentId);
            }

            if (namesAll.Count > 0)
            {
                LoadCountsByName(conn, namesAll, includeLinked: true, batch.ByNameAll);
            }

            if (namesNullId.Count > 0)
            {
                LoadCountsByName(conn, namesNullId, includeLinked: false, batch.ByNameNullIdOnly);
            }
        }
        catch (Exception ex)
        {
            // Best-effort: repeat flags should never block the workflow.
            AppLogger.LogWarning("Unable to load repeat-respondent counts.", ex);
        }

        return batch;
    }

    private static List<string> NormalizeDistinctNames(IEnumerable<string> names)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in names ?? Array.Empty<string>())
        {
            string normalized = NormalizeName(name);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                set.Add(normalized);
            }
        }

        return set.ToList();
    }

    private static void LoadCountsByResidentId(MySqlConnection conn, IReadOnlyList<int> ids, Dictionary<int, RepeatRespondentCounts> target)
    {
        using var cmd = new MySqlCommand();
        cmd.Connection = conn;
        string inClause = BuildInClause(cmd, ids, "@rid");
        cmd.CommandText =
            $@"SELECT respondent_resident_id AS rid,
                      COUNT(*) AS total_cases,
                      SUM(CASE WHEN UPPER(status) IN ('OPEN','ONGOING') THEN 1 ELSE 0 END) AS active_cases
               FROM case_record
               WHERE respondent_resident_id IN ({inClause})
               GROUP BY respondent_resident_id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader["rid"] == DBNull.Value)
            {
                continue;
            }

            int rid = Convert.ToInt32(reader["rid"]);
            target[rid] = ReadCounts(reader);
        }
    }

    private static void LoadCountsByName(
        MySqlConnection conn,
        IReadOnlyList<string> normalizedNames,
        bool includeLinked,
        Dictionary<string, RepeatRespondentCounts> target)
    {
        string linkedClause = includeLinked ? string.Empty : " AND respondent_resident_id IS NULL";

        using var cmd = new MySqlCommand();
        cmd.Connection = conn;
        string inClause = BuildInClause(cmd, normalizedNames, "@nm");
        cmd.CommandText =
            $@"SELECT UPPER(TRIM(respondent_name)) AS norm_name,
                      COUNT(*) AS total_cases,
                      SUM(CASE WHEN UPPER(status) IN ('OPEN','ONGOING') THEN 1 ELSE 0 END) AS active_cases
               FROM case_record
               WHERE respondent_name IS NOT NULL
                 AND respondent_name <> ''
                 AND UPPER(TRIM(respondent_name)) IN ({inClause})
                 {linkedClause}
               GROUP BY UPPER(TRIM(respondent_name))";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string key = reader["norm_name"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            key = NormalizeName(key);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            target[key] = ReadCounts(reader);
        }
    }

    private static string BuildInClause<T>(MySqlCommand cmd, IReadOnlyList<T> values, string prefix)
    {
        var names = new string[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            string name = prefix + i;
            names[i] = name;
            cmd.Parameters.AddWithValue(name, values[i]!);
        }

        return string.Join(", ", names);
    }

    private static RepeatRespondentCounts ReadCounts(MySqlDataReader reader)
    {
        int total = reader["total_cases"] == DBNull.Value ? 0 : Convert.ToInt32(reader["total_cases"]);
        int active = reader["active_cases"] == DBNull.Value ? 0 : Convert.ToInt32(reader["active_cases"]);
        return new RepeatRespondentCounts(total, active);
    }
}
