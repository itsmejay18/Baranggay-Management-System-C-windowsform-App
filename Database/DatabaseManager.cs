using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Database;

internal enum DatabaseMode
{
    Offline = 0,
    Online = 1
}

internal enum DatabaseOperationKind
{
    Select = 0,
    Insert = 1,
    Update = 2,
    Delete = 3,
    Other = 4
}

internal sealed record DatabaseOperationInfo(DatabaseOperationKind Kind, string? TableName)
{
    public bool IsWrite => Kind == DatabaseOperationKind.Insert
        || Kind == DatabaseOperationKind.Update
        || Kind == DatabaseOperationKind.Delete;
}

internal sealed class DatabaseModeChangedEventArgs : EventArgs
{
    public DatabaseModeChangedEventArgs(DatabaseMode previousMode, DatabaseMode currentMode)
    {
        PreviousMode = previousMode;
        CurrentMode = currentMode;
    }

    public DatabaseMode PreviousMode { get; }
    public DatabaseMode CurrentMode { get; }
}

internal static class DatabaseManager
{
    private static readonly object SyncRoot = new();
    private static readonly RemoteDatabaseService RemoteService = new();
    private static readonly LocalDatabaseService LocalService = new();

    // simple in-memory cache for query results. key = sql + serialized params
    private static readonly Dictionary<string, DataTable> QueryCache = new(StringComparer.Ordinal);

    // DEBUG: Override mode for offline/online testing
    private static DatabaseMode? _debugModeOverride = null;

    private static string ComputeCacheKey(string sql, IEnumerable<DbParameterValue>? parameters)
    {
        string paramJson = DbParameterMapper.Serialize(parameters);
        return sql + "|" + paramJson;
    }

    public static bool TryGetCachedTable(string sql, IEnumerable<DbParameterValue>? parameters, out DataTable table)
    {
        string key = ComputeCacheKey(sql, parameters);
        lock (SyncRoot)
        {
            if (QueryCache.TryGetValue(key, out DataTable cached))
            {
                table = cached.Copy(); // return copy to avoid accidental modification
                return true;
            }
        }
        table = null!;
        return false;
    }

    public static DataTable SelectCached(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        string key = ComputeCacheKey(sql, parameters);
        lock (SyncRoot)
        {
            if (QueryCache.TryGetValue(key, out DataTable cached))
            {
                return cached.Copy();
            }
        }

        DataTable result = Select(sql, parameters); // normal execution
        lock (SyncRoot)
        {
            QueryCache[key] = result.Copy();
        }
        return result;
    }

    public static void PreloadCache(params string[] sqlStatements)
    {
        foreach (string sql in sqlStatements)
        {
            try
            {
                SelectCached(sql, null);
            }
            catch
            {
                // ignore failures during preload
            }
        }
    }

    public static void ClearCache()
    {
        lock (SyncRoot)
        {
            QueryCache.Clear();
        }
    }

    public static event EventHandler<DatabaseModeChangedEventArgs>? ModeChanged;

    public static DatabaseMode CurrentMode { get; private set; } = DatabaseMode.Offline;
    public static bool IsOnline => CurrentMode == DatabaseMode.Online;
    public static string LocalDatabasePath => LocalService.DatabasePath;

    public static void Initialize()
    {
        LocalService.EnsureDatabaseInitialized();
        RefreshConnectivity(forceRaiseEvent: false);

        if (IsOnline)
        {
            SyncService.TrySynchronizePendingChanges();
        }
    }

    public static bool RefreshConnectivity(bool forceRaiseEvent = true)
    {
        DatabaseMode previousMode;
        DatabaseMode nextMode;

        lock (SyncRoot)
        {
            // If debug mode is active, skip normal connectivity check
            if (_debugModeOverride.HasValue)
            {
                return _debugModeOverride.Value == DatabaseMode.Online;
            }

            previousMode = CurrentMode;
            bool hasNetwork = NetworkInterface.GetIsNetworkAvailable();
            bool remoteAvailable = hasNetwork && RemoteService.CanConnect();
            nextMode = remoteAvailable ? DatabaseMode.Online : DatabaseMode.Offline;
            CurrentMode = nextMode;
        }

        if (forceRaiseEvent && previousMode != nextMode)
        {
            AppLogger.LogInfo($"Database mode changed: {previousMode} -> {nextMode}.");
            ModeChanged?.Invoke(null, new DatabaseModeChangedEventArgs(previousMode, nextMode));
        }

        return nextMode == DatabaseMode.Online;
    }

    /// <summary>
    /// DEBUG ONLY: Force database mode to online or offline for testing purposes.
    /// </summary>
    public static void SetDebugMode(bool forceOnline)
    {
        lock (SyncRoot)
        {
            DatabaseMode newMode = forceOnline ? DatabaseMode.Online : DatabaseMode.Offline;
            DatabaseMode previousMode = CurrentMode;

            _debugModeOverride = newMode;
            CurrentMode = newMode;

            if (previousMode != newMode)
            {
                AppLogger.LogInfo($"[DEBUG] Database mode forced to {newMode}");
                ModeChanged?.Invoke(null, new DatabaseModeChangedEventArgs(previousMode, newMode));
            }
        }
    }

    /// <summary>
    /// DEBUG ONLY: Clear debug mode and resume normal connectivity detection.
    /// </summary>
    public static void ClearDebugMode()
    {
        lock (SyncRoot)
        {
            if (_debugModeOverride.HasValue)
            {
                AppLogger.LogInfo("[DEBUG] Debug mode cleared, resuming normal connectivity detection");
                _debugModeOverride = null;
            }
        }

        // Refresh to normal connectivity check
        RefreshConnectivity(forceRaiseEvent: true);
    }

