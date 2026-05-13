using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

public class BarangayOfficialService
{
	private const string CreateNewTermDisplay = "Create New Term";

	public async Task<DataTable> GetBarangayOfficialsAsync(string? search = null, string? position = null, string? status = null, int? termId = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("SELECT bo.official_id, bo.term_id, bo.resident_id, bo.position, bo.committee, bo.status, ");
		stringBuilder.Append("ot.term_start, ot.term_end, ot.notes, ");
		stringBuilder.Append("r.first_name, r.middle_name, r.last_name, r.suffix, r.contact_no, r.email, r.occupation, r.photo_url, r.status AS resident_status ");
		stringBuilder.Append("FROM barangay_official bo ");
		stringBuilder.Append("INNER JOIN official_term ot ON ot.term_id = bo.term_id ");
		stringBuilder.Append("INNER JOIN resident r ON r.resident_id = bo.resident_id ");
		stringBuilder.Append("WHERE ot.barangay_id = @barangayId ");
		Dictionary<string, object> dictionary = new Dictionary<string, object> { ["@barangayId"] = ResolveBarangayId() };
		if (!string.IsNullOrWhiteSpace(search))
		{
			stringBuilder.Append("AND (r.first_name LIKE @q OR r.middle_name LIKE @q OR r.last_name LIKE @q OR r.suffix LIKE @q ");
			stringBuilder.Append("OR bo.position LIKE @q OR bo.committee LIKE @q OR r.contact_no LIKE @q OR r.email LIKE @q) ");
			dictionary["@q"] = "%" + search.Trim() + "%";
		}
		if (!string.IsNullOrWhiteSpace(position) && !string.Equals(position, "All Positions", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.Append("AND bo.position = @position ");
			dictionary["@position"] = position.Trim();
		}
		if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All Statuses", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.Append("AND bo.status = @status ");
			dictionary["@status"] = status.Trim().ToUpperInvariant();
		}
		if (termId.HasValue && termId.Value > 0)
		{
			stringBuilder.Append("AND bo.term_id = @termId ");
			dictionary["@termId"] = termId.Value;
		}
		stringBuilder.Append("ORDER BY bo.status ASC, ot.term_start DESC, bo.position ASC, r.last_name ASC, r.first_name ASC ");
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync(stringBuilder.ToString(), BuildParameters(dictionary));
		NormalizeOfficialsTable(obj);
		return obj;
	}

