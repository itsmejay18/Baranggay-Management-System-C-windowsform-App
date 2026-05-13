using System;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1.helper;

internal static class AuditTrailService
{
	private static readonly object Sync = new object();

	private static bool _onlineSchemaEnsured;

	private static bool _offlineSchemaEnsured;

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = false
	};

	internal static void Log(string module, string entityType, object? entityId, string action, object? beforeState = null, object? afterState = null, string? notes = null, int? actionBy = null)
	{
		try
		{
			EnsureSchema();
			DbHelper.ExecuteNonQuery("INSERT INTO audit_trail\n                    (module, entity_type, entity_id, action, before_json, after_json, notes, action_by)\n                  VALUES\n                    (@module, @entityType, @entityId, @action, @before, @after, @notes, @by)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@module", (object)module);
				cmd.Parameters.AddWithValue("@entityType", (object)entityType);
				cmd.Parameters.AddWithValue("@entityId", (object)entityId?.ToString());
				cmd.Parameters.AddWithValue("@action", (object)action);
				cmd.Parameters.AddWithValue("@before", ToDbValue(Serialize(beforeState)));
				cmd.Parameters.AddWithValue("@after", ToDbValue(Serialize(afterState)));
				cmd.Parameters.AddWithValue("@notes", ToDbValue(notes));
				cmd.Parameters.AddWithValue("@by", (object)(actionBy ?? UserSession.UserId));
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogError("AuditTrailService.Log failed.", ex);
		}
	}

	internal static void LogTransactional(MySqlConnection connection, MySqlTransaction? transaction, string module, string entityType, object? entityId, string action, object? beforeState = null, object? afterState = null, string? notes = null, int? actionBy = null)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (connection == null)
		{
			throw new ArgumentNullException("connection");
		}
		if (((DbConnection)(object)connection).State != ConnectionState.Open)
		{
			throw new InvalidOperationException("Audit logging requires an open connection.");
		}
		EnsureOnlineSchema(connection, transaction);
		MySqlCommand val = new MySqlCommand("INSERT INTO audit_trail\n                (module, entity_type, entity_id, action, before_json, after_json, notes, action_by)\n              VALUES\n                (@module, @entityType, @entityId, @action, @before, @after, @notes, @by)", connection, transaction);
		try
		{
			val.Parameters.AddWithValue("@module", (object)module);
			val.Parameters.AddWithValue("@entityType", (object)entityType);
			val.Parameters.AddWithValue("@entityId", (object)entityId?.ToString());
			val.Parameters.AddWithValue("@action", (object)action);
			val.Parameters.AddWithValue("@before", ToDbValue(Serialize(beforeState)));
			val.Parameters.AddWithValue("@after", ToDbValue(Serialize(afterState)));
			val.Parameters.AddWithValue("@notes", ToDbValue(notes));
			val.Parameters.AddWithValue("@by", (object)(actionBy ?? UserSession.UserId));
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	internal static void EnsureSchema()
	{
		if (OfflineDatabaseSupport.IsOffline)
		{
			EnsureOfflineSchema();
		}
		else
		{
			EnsureOnlineSchema();
		}
	}

	private static void EnsureOnlineSchema(MySqlConnection? connection = null, MySqlTransaction? transaction = null)
	{
		if (_onlineSchemaEnsured)
		{
			return;
		}
		lock (Sync)
		{
			if (_onlineSchemaEnsured)
			{
				return;
			}
			if (connection != null)
			{
				EnsureOnlineSchemaCore(connection, transaction);
			}
			else
			{
				MySqlConnection connection2 = DBConnection.GetConnection();
				try
				{
					((DbConnection)(object)connection2).Open();
					EnsureOnlineSchemaCore(connection2, null);
				}
				finally
				{
					((IDisposable)connection2)?.Dispose();
				}
			}
			_onlineSchemaEnsured = true;
		}
	}

	private static void EnsureOnlineSchemaCore(MySqlConnection connection, MySqlTransaction? transaction)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("CREATE TABLE IF NOT EXISTS audit_trail (\n                audit_id BIGINT AUTO_INCREMENT PRIMARY KEY,\n                module VARCHAR(60) NOT NULL,\n                entity_type VARCHAR(60) NOT NULL,\n                entity_id VARCHAR(64) NULL,\n                action VARCHAR(60) NOT NULL,\n                before_json LONGTEXT NULL,\n                after_json LONGTEXT NULL,\n                notes TEXT NULL,\n                action_by INT NULL,\n                action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                INDEX idx_audit_entity (entity_type, entity_id),\n                INDEX idx_audit_module (module),\n                INDEX idx_audit_action_at (action_at)\n              )", connection, transaction);
		try
		{
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void EnsureOfflineSchema()
	{
		if (_offlineSchemaEnsured)
		{
			return;
		}
		lock (Sync)
		{
			if (_offlineSchemaEnsured)
			{
				return;
			}
			SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
			try
			{
				EnsureOfflineSchemaCore(connection);
				_offlineSchemaEnsured = true;
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
	}

	private static void EnsureOfflineSchemaCore(SqliteConnection connection)
	{
		SqliteCommand val = connection.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "CREATE TABLE IF NOT EXISTS audit_trail (\n                    audit_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,\n                    module TEXT NOT NULL,\n                    entity_type TEXT NOT NULL,\n                    entity_id TEXT NULL,\n                    action TEXT NOT NULL,\n                    before_json TEXT NULL,\n                    after_json TEXT NULL,\n                    notes TEXT NULL,\n                    action_by INTEGER NULL,\n                    action_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,\n                    sync_status TEXT NOT NULL DEFAULT 'pending'\n                  )";
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		EnsureOfflineColumn(connection, "audit_trail", "sync_status", "TEXT NOT NULL DEFAULT 'pending'");
		EnsureOfflineIndex(connection, "CREATE INDEX IF NOT EXISTS idx_audit_entity ON audit_trail (entity_type, entity_id)");
		EnsureOfflineIndex(connection, "CREATE INDEX IF NOT EXISTS idx_audit_module ON audit_trail (module)");
		EnsureOfflineIndex(connection, "CREATE INDEX IF NOT EXISTS idx_audit_action_at ON audit_trail (action_at)");
	}

	private static void EnsureOfflineColumn(SqliteConnection connection, string tableName, string columnName, string definition)
	{
		SqliteCommand val = connection.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "PRAGMA table_info(" + tableName + ");";
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val2).Read())
				{
					if (string.Equals((((DbDataReader)(object)val2)["name"] == DBNull.Value) ? string.Empty : (Convert.ToString(((DbDataReader)(object)val2)["name"]) ?? string.Empty), columnName, StringComparison.OrdinalIgnoreCase))
					{
						return;
					}
				}
				SqliteCommand val3 = connection.CreateCommand();
				try
				{
					((DbCommand)(object)val3).CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
					((DbCommand)(object)val3).ExecuteNonQuery();
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
			((IDisposable)val)?.Dispose();
		}
	}

	private static void EnsureOfflineIndex(SqliteConnection connection, string sql)
	{
		SqliteCommand val = connection.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = sql;
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static object ToDbValue(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return DBNull.Value;
	}

	private static string? Serialize(object? value)
	{
		if (value == null)
		{
			return null;
		}
		return JsonSerializer.Serialize(value, JsonOptions);
	}
}
