using System;
using System.Windows.Forms;
using baranggaysystem1.Database;

namespace baranggaysystem1.Services;

internal sealed class ConnectivityMonitorService : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;

    public ConnectivityMonitorService()
    {
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 30_000
        };
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void Dispose()
    {
        _timer.Tick -= Timer_Tick;
        _timer.Dispose();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        // Run connectivity check on background thread to avoid blocking the UI thread
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                bool wasOnline = DatabaseManager.IsOnline;
                bool isOnline = DatabaseManager.RefreshConnectivity();
                if (!wasOnline && isOnline)
                {
                    SyncService.TrySynchronizePendingChanges();
                    return;
                }

                if (isOnline)
                {
                    SyncService.TrySynchronizePendingChanges();
                }
            }
            catch
            {
                // Silently continue; connectivity checks are non-critical.
            }
        });
    }
}