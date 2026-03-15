using System;
using System.Collections.Generic;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Services;

internal static class SyncService
{
    private static readonly object SyncRoot = new();
    private static bool _syncInProgress;

    public static void TrySynchronizePendingChanges()
    {
        lock (SyncRoot)
        {
            if (_syncInProgress)
            {
                return;
            }

            _syncInProgress = true;
        }

        try
        {
            if (!DatabaseManager.RefreshConnectivity(forceRaiseEvent: false))
            {
                return;
            }

            IReadOnlyList<SyncQueueItem> queue = DatabaseManager.LoadPendingSyncQueue();
            if (queue.Count == 0)
            {
                DatabaseManager.RefreshLocalTrackedTables();
                return;
            }

            var successfulQueueIds = new List<long>();
            using var conn = DBConnection.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            foreach (SyncQueueItem item in queue)
            {
                try
                {
                    using var cmd = new MySqlCommand(item.SqlText, conn, tx);
                    DbParameterMapper.Apply(cmd, item.Parameters);
                    cmd.ExecuteNonQuery();
                    successfulQueueIds.Add(item.QueueId);
                }
                catch (MySqlException ex) when (ex.Number == 1062)
                {
                    AppLogger.LogWarning($"Duplicate detected while syncing '{item.TableName}'. Keeping remote row and clearing the pending item.", ex);
                    successfulQueueIds.Add(item.QueueId);
                }
                catch (Exception ex)
                {
                    DatabaseManager.UpdatePendingSyncFailure(item.QueueId, ex.Message);
                    AppLogger.LogWarning($"Sync failed for queue item {item.QueueId} on table '{item.TableName}'.", ex);
                    throw;
                }
            }

            tx.Commit();
            DatabaseManager.DeletePendingSyncQueue(successfulQueueIds);
            DatabaseManager.RefreshLocalTrackedTables();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Offline sync cycle failed.", ex);
        }
        finally
        {
            lock (SyncRoot)
            {
                _syncInProgress = false;
            }
        }
    }
}