    public static DataTable Select(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        if (IsOnline)
        {
            try
            {
                return RemoteService.LoadTable(sql, parameters);
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning("Remote SELECT failed. Falling back to local offline cache.", ex);
                RefreshConnectivity();
            }
        }

        return LocalService.LoadTable(sql, parameters);
    }

    public static int Insert(string sql, IEnumerable<DbParameterValue>? parameters = null)
        => ExecuteWrite(sql, parameters, new DatabaseOperationInfo(DatabaseOperationKind.Insert, SqlOperationParser.GetTableName(sql)));

    public static int Update(string sql, IEnumerable<DbParameterValue>? parameters = null)
        => ExecuteWrite(sql, parameters, new DatabaseOperationInfo(DatabaseOperationKind.Update, SqlOperationParser.GetTableName(sql)));

    public static int Delete(string sql, IEnumerable<DbParameterValue>? parameters = null)
        => ExecuteWrite(sql, parameters, new DatabaseOperationInfo(DatabaseOperationKind.Delete, SqlOperationParser.GetTableName(sql)));

    public static int Execute(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        DatabaseOperationInfo info = SqlOperationParser.Parse(sql);
        return info.Kind switch
        {
            DatabaseOperationKind.Insert => Insert(sql, parameters),
            DatabaseOperationKind.Update => Update(sql, parameters),
            DatabaseOperationKind.Delete => Delete(sql, parameters),
            _ => ExecuteNonQuery(sql, parameters)
        };
    }

    public static int ExecuteNonQuery(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        DatabaseOperationInfo info = SqlOperationParser.Parse(sql);
        if (info.IsWrite)
        {
            return ExecuteWrite(sql, parameters, info);
        }

        if (IsOnline)
        {
            try
            {
                return RemoteService.ExecuteNonQuery(sql, parameters);
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning("Remote non-query failed. Falling back to local offline cache.", ex);
                RefreshConnectivity();
            }
        }

        return LocalService.ExecuteNonQuery(sql, parameters, info, queueForSync: false, syncStatus: null);
    }

    public static T? ExecuteScalar<T>(string sql, IEnumerable<DbParameterValue>? parameters = null)
    {
        if (IsOnline)
        {
            try
            {
                return RemoteService.ExecuteScalar<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning("Remote scalar query failed. Falling back to local offline cache.", ex);
                RefreshConnectivity();
            }
        }

        return LocalService.ExecuteScalar<T>(sql, parameters);
    }

    internal static IReadOnlyList<SyncQueueItem> LoadPendingSyncQueue()
        => LocalService.LoadPendingQueue();

    internal static void DeletePendingSyncQueue(IReadOnlyCollection<long> queueIds)
        => LocalService.DeletePendingQueue(queueIds);

    internal static void UpdatePendingSyncFailure(long queueId, string error)
        => LocalService.RecordSyncFailure(queueId, error);

    internal static void RefreshLocalTrackedTables()
    {
        if (!IsOnline)
        {
            return;
        }

        foreach (string tableName in OfflineTrackedTableCatalog.All)
        {
            try
            {
                DataTable snapshot = RemoteService.LoadTable($"SELECT * FROM {tableName}");
                LocalService.ReplaceTrackedTable(tableName, snapshot);
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Failed to refresh local cache for table '{tableName}'.", ex);
            }
        }
    }

    private static int ExecuteWrite(string sql, IEnumerable<DbParameterValue>? parameters, DatabaseOperationInfo info)
    {
        // any write operation could change data returned by earlier selects; clear cache to avoid stale reads
        ClearCache();

        if (IsOnline)
        {
            try
            {
                int affected = RemoteService.ExecuteNonQuery(sql, parameters);
                LocalService.ExecuteNonQuery(sql, parameters, info, queueForSync: false, syncStatus: OfflineSyncStatus.Synced);
                return affected;
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning("Remote write failed. Queuing the change locally.", ex);
                RefreshConnectivity();
            }
        }

        return LocalService.ExecuteNonQuery(sql, parameters, info, queueForSync: true, syncStatus: OfflineSyncStatus.Pending);
    }
}

internal static class SqlOperationParser
{
    private static readonly Regex InsertRegex = new(@"^\s*INSERT\s+INTO\s+[`\[]?(?<table>[A-Za-z0-9_]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UpdateRegex = new(@"^\s*UPDATE\s+[`\[]?(?<table>[A-Za-z0-9_]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeleteRegex = new(@"^\s*DELETE\s+FROM\s+[`\[]?(?<table>[A-Za-z0-9_]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SelectRegex = new(@"^\s*SELECT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static DatabaseOperationInfo Parse(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new DatabaseOperationInfo(DatabaseOperationKind.Other, null);
        }

        Match insert = InsertRegex.Match(sql);
        if (insert.Success)
        {
            return new DatabaseOperationInfo(DatabaseOperationKind.Insert, insert.Groups["table"].Value);
        }

        Match update = UpdateRegex.Match(sql);
        if (update.Success)
        {
            return new DatabaseOperationInfo(DatabaseOperationKind.Update, update.Groups["table"].Value);
        }

        Match delete = DeleteRegex.Match(sql);
        if (delete.Success)
        {
            return new DatabaseOperationInfo(DatabaseOperationKind.Delete, delete.Groups["table"].Value);
        }

        return SelectRegex.IsMatch(sql)
            ? new DatabaseOperationInfo(DatabaseOperationKind.Select, null)
            : new DatabaseOperationInfo(DatabaseOperationKind.Other, null);
    }

    public static string? GetTableName(string sql)
        => Parse(sql).TableName;
}