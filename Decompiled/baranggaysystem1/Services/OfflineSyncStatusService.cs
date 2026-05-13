using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Threading;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Provides real-time offline sync status information for UI display.
/// Tracks pending changes, sync progress, and connection state.
/// Implements INotifyPropertyChanged for WPF data binding.
/// </summary>
public sealed class OfflineSyncStatusService : INotifyPropertyChanged, IDisposable
{
    private static OfflineSyncStatusService? _instance;
    private static readonly object InstanceLock = new object();

    private readonly DispatcherTimer _statusTimer;
    private bool _disposed;

    private ConnectionState _connectionState = ConnectionState.Online;
    private int _pendingChangesCount;
    private DateTime? _lastSyncTime;
    private string _statusMessage = "Connected";
    private bool _isSyncing;
    private int _syncProgress;
    private int _totalToSync;
    private int _conflictCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Current connection state.
    /// </summary>
    public ConnectionState State
    {
        get => _connectionState;
        private set { _connectionState = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsOnline)); OnPropertyChanged(nameof(IsOffline)); OnPropertyChanged(nameof(StatusIcon)); }
    }

    /// <summary>
    /// Number of changes pending sync to the server.
    /// </summary>
    public int PendingChangesCount
    {
        get => _pendingChangesCount;
        private set { _pendingChangesCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingChanges)); }
    }

    /// <summary>
    /// Last successful sync timestamp.
    /// </summary>
    public DateTime? LastSyncTime
    {
        get => _lastSyncTime;
        private set { _lastSyncTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastSyncDisplay)); }
    }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Whether a sync operation is currently in progress.
    /// </summary>
    public bool IsSyncing
    {
        get => _isSyncing;
        private set { _isSyncing = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Current sync progress (items synced so far).
    /// </summary>
    public int SyncProgress
    {
        get => _syncProgress;
        private set { _syncProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(SyncProgressPercent)); }
    }

    /// <summary>
    /// Total items to sync.
    /// </summary>
    public int TotalToSync
    {
        get => _totalToSync;
        private set { _totalToSync = value; OnPropertyChanged(); OnPropertyChanged(nameof(SyncProgressPercent)); }
    }

    /// <summary>
    /// Number of sync conflicts detected.
    /// </summary>
    public int ConflictCount
    {
        get => _conflictCount;
        private set { _conflictCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasConflicts)); }
    }

    // Computed properties for UI binding
    public bool IsOnline => State == ConnectionState.Online;
    public bool IsOffline => State == ConnectionState.Offline;
    public bool HasPendingChanges => PendingChangesCount > 0;
    public bool HasConflicts => ConflictCount > 0;
    public string StatusIcon => State switch
    {
        ConnectionState.Online => "✓",
        ConnectionState.Offline => "⚠",
        ConnectionState.Syncing => "↻",
        ConnectionState.Error => "✗",
        _ => "?"
    };

    public string LastSyncDisplay => LastSyncTime.HasValue
        ? $"Last sync: {FormatTimeAgo(LastSyncTime.Value)}"
        : "Never synced";

    public int SyncProgressPercent => TotalToSync > 0
        ? (int)((double)SyncProgress / TotalToSync * 100)
        : 0;

    public static OfflineSyncStatusService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (InstanceLock)
                {
                    _instance ??= new OfflineSyncStatusService();
                }
            }
            return _instance;
        }
    }

    private OfflineSyncStatusService()
    {
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _statusTimer.Tick += RefreshStatus;
    }

    /// <summary>
    /// Start monitoring sync status. Call after login.
    /// </summary>
    public void Start()
    {
        RefreshStatus(null, EventArgs.Empty);
        _statusTimer.Start();
    }

    /// <summary>
    /// Stop monitoring. Call on logout.
    /// </summary>
    public void Stop()
    {
        _statusTimer.Stop();
    }

    /// <summary>
    /// Force a status refresh.
    /// </summary>
    public void Refresh()
    {
        RefreshStatus(null, EventArgs.Empty);
    }

    /// <summary>
    /// Attempt to sync pending offline changes to the server.
    /// </summary>
    public SyncResult TrySync()
    {
        if (State == ConnectionState.Online && !HasPendingChanges)
            return new SyncResult { IsSuccess = true, Message = "Nothing to sync." };

        if (IsSyncing)
            return new SyncResult { IsSuccess = false, Message = "Sync already in progress." };

        IsSyncing = true;
        State = ConnectionState.Syncing;
        StatusMessage = "Syncing...";

        try
        {
            // Check if we can reach the server
            if (!TryConnectOnline())
            {
                State = ConnectionState.Offline;
                StatusMessage = "Cannot reach server. Changes saved locally.";
                return new SyncResult { IsSuccess = false, Message = "Server unreachable." };
            }

            int pending = GetPendingChangeCount();
            TotalToSync = pending;
            SyncProgress = 0;

            // Attempt to replay offline changes
            int synced = 0;
            int failed = 0;

            try
            {
                // Attempt to replay pending offline changes to the online database
                var conn = DBConnection.GetConnection();
                try
                {
                    ((System.Data.Common.DbConnection)(object)conn).Open();
                    DBConnection.RegisterConnectivitySuccess();

                    var offlineConn = OfflineDatabaseSupport.GetConnection();
                    try
                    {
                        using var readCmd = offlineConn.CreateCommand();
                        ((System.Data.Common.DbCommand)(object)readCmd).CommandText =
                            "SELECT sync_id, sql_statement FROM offline_sync_queue WHERE sync_status = 'pending' ORDER BY created_at ASC LIMIT @limit";
                        readCmd.Parameters.AddWithValue("@limit", pending);

                        using var reader = readCmd.ExecuteReader();
                        var items = new System.Collections.Generic.List<(long Id, string Sql)>();
                        while (((System.Data.Common.DbDataReader)(object)reader).Read())
                        {
                            items.Add((reader.GetInt64(0), reader.GetString(1)));
                        }
                        ((System.Data.Common.DbDataReader)(object)reader).Close();

                        TotalToSync = items.Count;
                        foreach (var (id, sql) in items)
                        {
                            try
                            {
                                var execCmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
                                try
                                {
                                    ((System.Data.Common.DbCommand)(object)execCmd).ExecuteNonQuery();
                                    synced++;
                                    SyncProgress = synced;

                                    // Mark as synced
                                    using var markCmd = offlineConn.CreateCommand();
                                    ((System.Data.Common.DbCommand)(object)markCmd).CommandText =
                                        "UPDATE offline_sync_queue SET sync_status = 'synced', synced_at = datetime('now') WHERE sync_id = @id";
                                    markCmd.Parameters.AddWithValue("@id", id);
                                    ((System.Data.Common.DbCommand)(object)markCmd).ExecuteNonQuery();
                                }
                                finally { ((IDisposable)execCmd)?.Dispose(); }
                            }
                            catch
                            {
                                failed++;
                            }
                        }
                    }
                    finally { ((IDisposable)offlineConn)?.Dispose(); }
                }
                finally { ((IDisposable)conn)?.Dispose(); }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning("Sync replay encountered errors.", ex);
                failed = pending - synced;
            }

            // Update state
            RefreshStatus(null, EventArgs.Empty);

            if (failed == 0)
            {
                State = ConnectionState.Online;
                LastSyncTime = DateTime.Now;
                StatusMessage = $"Sync complete. {synced} change(s) uploaded.";
                return new SyncResult { IsSuccess = true, SyncedCount = synced, Message = StatusMessage };
            }
            else
            {
                ConflictCount = failed;
                State = ConnectionState.Online;
                StatusMessage = $"Sync partial. {synced} synced, {failed} failed.";
                return new SyncResult
                {
                    IsSuccess = false,
                    SyncedCount = synced,
                    FailedCount = failed,
                    Message = StatusMessage
                };
            }
        }
        catch (Exception ex)
        {
            State = ConnectionState.Error;
            StatusMessage = $"Sync error: {ex.Message}";
            AppLogger.LogError("Sync failed.", ex);
            return new SyncResult { IsSuccess = false, Message = StatusMessage };
        }
        finally
        {
            IsSyncing = false;
        }
    }

    /// <summary>
    /// Get a summary of pending changes by module.
    /// </summary>
    public PendingChangesSummary GetPendingChangesSummary()
    {
        var summary = new PendingChangesSummary();

        try
        {
            if (!OfflineDatabaseSupport.IsAvailable) return summary;

            var conn = OfflineDatabaseSupport.GetConnection();
            try
            {
                using var cmd = conn.CreateCommand();
                ((System.Data.Common.DbCommand)(object)cmd).CommandText =
                    @"SELECT COALESCE(module, 'Unknown') as module, COUNT(*) as cnt 
                      FROM offline_sync_queue 
                      WHERE sync_status = 'pending' 
                      GROUP BY module";

                using var reader = cmd.ExecuteReader();
                while (((System.Data.Common.DbDataReader)(object)reader).Read())
                {
                    string module = reader.GetString(0);
                    int count = reader.GetInt32(1);
                    summary.ByModule[module] = count;
                    summary.Total += count;
                }
            }
            finally
            {
                ((IDisposable)conn)?.Dispose();
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to load pending changes summary.", ex);
        }

        return summary;
    }

    private void RefreshStatus(object? sender, EventArgs e)
    {
        try
        {
            bool isOffline = OfflineDatabaseSupport.IsOffline;
            int pending = GetPendingChangeCount();

            PendingChangesCount = pending;

            if (isOffline)
            {
                State = ConnectionState.Offline;
                StatusMessage = pending > 0
                    ? $"Offline — {pending} change(s) pending sync"
                    : "Offline — No pending changes";
            }
            else
            {
                if (pending > 0)
                {
                    State = ConnectionState.Online;
                    StatusMessage = $"Online — {pending} change(s) pending upload";
                }
                else
                {
                    State = ConnectionState.Online;
                    StatusMessage = "Connected";
                }
            }
        }
        catch (Exception ex)
        {
            State = ConnectionState.Error;
            StatusMessage = "Status check failed";
            AppLogger.LogWarning("Sync status refresh failed.", ex);
        }
    }

    private static int GetPendingChangeCount()
    {
        try
        {
            if (!OfflineDatabaseSupport.IsAvailable) return 0;

            var conn = OfflineDatabaseSupport.GetConnection();
            try
            {
                using var cmd = conn.CreateCommand();
                ((System.Data.Common.DbCommand)(object)cmd).CommandText =
                    "SELECT COUNT(*) FROM offline_sync_queue WHERE sync_status = 'pending'";
                object result = ((System.Data.Common.DbCommand)(object)cmd).ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            finally
            {
                ((IDisposable)conn)?.Dispose();
            }
        }
        catch
        {
            return 0;
        }
    }

    private static bool TryConnectOnline()
    {
        try
        {
            var conn = DBConnection.GetConnection();
            try
            {
                ((System.Data.Common.DbConnection)(object)conn).Open();
                ((IDisposable)conn)?.Dispose();
                DBConnection.RegisterConnectivitySuccess();
                return true;
            }
            catch
            {
                ((IDisposable)conn)?.Dispose();
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private static string FormatTimeAgo(DateTime time)
    {
        var diff = DateTime.Now - time;
        if (diff.TotalSeconds < 60) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return time.ToString("MMM dd, HH:mm");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _statusTimer.Stop();
    }
}

/// <summary>
/// Connection state enumeration.
/// </summary>
public enum ConnectionState
{
    Online,
    Offline,
    Syncing,
    Error
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public sealed class SyncResult
{
    public bool IsSuccess { get; set; }
    public int SyncedCount { get; set; }
    public int FailedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Summary of pending changes grouped by module.
/// </summary>
public sealed class PendingChangesSummary
{
    public int Total { get; set; }
    public System.Collections.Generic.Dictionary<string, int> ByModule { get; set; } = new();
}
