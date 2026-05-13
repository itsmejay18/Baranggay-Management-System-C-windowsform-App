using System;
using System.Collections.Generic;
using System.Data;
using baranggaysystem1.Database;

namespace baranggaysystem1;

/// <summary>
/// Service for tracking repeat respondents in blotter cases.
/// </summary>
public static class RepeatRespondentService
{
    /// <summary>
    /// Normalizes a respondent name for comparison.
    /// </summary>
    public static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return name.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Gets the count of cases where a resident was a respondent.
    /// </summary>
    public static RepeatRespondentCounts GetCounts(int residentId)
    {
        var table = DbHelper.LoadTable(
            @"SELECT
                COUNT(*) AS total_cases,
                SUM(CASE WHEN cr.status IN ('OPEN','ONGOING') THEN 1 ELSE 0 END) AS active_cases,
                SUM(CASE WHEN cr.status IN ('RESOLVED','CLOSED','SETTLED') THEN 1 ELSE 0 END) AS resolved_cases,
                MAX(cr.created_at) AS last_case_date
              FROM case_record cr
              INNER JOIN case_respondent cres ON cres.case_id = cr.case_id
              WHERE cres.resident_id = @residentId",
            cmd => cmd.Parameters.AddWithValue("@residentId", residentId));

        if (table.Rows.Count == 0 || table.Rows[0]["total_cases"] == DBNull.Value)
        {
            return RepeatRespondentCounts.Zero;
        }

        var row = table.Rows[0];
        return new RepeatRespondentCounts
        {
            ResidentId = residentId,
            TotalCases = Convert.ToInt32(row["total_cases"]),
            ActiveCases = Convert.ToInt32(row["active_cases"]),
            ResolvedCases = Convert.ToInt32(row["resolved_cases"]),
            LastCaseDate = row["last_case_date"] != DBNull.Value ? Convert.ToDateTime(row["last_case_date"]) : null
        };
    }

    /// <summary>
    /// Gets counts for a respondent by resident ID and/or name.
    /// </summary>
    public static RepeatRespondentCounts GetCountsForRespondent(int? residentId, string? respondentName)
    {
        if (residentId.HasValue && residentId.Value > 0)
        {
            return GetCounts(residentId.Value);
        }

        if (string.IsNullOrWhiteSpace(respondentName))
        {
            return RepeatRespondentCounts.Zero;
        }

        string normalized = NormalizeName(respondentName);
        var table = DbHelper.LoadTable(
            @"SELECT
                COUNT(*) AS total_cases,
                SUM(CASE WHEN cr.status IN ('OPEN','ONGOING') THEN 1 ELSE 0 END) AS active_cases,
                SUM(CASE WHEN cr.status IN ('RESOLVED','CLOSED','SETTLED') THEN 1 ELSE 0 END) AS resolved_cases,
                MAX(cr.created_at) AS last_case_date
              FROM case_record cr
              INNER JOIN case_respondent cres ON cres.case_id = cr.case_id
              WHERE UPPER(TRIM(cres.respondent_name)) = @name",
            cmd => cmd.Parameters.AddWithValue("@name", normalized));

        if (table.Rows.Count == 0 || table.Rows[0]["total_cases"] == DBNull.Value)
        {
            return RepeatRespondentCounts.Zero;
        }

        var row = table.Rows[0];
        return new RepeatRespondentCounts
        {
            ResidentId = null,
            TotalCases = Convert.ToInt32(row["total_cases"]),
            ActiveCases = Convert.ToInt32(row["active_cases"]),
            ResolvedCases = Convert.ToInt32(row["resolved_cases"]),
            LastCaseDate = row["last_case_date"] != DBNull.Value ? Convert.ToDateTime(row["last_case_date"]) : null
        };
    }

    /// <summary>
    /// Gets repeat respondent info for all respondents in a given case.
    /// </summary>
    public static RepeatRespondentBatch GetBatchForCase(int caseId)
    {
        var respondentTable = DbHelper.LoadTable(
            "SELECT resident_id FROM case_respondent WHERE case_id = @caseId",
            cmd => cmd.Parameters.AddWithValue("@caseId", caseId));

        var items = new List<RepeatRespondentCounts>();

        foreach (DataRow row in respondentTable.Rows)
        {
            int residentId = Convert.ToInt32(row["resident_id"]);
            items.Add(GetCounts(residentId));
        }

        return new RepeatRespondentBatch(items);
    }