	public async Task<IReadOnlyList<string>> GetPositionOptionsAsync()
	{
		return (from row in (await DatabaseManagerAsync.LoadTableAsync("SELECT DISTINCT bo.position\n                  FROM barangay_official bo\n                  INNER JOIN official_term ot ON ot.term_id = bo.term_id\n                  WHERE ot.barangay_id = @barangayId\n                    AND bo.position IS NOT NULL\n                    AND TRIM(bo.position) <> ''\n                  ORDER BY bo.position ASC", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
			})).AsEnumerable()
			select Convert.ToString(row["position"])?.Trim() into value
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string value) => value, StringComparer.OrdinalIgnoreCase).Cast<string>()
			.ToList();
	}

	public async Task<IReadOnlyList<OfficialTermOption>> GetTermOptionsAsync()
	{
		DataTable obj = await DatabaseManagerAsync.LoadTableAsync("SELECT term_id, term_start, term_end, notes\n                  FROM official_term\n                  WHERE barangay_id = @barangayId\n                  ORDER BY term_start DESC, term_end DESC, term_id DESC", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		});
		List<OfficialTermOption> list = new List<OfficialTermOption>();
		DateTime today = DateTime.Today;
		foreach (DataRow row in obj.Rows)
		{
			DateTime? termStart = ((row["term_start"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(row["term_start"], CultureInfo.InvariantCulture)));
			DateTime? termEnd = ((row["term_end"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(row["term_end"], CultureInfo.InvariantCulture)));
			list.Add(new OfficialTermOption
			{
				TermId = Convert.ToInt32(row["term_id"], CultureInfo.InvariantCulture),
				TermStart = termStart,
				TermEnd = termEnd,
				Notes = (Convert.ToString(row["notes"]) ?? string.Empty),
				IsCurrent = ((!termStart.HasValue || termStart.Value.Date <= today) && (!termEnd.HasValue || termEnd.Value.Date >= today)),
				DisplayName = BuildTermDisplay(termStart, termEnd)
			});
		}
		list.Sort(delegate(OfficialTermOption left, OfficialTermOption right)
		{
			int num = right.IsCurrent.CompareTo(left.IsCurrent);
			if (num != 0)
			{
				return num;
			}
			int num2 = Nullable.Compare(right.TermStart, left.TermStart);
			return (num2 != 0) ? num2 : right.TermId.CompareTo(left.TermId);
		});
		return list;
	}

	public async Task<IReadOnlyList<OfficialResidentOption>> GetResidentOptionsAsync()
	{
		int barangayId = ResolveBarangayId();
		List<OfficialResidentOption> residents = await LoadResidentOptionsAsync(barangayId, filterByBarangay: true).ConfigureAwait(continueOnCapturedContext: false);
		if (residents.Count == 0)
		{
			residents = await LoadResidentOptionsAsync(barangayId, filterByBarangay: false).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (residents.Count == 0 && OfflineDatabaseSupport.IsOffline)
		{
			List<OfficialResidentOption> list = await TryLoadResidentOptionsDirectAsync(barangayId, filterByBarangay: true).ConfigureAwait(continueOnCapturedContext: false);
			if (list.Count == 0)
			{
				list = await TryLoadResidentOptionsDirectAsync(barangayId, filterByBarangay: false).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (list.Count > 0)
			{
				OfflineDatabaseSupport.ActivateOnlineMode();
				residents = list;
			}
		}
		return residents;
	}

	public async Task<IReadOnlyList<OfficialResidentOption>> SearchResidentOptionsAsync(string? search = null, int limit = 10, int? preferredResidentId = null)
	{
		int barangayId = ResolveBarangayId();
		int safeLimit = Math.Clamp(limit, 1, 50);
		List<OfficialResidentOption> residents = await LoadResidentOptionsAsync(barangayId, filterByBarangay: true, search, safeLimit, preferredResidentId).ConfigureAwait(continueOnCapturedContext: false);
		if (residents.Count == 0)
		{
			residents = await LoadResidentOptionsAsync(barangayId, filterByBarangay: false, search, safeLimit, preferredResidentId).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (residents.Count == 0 && OfflineDatabaseSupport.IsOffline)
		{
			List<OfficialResidentOption> list = await TryLoadResidentOptionsDirectAsync(barangayId, filterByBarangay: true, search, safeLimit, preferredResidentId).ConfigureAwait(continueOnCapturedContext: false);
			if (list.Count == 0)
			{
				list = await TryLoadResidentOptionsDirectAsync(barangayId, filterByBarangay: false, search, safeLimit, preferredResidentId).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (list.Count > 0)
			{
				OfflineDatabaseSupport.ActivateOnlineMode();
				residents = list;
			}
		}
		return residents;
	}

	public async Task<BarangayOfficial?> GetBarangayOfficialDetailsAsync(int officialId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("SELECT bo.official_id, bo.term_id, bo.resident_id, bo.position, bo.committee, bo.status,\n                         ot.term_start, ot.term_end, ot.notes,\n                         r.first_name, r.middle_name, r.last_name, r.suffix, r.contact_no, r.email, r.occupation, r.photo_url, r.status AS resident_status\n                  FROM barangay_official bo\n                  INNER JOIN official_term ot ON ot.term_id = bo.term_id\n                  INNER JOIN resident r ON r.resident_id = bo.resident_id\n                  WHERE bo.official_id = @officialId\n                    AND ot.barangay_id = @barangayId\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@officialId", (object)officialId);
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
		});
		if (dataTable.Rows.Count == 0)
		{
			return null;
		}
		return MapOfficial(dataTable.Rows[0]);
	}

	public async Task<bool> OfficialAssignmentExistsAsync(int residentId, int termId, int excludeOfficialId = 0)
	{
		return await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*)\n                  FROM barangay_official\n                  WHERE resident_id = @residentId\n                    AND term_id = @termId\n                    AND (@excludeOfficialId = 0 OR official_id <> @excludeOfficialId)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@residentId", (object)residentId);
			cmd.Parameters.AddWithValue("@termId", (object)termId);
			cmd.Parameters.AddWithValue("@excludeOfficialId", (object)excludeOfficialId);
		}) > 0;
	}

	public async Task AddBarangayOfficialAsync(BarangayOfficial official)
	{
		int targetTermId = await ResolveTermIdAsync(official, 0);
		await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO barangay_official (term_id, resident_id, position, committee, status)\n                  VALUES (@termId, @residentId, @position, @committee, @status)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@termId", (object)targetTermId);
			cmd.Parameters.AddWithValue("@residentId", (object)official.ResidentId);
			cmd.Parameters.AddWithValue("@position", (object)NormalizeDbString(official.Position));
			cmd.Parameters.AddWithValue("@committee", (object)NormalizeDbString(official.Committee));
			cmd.Parameters.AddWithValue("@status", (object)NormalizeStatus(official.Status));
		});
	}

	public async Task UpdateBarangayOfficialAsync(BarangayOfficial official)
	{
		int targetTermId = await ResolveTermIdAsync(official, official.OfficialId);
		await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE barangay_official\n                  SET term_id = @termId,\n                      resident_id = @residentId,\n                      position = @position,\n                      committee = @committee,\n                      status = @status\n                  WHERE official_id = @officialId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@termId", (object)targetTermId);
			cmd.Parameters.AddWithValue("@residentId", (object)official.ResidentId);
			cmd.Parameters.AddWithValue("@position", (object)NormalizeDbString(official.Position));
			cmd.Parameters.AddWithValue("@committee", (object)NormalizeDbString(official.Committee));
			cmd.Parameters.AddWithValue("@status", (object)NormalizeStatus(official.Status));
			cmd.Parameters.AddWithValue("@officialId", (object)official.OfficialId);
		});
	}

	public async Task SetBarangayOfficialStatusAsync(int officialId, string status)
	{
		await DatabaseManagerAsync.ExecuteNonQueryAsync("UPDATE barangay_official SET status = @status WHERE official_id = @officialId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@status", (object)NormalizeStatus(status));
			cmd.Parameters.AddWithValue("@officialId", (object)officialId);
		});
	}

	public async Task DeleteBarangayOfficialAsync(int officialId)
	{
		await DatabaseManagerAsync.ExecuteNonQueryAsync("DELETE FROM barangay_official WHERE official_id = @officialId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@officialId", (object)officialId);
		});
	}

	public OfficialTermOption CreateNewTermOption()
	{
		return new OfficialTermOption
		{
			TermId = 0,
			IsCreateNewOption = true,
			DisplayName = "Create New Term"
		};
	}

	private async Task<int> ResolveTermIdAsync(BarangayOfficial official, int excludeOfficialId)
	{
		if (!official.CreateNewTerm && official.TermId > 0)
		{
			return official.TermId;
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("INSERT INTO official_term (barangay_id, term_start, term_end, notes)\n                  VALUES (@barangayId, @termStart, @termEnd, @notes)", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
			cmd.Parameters.AddWithValue("@termStart", official.TermStart.HasValue ? ((object)official.TermStart.Value.Date) : DBNull.Value);
			cmd.Parameters.AddWithValue("@termEnd", official.TermEnd.HasValue ? ((object)official.TermEnd.Value.Date) : DBNull.Value);
			cmd.Parameters.AddWithValue("@notes", (object)NormalizeDbString(official.TermNotes));
		});
		int num = await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT term_id\n                  FROM official_term\n                  WHERE barangay_id = @barangayId\n                    AND COALESCE(term_start, '1900-01-01') = COALESCE(@termStart, '1900-01-01')\n                    AND COALESCE(term_end, '1900-01-01') = COALESCE(@termEnd, '1900-01-01')\n                    AND COALESCE(notes, '') = @notes\n                  ORDER BY term_id DESC\n                  LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)ResolveBarangayId());
			cmd.Parameters.AddWithValue("@termStart", official.TermStart.HasValue ? ((object)official.TermStart.Value.Date) : DBNull.Value);
			cmd.Parameters.AddWithValue("@termEnd", official.TermEnd.HasValue ? ((object)official.TermEnd.Value.Date) : DBNull.Value);
			cmd.Parameters.AddWithValue("@notes", (object)NormalizeDbString(official.TermNotes));
		});
		if (num <= 0)
		{
			throw new InvalidOperationException("Unable to resolve the selected official term.");
		}
		return num;
	}

	private static void NormalizeOfficialsTable(DataTable table)
	{
		EnsureColumn(table, "full_name", typeof(string));
		EnsureColumn(table, "term_display", typeof(string));
		EnsureColumn(table, "term_sort_key", typeof(string));
		EnsureColumn(table, "status_display", typeof(string));
		foreach (DataRow row in table.Rows)
		{
			string value = BuildResidentFullName(row);
			DateTime? termStart = ((row["term_start"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(row["term_start"], CultureInfo.InvariantCulture)));
			DateTime? termEnd = ((row["term_end"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(row["term_end"], CultureInfo.InvariantCulture)));
			row["full_name"] = value;
			row["term_display"] = BuildTermDisplay(termStart, termEnd);
			row["term_sort_key"] = BuildTermSortKey(termStart, termEnd);
			row["status_display"] = NormalizeStatus(Convert.ToString(row["status"]));
		}
	}

	private static void EnsureColumn(DataTable table, string columnName, Type type)
	{
		if (!table.Columns.Contains(columnName))
		{
			table.Columns.Add(columnName, type);
		}
	}

	private static BarangayOfficial MapOfficial(DataRow row)
	{
		DateTime? termStart = ((row["term_start"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(row["term_start"], CultureInfo.InvariantCulture)));
		DateTime? termEnd = ((row["term_end"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(row["term_end"], CultureInfo.InvariantCulture)));
		return new BarangayOfficial
		{
			OfficialId = Convert.ToInt32(row["official_id"], CultureInfo.InvariantCulture),
			TermId = Convert.ToInt32(row["term_id"], CultureInfo.InvariantCulture),
			ResidentId = Convert.ToInt32(row["resident_id"], CultureInfo.InvariantCulture),
			Position = (Convert.ToString(row["position"]) ?? string.Empty),
			Committee = (Convert.ToString(row["committee"]) ?? string.Empty),
			Status = NormalizeStatus(Convert.ToString(row["status"])),
			ResidentFirstName = (Convert.ToString(row["first_name"]) ?? string.Empty),
			ResidentMiddleName = (Convert.ToString(row["middle_name"]) ?? string.Empty),
			ResidentLastName = (Convert.ToString(row["last_name"]) ?? string.Empty),
			ResidentSuffix = (Convert.ToString(row["suffix"]) ?? string.Empty),
			FullName = BuildResidentFullName(row),
			ContactNo = (Convert.ToString(row["contact_no"]) ?? string.Empty),
			Email = (Convert.ToString(row["email"]) ?? string.Empty),
			Occupation = (Convert.ToString(row["occupation"]) ?? string.Empty),
			PhotoUrl = (Convert.ToString(row["photo_url"]) ?? string.Empty),
			ResidentStatus = (Convert.ToString(row["resident_status"]) ?? string.Empty),
			TermStart = termStart,
			TermEnd = termEnd,
			TermNotes = (Convert.ToString(row["notes"]) ?? string.Empty),
			TermDisplay = BuildTermDisplay(termStart, termEnd)
		};
	}

	private static string BuildResidentFullName(DataRow row)
	{
		string text = Convert.ToString(row["first_name"])?.Trim() ?? string.Empty;
		string text2 = Convert.ToString(row["middle_name"])?.Trim() ?? string.Empty;
		string text3 = Convert.ToString(row["last_name"])?.Trim() ?? string.Empty;
		string text4 = Convert.ToString(row["suffix"])?.Trim() ?? string.Empty;
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
		string text5 = string.Join(" ", list).Trim();
		if (!string.IsNullOrWhiteSpace(text4))
		{
			text5 = (string.IsNullOrWhiteSpace(text5) ? text4 : (text5 + ", " + text4));
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			return text5;
		}
		return "Resident";
	}

	private static string BuildTermDisplay(DateTime? termStart, DateTime? termEnd)
	{
		string obj = (termStart.HasValue ? termStart.Value.ToString("MMM dd, yyyy") : "--");
		string text = (termEnd.HasValue ? termEnd.Value.ToString("MMM dd, yyyy") : "--");
		return obj + " - " + text;
	}

	private static string BuildTermSortKey(DateTime? termStart, DateTime? termEnd)
	{
		string obj = (termStart.HasValue ? termStart.Value.ToString("yyyyMMdd") : "00000000");
		string text = (termEnd.HasValue ? termEnd.Value.ToString("yyyyMMdd") : "99999999");
		return obj + "|" + text;
	}

	private static string NormalizeStatus(string? value)
	{
		if (!string.Equals(value, "INACTIVE", StringComparison.OrdinalIgnoreCase))
		{
			return "ACTIVE";
		}
		return "INACTIVE";
	}

	private static string NormalizeDbString(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return string.Empty;
	}

	private static int ResolveBarangayId()
	{
		if (UserSession.BarangayId <= 0)
		{
			return 1;
		}
		return UserSession.BarangayId;
	}

	private static string BuildResidentOptionsSql(bool filterByBarangay, string? search = null, int limit = 0, int? preferredResidentId = null)
	{
		int num = ((limit > 0) ? Math.Clamp(limit, 1, 50) : 0);
		string value = (string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim());
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("SELECT resident_id,\n                                first_name,\n                                middle_name,\n                                last_name,\n                                suffix,\n                                contact_no,\n                                email,\n                                occupation,\n                                photo_url,\n                                status\n                         FROM resident\n                         WHERE COALESCE(is_deleted, 0) = 0\n                           AND UPPER(COALESCE(status, 'ACTIVE')) = 'ACTIVE' ");
		if (filterByBarangay)
		{
			stringBuilder.Append("AND COALESCE(barangay_id, @barangayId) = @barangayId ");
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.Append("AND (");
			stringBuilder.Append("COALESCE(first_name, '') LIKE @residentQuery ");
			stringBuilder.Append("OR COALESCE(middle_name, '') LIKE @residentQuery ");
			stringBuilder.Append("OR COALESCE(last_name, '') LIKE @residentQuery ");
			stringBuilder.Append("OR COALESCE(suffix, '') LIKE @residentQuery ");
			stringBuilder.Append("OR COALESCE(contact_no, '') LIKE @residentQuery ");
			stringBuilder.Append("OR COALESCE(email, '') LIKE @residentQuery ");
			if (preferredResidentId.HasValue && preferredResidentId.Value > 0)
			{
				stringBuilder.Append("OR resident_id = @preferredResidentId ");
			}
			stringBuilder.Append(") ");
		}
		stringBuilder.Append("ORDER BY ");
		if (preferredResidentId.HasValue && preferredResidentId.Value > 0)
		{
			stringBuilder.Append("CASE WHEN resident_id = @preferredResidentId THEN 0 ELSE 1 END, ");
		}
		stringBuilder.Append("last_name ASC, first_name ASC, middle_name ASC ");
		if (num > 0)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
			handler.AppendLiteral("LIMIT ");
			handler.AppendFormatted(num);
			stringBuilder2.Append(ref handler);
		}
		return stringBuilder.ToString();
	}

	private static async Task<List<OfficialResidentOption>> LoadResidentOptionsAsync(int barangayId, bool filterByBarangay, string? search = null, int limit = 0, int? preferredResidentId = null)
	{
		string normalizedSearch = (string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim());
		return MapResidentOptions(await DatabaseManagerAsync.LoadTableAsync(BuildResidentOptionsSql(filterByBarangay, normalizedSearch, limit, preferredResidentId), delegate(MySqlCommand cmd)
		{
			if (filterByBarangay)
			{
				cmd.Parameters.AddWithValue("@barangayId", (object)barangayId);
			}
			if (!string.IsNullOrWhiteSpace(normalizedSearch))
			{
				cmd.Parameters.AddWithValue("@residentQuery", (object)("%" + normalizedSearch + "%"));
			}
			if (preferredResidentId.HasValue && preferredResidentId.Value > 0)
			{
				cmd.Parameters.AddWithValue("@preferredResidentId", (object)preferredResidentId.Value);
			}
		}).ConfigureAwait(continueOnCapturedContext: false));
	}

	private static async Task<List<OfficialResidentOption>> TryLoadResidentOptionsDirectAsync(int barangayId, bool filterByBarangay, string? search = null, int limit = 0, int? preferredResidentId = null)
	{
		_ = 1;
		try
		{
			string normalizedSearch = (string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim());
			string text = BuildDirectResidentLookupConnectionString();
			MySqlConnection connection = new MySqlConnection(text);
			try
			{
				await ((DbConnection)(object)connection).OpenAsync().ConfigureAwait(continueOnCapturedContext: false);
				MySqlCommand command = new MySqlCommand(BuildResidentOptionsSql(filterByBarangay, normalizedSearch, limit, preferredResidentId), connection);
				try
				{
					if (filterByBarangay)
					{
						command.Parameters.AddWithValue("@barangayId", (object)barangayId);
					}
					if (!string.IsNullOrWhiteSpace(normalizedSearch))
					{
						command.Parameters.AddWithValue("@residentQuery", (object)("%" + normalizedSearch + "%"));
					}
					if (preferredResidentId.HasValue && preferredResidentId.Value > 0)
					{
						command.Parameters.AddWithValue("@preferredResidentId", (object)preferredResidentId.Value);
					}
					MySqlDataAdapter adapter = new MySqlDataAdapter(command);
					try
					{
						DataTable table = new DataTable();
						await Task.Run(() => ((DbDataAdapter)(object)adapter).Fill(table)).ConfigureAwait(continueOnCapturedContext: false);
						return MapResidentOptions(table);
					}
					finally
					{
						if (adapter != null)
						{
							((IDisposable)adapter).Dispose();
						}
					}
				}
				finally
				{
					((IDisposable)command)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Officials resident lookup direct online retry failed.", ex);
			return new List<OfficialResidentOption>();
		}
	}

	private static string BuildDirectResidentLookupConnectionString()
	{
		if (DbConnectionSettingsStore.TryLoad(out DatabaseConnectionProfile profile))
		{
			return DbConnectionSettingsStore.BuildConnectionString(profile);
		}
		return DBConnection.GetCurrentConnectionString();
	}

	private static List<OfficialResidentOption> MapResidentOptions(DataTable table)
	{
		List<OfficialResidentOption> list = new List<OfficialResidentOption>();
		foreach (DataRow row in table.Rows)
		{
			list.Add(new OfficialResidentOption
			{
				ResidentId = Convert.ToInt32(row["resident_id"], CultureInfo.InvariantCulture),
				FullName = BuildResidentFullName(row),
				ContactNo = (Convert.ToString(row["contact_no"]) ?? string.Empty),
				Email = (Convert.ToString(row["email"]) ?? string.Empty),
				Occupation = (Convert.ToString(row["occupation"]) ?? string.Empty),
				PhotoUrl = (Convert.ToString(row["photo_url"]) ?? string.Empty),
				Status = (Convert.ToString(row["status"]) ?? string.Empty)
			});
		}
		return list;
	}

	private static Action<MySqlCommand> BuildParameters(Dictionary<string, object?> parameters)
	{
		return delegate(MySqlCommand cmd)
		{
			foreach (var (text2, obj2) in parameters)
			{
				cmd.Parameters.AddWithValue(text2, obj2 ?? DBNull.Value);
			}
		};
	}
}
