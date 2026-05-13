using System;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class CaseTimelineService
{
	private const int MaxEventTypeLength = 50;

	private const int MaxTitleLength = 150;

	private const int MaxStatusLength = 30;

	private const int MaxDetailsLength = 8000;

	public static void Log(int caseId, string eventType, string title, string? details = null, string? fromStatus = null, string? toStatus = null, int? userId = null)
	{
		if (caseId <= 0)
		{
			return;
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				MySqlCommand val = BuildInsertCommand(connection, null, caseId, eventType, title, details, fromStatus, toStatus, userId);
				try
				{
					((DbCommand)(object)val).ExecuteNonQuery();
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
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to write blotter timeline entry.", ex);
		}
	}

	public static void LogTransactional(MySqlConnection conn, MySqlTransaction tx, int caseId, string eventType, string title, string? details = null, string? fromStatus = null, string? toStatus = null, int? userId = null)
	{
		if (caseId <= 0)
		{
			return;
		}
		try
		{
			MySqlCommand val = BuildInsertCommand(conn, tx, caseId, eventType, title, details, fromStatus, toStatus, userId);
			try
			{
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to write blotter timeline entry (transactional).", ex);
		}
	}

	public static DataTable LoadTimeline(int caseId, int limit = 80)
	{
		if (caseId <= 0)
		{
			return new DataTable();
		}
		int value = Math.Clamp(limit, 5, 200);
		return DbHelper.LoadTable($"SELECT ct.timeline_id,\n                               ct.created_at,\n                               ct.event_type,\n                               ct.event_title,\n                               ct.event_details,\n                               ct.from_status,\n                               ct.to_status,\n                               u.username AS created_by\n                        FROM case_timeline ct\n                        LEFT JOIN user_account u ON u.user_id = ct.created_by_user_id\n                        WHERE ct.case_id = @id\n                        ORDER BY ct.created_at DESC, ct.timeline_id DESC\n                        LIMIT {value}", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@id", (object)caseId);
		});
	}

	private static MySqlCommand BuildInsertCommand(MySqlConnection conn, MySqlTransaction? tx, int caseId, string eventType, string title, string? details, string? fromStatus, string? toStatus, int? userId)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		string text = Normalize(eventType, 50, "EVENT");
		string text2 = Normalize(title, 150, "Update");
		string text3 = Normalize(details, 8000, string.Empty);
		string text4 = Normalize(fromStatus, 30, string.Empty);
		string text5 = Normalize(toStatus, 30, string.Empty);
		MySqlCommand val = new MySqlCommand("INSERT INTO case_timeline\n                (case_id, event_type, event_title, event_details, from_status, to_status, created_by_user_id)\n              VALUES\n                (@case_id, @event_type, @event_title, @event_details, @from_status, @to_status, @created_by)", conn)
		{
			Transaction = tx
		};
		val.Parameters.AddWithValue("@case_id", (object)caseId);
		val.Parameters.AddWithValue("@event_type", (object)text);
		val.Parameters.AddWithValue("@event_title", (object)text2);
		val.Parameters.AddWithValue("@event_details", (object)(string.IsNullOrWhiteSpace(text3) ? ((IConvertible)DBNull.Value) : ((IConvertible)text3)));
		val.Parameters.AddWithValue("@from_status", (object)(string.IsNullOrWhiteSpace(text4) ? ((IConvertible)DBNull.Value) : ((IConvertible)text4)));
		val.Parameters.AddWithValue("@to_status", (object)(string.IsNullOrWhiteSpace(text5) ? ((IConvertible)DBNull.Value) : ((IConvertible)text5)));
		val.Parameters.AddWithValue("@created_by", userId.HasValue ? ((object)userId.Value) : DBNull.Value);
		return val;
	}

	private static string Normalize(string? value, int maxLen, string fallback)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return fallback;
		}
		if (text.Length > maxLen)
		{
			return text.Substring(0, maxLen);
		}
		return text;
	}
}
