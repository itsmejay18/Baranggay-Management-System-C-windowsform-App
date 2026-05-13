using System;
using System.Windows;
using baranggaysystem1.Views;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;

namespace baranggaysystem1.helper;

/// <summary>
/// Integration helper for all UX enhancements.
/// Call Initialize() from MainWindow.Loaded to wire up:
///   - Keyboard shortcuts (Ctrl+K, Ctrl+N, Ctrl+L, F1, F5, etc.)
///   - Command palette (Ctrl+Shift+P)
///   - Toast notifications
///   - Navigation history (back/forward)
///   - Session timeout warnings via toast
///
/// Usage in MainWindow.xaml.cs constructor (after InitializeComponent):
///   UxEnhancementsIntegration.Initialize(this);
/// </summary>
internal static class UxEnhancementsIntegration
{
    private static MainWindow? _mainWindow;
    private static NavigationHistory? _navHistory;
    private static bool _initialized;

    /// <summary>
    /// Initialize all UX enhancements on the main window.
    /// </summary>
    public static void Initialize(MainWindow mainWindow)
    {
        if (_initialized || mainWindow == null) return;
        _initialized = true;
        _mainWindow = mainWindow;
        _navHistory = new NavigationHistory();

        // Register keyboard shortcuts
        KeyboardShortcutManager.Initialize(mainWindow);
        KeyboardShortcutManager.DialogRequested += OnDialogRequested;
        KeyboardShortcutManager.NavigateRequested += OnNavigateRequested;
        KeyboardShortcutManager.RefreshRequested += OnRefreshRequested;

        // Wire session timeout warnings to toast
        SessionTimeoutManager.Instance.InactivityWarning += OnInactivityWarning;

        AppLogger.LogInfo("UX enhancements initialized (shortcuts, command palette, toast, nav history).");
    }

    /// <summary>
    /// Record a page navigation for history tracking.
    /// Call this from MainWindow.NavigatePage() after successful navigation.
    /// </summary>
    public static void RecordNavigation(string route, string title)
    {
        _navHistory?.Push(route, title);
    }

    /// <summary>
    /// Navigate back in history.
    /// </summary>
    public static void GoBack()
    {
        string? route = _navHistory?.GoBack();
        if (route != null)
        {
            _mainWindow?.NavigatePage(route);
        }
    }

    /// <summary>
    /// Navigate forward in history.
    /// </summary>
    public static void GoForward()
    {
        string? route = _navHistory?.GoForward();
        if (route != null)
        {
            _mainWindow?.NavigatePage(route);
        }
    }

    /// <summary>
    /// Whether back navigation is available.
    /// </summary>
    public static bool CanGoBack => _navHistory?.CanGoBack ?? false;

    /// <summary>
    /// Whether forward navigation is available.
    /// </summary>
    public static bool CanGoForward => _navHistory?.CanGoForward ?? false;

    private static void OnDialogRequested(string dialogId)
    {
        if (_mainWindow == null) return;

        try
        {
            switch (dialogId)
            {
                case "GlobalSearch":
                    new GlobalSearchWindow { Owner = _mainWindow }.ShowDialog();
                    break;

                case "EllieAssistant":
                    new EllieAssistantWindow { Owner = _mainWindow }.ShowDialog();
                    break;

                case "NewResident":
                    if (Permissions.Has(PermissionKeys.CreateResidents))
                    {
                        new ResidentDetailsWindow { Owner = _mainWindow }.ShowDialog();
                    }
                    else
                    {
                        ToastService.Warning("Access Denied", "You don't have permission to create residents.");
                    }
                    break;

                case "NewCertificate":
                    if (Permissions.Has(PermissionKeys.RequestCertificates))
                    {
                        new CertificationWindow { Owner = _mainWindow }.ShowDialog();
                    }
                    else
                    {
                        ToastService.Warning("Access Denied", "You don't have permission to create certificate requests.");
                    }
                    break;

                case "BulkImport":
                    SessionSecurityIntegration.OpenBulkImport(_mainWindow);
                    break;

                case "Export":
                    ToastService.Info("Export", "Navigate to a module page and use the export button.");
                    break;

                case "LockSession":
                    SessionSecurityIntegration.LockSession();
                    break;

                case "ChangePassword":
                    SessionSecurityIntegration.OpenChangePassword(_mainWindow);
                    break;

                case "NotificationSettings":
                    SessionSecurityIntegration.OpenNotificationSettings(_mainWindow);
                    break;

                case "SecurityQuestions":
                    SessionSecurityIntegration.OpenSecurityQuestions(_mainWindow);
                    break;

                case "CommandPalette":
                    ShowCommandPalette();
                    break;

                case "Refresh":
                    OnRefreshRequested();
                    break;
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Error handling dialog request: {dialogId}", ex);
            ToastService.Error("Error", ex.Message);
        }
    }

    private static void OnNavigateRequested(string route)
    {
        _mainWindow?.NavigatePage(route);
    }

    private static void OnRefreshRequested()
    {
        // Re-navigate to current page to refresh it
        if (_navHistory?.Current != null)
        {
            string currentRoute = _navHistory.Current.Route;
            // Force re-creation by navigating away and back
            _mainWindow?.NavigatePage(currentRoute);
            ToastService.Info("Refreshed", "Page reloaded.");
        }
    }

    private static void OnInactivityWarning(int remainingSeconds)
    {
        string message = remainingSeconds > 60
            ? $"Session will lock in {remainingSeconds / 60} minute(s)."
            : $"Session will lock in {remainingSeconds} seconds.";

        ToastService.Warning("Inactivity Warning", message);
    }

    private static void ShowCommandPalette()
    {
        if (_mainWindow == null) return;

        var palette = new CommandPaletteWindow
        {
            Owner = _mainWindow
        };

        bool? result = palette.ShowDialog();

        if (result == true && !string.IsNullOrEmpty(palette.SelectedAction))
        {
            switch (palette.SelectedType)
            {
                case CommandPaletteActionType.Navigate:
                    _mainWindow.NavigatePage(palette.SelectedAction);
                    break;

                case CommandPaletteActionType.Dialog:
                    OnDialogRequested(palette.SelectedAction);
                    break;
            }
        }
    }

    /// <summary>
    /// Clean up on application exit.
    /// </summary>
    public static void Shutdown()
    {
        KeyboardShortcutManager.DialogRequested -= OnDialogRequested;
        KeyboardShortcutManager.NavigateRequested -= OnNavigateRequested;
        KeyboardShortcutManager.RefreshRequested -= OnRefreshRequested;
        SessionTimeoutManager.Instance.InactivityWarning -= OnInactivityWarning;
        _initialized = false;
    }
}