    /// <summary>
    /// Loads counts for a batch of resident IDs and names.
    /// </summary>
    public static RepeatRespondentBatch LoadCounts(
        IEnumerable<int> residentIds,
        IEnumerable<string> namesAll,
        IEnumerable<string> namesNullIdOnly)
    {
        var byResidentId = new Dictionary<int, RepeatRespondentCounts>();
        var byNameAll = new Dictionary<string, RepeatRespondentCounts>(StringComparer.Ordinal);
        var byNameNullIdOnly = new Dictionary<string, RepeatRespondentCounts>(StringComparer.Ordinal);

        foreach (int id in residentIds)
        {
            if (!byResidentId.ContainsKey(id))
            {
                byResidentId[id] = GetCounts(id);
            }
        }

        foreach (string name in namesAll)
        {
            if (!byNameAll.ContainsKey(name))
            {
                byNameAll[name] = GetCountsByName(name, includeLinked: true);
            }
        }

        foreach (string name in namesNullIdOnly)
        {
            if (!byNameNullIdOnly.ContainsKey(name))
            {
                byNameNullIdOnly[name] = GetCountsByName(name, includeLinked: false);
            }
        }

        return new RepeatRespondentBatch(byResidentId, byNameAll, byNameNullIdOnly);
    }

    private static RepeatRespondentCounts GetCountsByName(string normalizedName, bool includeLinked)
    {
        string condition = includeLinked
            ? "UPPER(TRIM(cres.respondent_name)) = @name"
            : "UPPER(TRIM(cres.respondent_name)) = @name AND (cres.resident_id IS NULL OR cres.resident_id = 0)";

        var table = DbHelper.LoadTable(
            $@"SELECT
                COUNT(*) AS total_cases,
                SUM(CASE WHEN cr.status IN ('OPEN','ONGOING') THEN 1 ELSE 0 END) AS active_cases,
                SUM(CASE WHEN cr.status IN ('RESOLVED','CLOSED','SETTLED') THEN 1 ELSE 0 END) AS resolved_cases,
                MAX(cr.created_at) AS last_case_date
              FROM case_record cr
              INNER JOIN case_respondent cres ON cres.case_id = cr.case_id
              WHERE {condition}",
            cmd => cmd.Parameters.AddWithValue("@name", normalizedName));

        if (table.Rows.Count == 0 || table.Rows[0]["total_cases"] == DBNull.Value)
        {
            return RepeatRespondentCounts.Zero;
        }

        var row = table.Rows[0];
        return new RepeatRespondentCounts
        {
            ResidentId = null,
            TotalCases = Convert.ToInt32(row["total_cases"]),
            ActiveCases = Convert.ToInt32(row["active_cases"]),
            ResolvedCases = Convert.ToInt32(row["resolved_cases"]),
            LastCaseDate = row["last_case_date"] != DBNull.Value ? Convert.ToDateTime(row["last_case_date"]) : null
        };
    }
}

/// <summary>
/// Counts of cases where a resident was a respondent.
/// </summary>
public sealed class RepeatRespondentCounts
{
    public static readonly RepeatRespondentCounts Zero = new();

    public int? ResidentId { get; set; }
    public int TotalCases { get; set; }
    public int ActiveCases { get; set; }
    public int ResolvedCases { get; set; }
    public DateTime? LastCaseDate { get; set; }

    /// <summary>
    /// Combines counts from another instance.
    /// </summary>
    public RepeatRespondentCounts Add(RepeatRespondentCounts other)
    {
        return new RepeatRespondentCounts
        {
            ResidentId = ResidentId ?? other.ResidentId,
            TotalCases = TotalCases + other.TotalCases,
            ActiveCases = ActiveCases + other.ActiveCases,
            ResolvedCases = ResolvedCases + other.ResolvedCases,
            LastCaseDate = other.LastCaseDate.HasValue
                ? (LastCaseDate.HasValue ? (other.LastCaseDate > LastCaseDate ? other.LastCaseDate : LastCaseDate) : other.LastCaseDate)
                : LastCaseDate
        };
    }
}

/// <summary>
/// Batch of repeat respondent counts.
/// </summary>
public sealed class RepeatRespondentBatch
{
    public IReadOnlyList<RepeatRespondentCounts> Items { get; }
    public IReadOnlyDictionary<int, RepeatRespondentCounts> ByResidentId { get; }
    public IReadOnlyDictionary<string, RepeatRespondentCounts> ByNameAll { get; }
    public IReadOnlyDictionary<string, RepeatRespondentCounts> ByNameNullIdOnly { get; }

    public RepeatRespondentBatch(IReadOnlyList<RepeatRespondentCounts> items)
    {
        Items = items;
        ByResidentId = new Dictionary<int, RepeatRespondentCounts>();
        ByNameAll = new Dictionary<string, RepeatRespondentCounts>();
        ByNameNullIdOnly = new Dictionary<string, RepeatRespondentCounts>();
    }

    public RepeatRespondentBatch(
        Dictionary<int, RepeatRespondentCounts> byResidentId,
        Dictionary<string, RepeatRespondentCounts> byNameAll,
        Dictionary<string, RepeatRespondentCounts> byNameNullIdOnly)
    {
        Items = new List<RepeatRespondentCounts>(byResidentId.Values);
        ByResidentId = byResidentId;
        ByNameAll = byNameAll;
        ByNameNullIdOnly = byNameNullIdOnly;
    }
}
