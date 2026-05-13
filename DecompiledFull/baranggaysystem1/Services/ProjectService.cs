using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class ProjectService
{
	private static readonly string[] AllowedRecordTypes = new string[2] { "Project", "Program" };

	private static readonly string[] AllowedStatuses = new string[4] { "Planned", "Ongoing", "On hold", "Completed" };

	private static readonly string[] AllowedOutcomeStatuses = new string[4] { "Pending", "In progress", "Needs follow-up", "Achieved" };

	public async Task<IReadOnlyList<ProjectRecord>> GetRecentProjectsAsync(int limit = 10)
	{
		return (await DatabaseManagerAsync.LoadTableAsync(BuildProjectQuery(limit)).ConfigureAwait(continueOnCapturedContext: false)).AsEnumerable().Select(MapProject).ToList();
	}

	public async Task<DataTable> GetProjectRegistryAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync(BuildProjectQuery()).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<ProjectRecord?> GetProjectAsync(int projectId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("SELECT project_id,\n                         COALESCE(record_type, 'Project') AS record_type,\n                         name,\n                         COALESCE(status, 'Planned') AS status,\n                         IFNULL(budget, 0.00) AS budget,\n                         start_date,\n                         end_date,\n                         COALESCE(lead, '') AS lead,\n                         COALESCE(remarks, '') AS remarks,\n                         IFNULL(attendance_target, 0) AS attendance_target,\n                         IFNULL(attendance_count, 0) AS attendance_count,\n                         last_activity_date,\n                         COALESCE(outcome_status, 'Pending') AS outcome_status,\n                         COALESCE(outcome_summary, '') AS outcome_summary,\n                         created_at\n                  FROM projects\n                  WHERE project_id = @projectId\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@projectId", (object)projectId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		return (dataTable.Rows.Count == 0) ? null : MapProject(dataTable.Rows[0]);
	}

	public async Task CreateProjectAsync(ProjectRecord record)
	{
		ProjectRecord sanitized = Sanitize(record);
		await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO projects\n                    (record_type, name, status, budget, start_date, end_date, lead, remarks, attendance_target, attendance_count, last_activity_date, outcome_status, outcome_summary)\n                  VALUES\n                    (@recordType, @name, @status, @budget, @startDate, @endDate, @lead, @remarks, @attendanceTarget, @attendanceCount, @lastActivityDate, @outcomeStatus, @outcomeSummary)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@recordType", (object)sanitized.RecordType);
			cmd.Parameters.AddWithValue("@name", (object)sanitized.Name);
			cmd.Parameters.AddWithValue("@status", (object)sanitized.Status);
			cmd.Parameters.AddWithValue("@budget", (object)sanitized.Budget);
			cmd.Parameters.AddWithValue("@startDate", DbNullIfDateMissing(sanitized.StartDate));
			cmd.Parameters.AddWithValue("@endDate", DbNullIfDateMissing(sanitized.EndDate));
			cmd.Parameters.AddWithValue("@lead", DbNullIfEmpty(sanitized.Lead));
			cmd.Parameters.AddWithValue("@remarks", DbNullIfEmpty(sanitized.Remarks));
			cmd.Parameters.AddWithValue("@attendanceTarget", (object)sanitized.AttendanceTarget);
			cmd.Parameters.AddWithValue("@attendanceCount", (object)sanitized.AttendanceCount);
			cmd.Parameters.AddWithValue("@lastActivityDate", DbNullIfDateMissing(sanitized.LastActivityDate));
			cmd.Parameters.AddWithValue("@outcomeStatus", (object)sanitized.OutcomeStatus);
			cmd.Parameters.AddWithValue("@outcomeSummary", DbNullIfEmpty(sanitized.OutcomeSummary));
		}).ConfigureAwait(continueOnCapturedContext: false);
		AuditTrailService.Log("Projects", "project", sanitized.Name, "CREATE", null, sanitized, "Project record created from the initiatives workflow.");
	}

	public async Task UpdateProjectAsync(ProjectRecord record)
	{
		if (record.ProjectId <= 0)
		{
			throw new InvalidOperationException("Project ID is required for updates.");
		}
		ProjectRecord before = await GetProjectAsync(record.ProjectId).ConfigureAwait(continueOnCapturedContext: false);
		if (before == null)
		{
			throw new InvalidOperationException("The selected project could not be found.");
		}
		ProjectRecord sanitized = Sanitize(record);
		sanitized.ProjectId = record.ProjectId;
		sanitized.CreatedAt = before.CreatedAt;
		await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE projects\n                  SET record_type = @recordType,\n                      name = @name,\n                      status = @status,\n                      budget = @budget,\n                      start_date = @startDate,\n                      end_date = @endDate,\n                      lead = @lead,\n                      remarks = @remarks,\n                      attendance_target = @attendanceTarget,\n                      attendance_count = @attendanceCount,\n                      last_activity_date = @lastActivityDate,\n                      outcome_status = @outcomeStatus,\n                      outcome_summary = @outcomeSummary\n                  WHERE project_id = @projectId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@projectId", (object)sanitized.ProjectId);
			cmd.Parameters.AddWithValue("@recordType", (object)sanitized.RecordType);
			cmd.Parameters.AddWithValue("@name", (object)sanitized.Name);
			cmd.Parameters.AddWithValue("@status", (object)sanitized.Status);
			cmd.Parameters.AddWithValue("@budget", (object)sanitized.Budget);
			cmd.Parameters.AddWithValue("@startDate", DbNullIfDateMissing(sanitized.StartDate));
			cmd.Parameters.AddWithValue("@endDate", DbNullIfDateMissing(sanitized.EndDate));
			cmd.Parameters.AddWithValue("@lead", DbNullIfEmpty(sanitized.Lead));
			cmd.Parameters.AddWithValue("@remarks", DbNullIfEmpty(sanitized.Remarks));
			cmd.Parameters.AddWithValue("@attendanceTarget", (object)sanitized.AttendanceTarget);
			cmd.Parameters.AddWithValue("@attendanceCount", (object)sanitized.AttendanceCount);
			cmd.Parameters.AddWithValue("@lastActivityDate", DbNullIfDateMissing(sanitized.LastActivityDate));
			cmd.Parameters.AddWithValue("@outcomeStatus", (object)sanitized.OutcomeStatus);
			cmd.Parameters.AddWithValue("@outcomeSummary", DbNullIfEmpty(sanitized.OutcomeSummary));
		}).ConfigureAwait(continueOnCapturedContext: false);
		AuditTrailService.Log("Projects", "project", sanitized.ProjectId, "UPDATE", before, sanitized, "Project record updated from the initiatives workflow.");
	}

	public async Task DeleteProjectAsync(int projectId)
	{
		ProjectRecord before = await GetProjectAsync(projectId).ConfigureAwait(continueOnCapturedContext: false);
		if (before == null)
		{
			throw new InvalidOperationException("The selected project could not be found.");
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM projects WHERE project_id = @projectId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@projectId", (object)projectId);
		}).ConfigureAwait(continueOnCapturedContext: false);
		AuditTrailService.Log("Projects", "project", projectId, "DELETE", before, null, "Project record deleted from the initiatives workflow.");
	}

	private static string BuildProjectQuery(int? limit = null)
	{
		string text = "SELECT project_id,\n                         COALESCE(record_type, 'Project') AS record_type,\n                         name,\n                         COALESCE(status, 'Planned') AS status,\n                         IFNULL(budget, 0.00) AS budget,\n                         start_date,\n                         end_date,\n                         COALESCE(lead, '') AS lead,\n                         COALESCE(remarks, '') AS remarks,\n                         IFNULL(attendance_target, 0) AS attendance_target,\n                         IFNULL(attendance_count, 0) AS attendance_count,\n                         last_activity_date,\n                         COALESCE(outcome_status, 'Pending') AS outcome_status,\n                         COALESCE(outcome_summary, '') AS outcome_summary,\n                         created_at\n                  FROM projects\n                  ORDER BY created_at DESC,\n                           project_id DESC";
		if (limit.HasValue)
		{
			text += $"\nLIMIT {Math.Clamp(limit.Value, 1, 50)}";
		}
		return text;
	}

	private static ProjectRecord Sanitize(ProjectRecord record)
	{
		string text = (record.Name ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("Project name is required.");
		}
		if (text.Length > 150)
		{
			throw new InvalidOperationException("Project name must be 150 characters or fewer.");
		}
		if (!string.IsNullOrWhiteSpace(record.Lead) && record.Lead.Trim().Length > 100)
		{
			throw new InvalidOperationException("Project lead must be 100 characters or fewer.");
		}
		if (record.Budget < 0m)
		{
			throw new InvalidOperationException("Project budget cannot be negative.");
		}
		DateTime? startDate = record.StartDate?.Date;
		DateTime? endDate = record.EndDate?.Date;
		if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
		{
			throw new InvalidOperationException("Project end date cannot be earlier than the start date.");
		}
		return new ProjectRecord
		{
			RecordType = NormalizeOption(record.RecordType, AllowedRecordTypes, "Project"),
			Name = text,
			Status = NormalizeOption(record.Status, AllowedStatuses, "Planned"),
			Budget = decimal.Round(record.Budget, 2, MidpointRounding.AwayFromZero),
			StartDate = startDate,
			EndDate = endDate,
			Lead = TrimToLength(record.Lead, 100),
			Remarks = TrimToLength(record.Remarks, 4000),
			AttendanceTarget = NormalizeWholeNumber(record.AttendanceTarget, "Attendance target"),
			AttendanceCount = NormalizeWholeNumber(record.AttendanceCount, "Attendance count"),
			LastActivityDate = record.LastActivityDate?.Date,
			OutcomeStatus = NormalizeOption(record.OutcomeStatus, AllowedOutcomeStatuses, "Pending"),
			OutcomeSummary = TrimToLength(record.OutcomeSummary, 4000)
		};
	}

	private static ProjectRecord MapProject(DataRow row)
	{
		return new ProjectRecord
		{
			ProjectId = ReadInt(row, "project_id"),
			RecordType = NormalizeOption(ReadString(row, "record_type"), AllowedRecordTypes, "Project"),
			Name = ReadString(row, "name"),
			Status = NormalizeOption(ReadString(row, "status"), AllowedStatuses, "Planned"),
			Budget = ReadDecimal(row, "budget"),
			StartDate = ReadDateTime(row, "start_date"),
			EndDate = ReadDateTime(row, "end_date"),
			Lead = ReadString(row, "lead"),
			Remarks = ReadString(row, "remarks"),
			AttendanceTarget = ReadInt(row, "attendance_target"),
			AttendanceCount = ReadInt(row, "attendance_count"),
			LastActivityDate = ReadDateTime(row, "last_activity_date"),
			OutcomeStatus = NormalizeOption(ReadString(row, "outcome_status"), AllowedOutcomeStatuses, "Pending"),
			OutcomeSummary = ReadString(row, "outcome_summary"),
			CreatedAt = ReadDateTime(row, "created_at")
		};
	}

	private static int NormalizeWholeNumber(int value, string fieldName)
	{
		if (value < 0)
		{
			throw new InvalidOperationException(fieldName + " cannot be negative.");
		}
		return value;
	}

	private static string NormalizeOption(string? value, IReadOnlyList<string> allowedValues, string fallback)
	{
		string normalized = (value ?? string.Empty).Trim();
		return allowedValues.FirstOrDefault((string option) => string.Equals(option, normalized, StringComparison.OrdinalIgnoreCase)) ?? fallback;
	}

	private static string TrimToLength(string? value, int maxLength)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length > maxLength)
		{
			return text.Substring(0, maxLength);
		}
		return text;
	}

	private static object DbNullIfEmpty(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static object DbNullIfDateMissing(DateTime? value)
	{
		if (!value.HasValue)
		{
			return DBNull.Value;
		}
		return value.Value;
	}

	private static int ReadInt(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0;
		}
		return Convert.ToInt32(row[columnName], CultureInfo.InvariantCulture);
	}

	private static decimal ReadDecimal(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0m;
		}
		return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
	}

	private static string ReadString(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(row[columnName], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
	}

	private static DateTime? ReadDateTime(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return null;
		}
		object obj = row[columnName];
		if (obj is DateTime)
		{
			return (DateTime)obj;
		}
		if (!DateTime.TryParse(Convert.ToString(row[columnName], CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
		{
			return null;
		}
		return result;
	}
}
