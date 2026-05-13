using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// A compact status indicator showing online/offline state and pending sync count.
/// Designed to be placed in the main window's status bar or header.
/// </summary>
public partial class SyncStatusIndicator : UserControl
{
    private readonly OfflineSyncStatusService _syncService;

    public SyncStatusIndicator()
    {
        InitializeComponent();
        _syncService = OfflineSyncStatusService.Instance;
        _syncService.PropertyChanged += OnSyncStatusChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateDisplay();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _syncService.PropertyChanged -= OnSyncStatusChanged;
    }

    private void OnSyncStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(UpdateDisplay);
    }

    private void UpdateDisplay()
    {
        switch (_syncService.State)
        {
            case ConnectionState.Online:
                statusIcon.Text = "✓";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                statusText.Text = _syncService.HasPendingChanges
                    ? $"Online ({_syncService.PendingChangesCount} pending)"
                    : "Connected";
                rootBorder.Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x44));
                break;

            case ConnectionState.Offline:
                statusIcon.Text = "⚠";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00));
                statusText.Text = "Offline";
                rootBorder.Background = new SolidColorBrush(Color.FromRgb(0x3d, 0x2d, 0x1a));
                break;

            case ConnectionState.Syncing:
                statusIcon.Text = "↻";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xdb));
                statusText.Text = $"Syncing... {_syncService.SyncProgressPercent}%";
                rootBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x2d, 0x3d));
                break;

            case ConnectionState.Error:
                statusIcon.Text = "✗";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
                statusText.Text = "Error";
                rootBorder.Background = new SolidColorBrush(Color.FromRgb(0x3d, 0x1a, 0x1a));
                break;
        }

        // Pending badge
        if (_syncService.HasPendingChanges)
        {
            pendingBadge.Visibility = Visibility.Visible;
            pendingCountText.Text = _syncService.PendingChangesCount.ToString();
        }
        else
        {
            pendingBadge.Visibility = Visibility.Collapsed;
        }

        // Tooltip
        string tooltip = _syncService.StatusMessage;
        if (_syncService.LastSyncTime.HasValue)
            tooltip += $"\n{_syncService.LastSyncDisplay}";
        if (_syncService.HasConflicts)
            tooltip += $"\n⚠ {_syncService.ConflictCount} conflict(s) need attention";
        ToolTip = tooltip;
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        // Show sync details popup or trigger sync
        if (_syncService.IsOffline || _syncService.HasPendingChanges)
        {
            var result = MessageBox.Show(
                $"{_syncService.StatusMessage}\n\n" +
                $"Pending changes: {_syncService.PendingChangesCount}\n" +
                $"{_syncService.LastSyncDisplay}\n\n" +
                "Would you like to attempt sync now?",
                "Sync Status",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                var syncResult = _syncService.TrySync();
                MessageBox.Show(syncResult.Message, "Sync Result",
                    MessageBoxButton.OK,
                    syncResult.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
        }
        else
        {
            MessageBox.Show(
                $"Status: {_syncService.StatusMessage}\n{_syncService.LastSyncDisplay}",
                "Connection Status",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
