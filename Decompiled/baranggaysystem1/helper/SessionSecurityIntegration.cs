using System;
using System.Windows;
using baranggaysystem1.Services;
using baranggaysystem1.Views;

namespace baranggaysystem1.helper;

/// <summary>
/// Integrates session security features (timeout, lock screen, forced password change)
/// with the main application lifecycle. Call methods from MainWindow or App.xaml.cs.
/// </summary>
internal static class SessionSecurityIntegration
{
    private static bool _initialized;

    /// <summary>
    /// Initialize session security after successful login.
    /// Call this from the main window's Loaded event or after login completes.
    /// </summary>
    public static void OnLoginSuccess()
    {
        if (_initialized) return;
        _initialized = true;

        // Load and start session timeout
        var timeout = SessionTimeoutManager.Instance;
        timeout.LoadSettings();
        timeout.SessionLocked += OnSessionLocked;
        timeout.InactivityWarning += OnInactivityWarning;
        timeout.SessionUnlocked += OnSessionUnlocked;
        timeout.Start();

        // Start sync status monitoring
        OfflineSyncStatusService.Instance.Start();

        // Check if user must change password
        if (PasswordResetService.MustChangePassword(UserSession.UserId))
        {
            UserSession.MustChangePassword = true;
            ShowForceChangePassword();
        }

        AppLogger.LogInfo($"Session security initialized for user '{UserSession.Username}'.");
    }

    /// <summary>
    /// Clean up on logout.
    /// </summary>
    public static void OnLogout()
    {
        SessionTimeoutManager.Instance.Stop();
        OfflineSyncStatusService.Instance.Stop();
        UserSession.Clear();
        _initialized = false;
    }

    /// <summary>
    /// Show the lock screen (called when session times out).
    /// </summary>
    private static void OnSessionLocked()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            UserSession.IsSessionLocked = true;

            var lockScreen = new LockScreenWindow
            {
                Owner = Application.Current.MainWindow
            };

            bool? result = lockScreen.ShowDialog();

            if (result != true || !lockScreen.WasUnlocked)
            {
                // User chose to log out
                OnLogout();
                // Navigate back to login
                NavigateToLogin();
            }
        });
    }

    /// <summary>
    /// Show inactivity warning before lock.
    /// </summary>
    private static void OnInactivityWarning(int remainingSeconds)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            // Show a non-blocking notification
            string message = remainingSeconds > 60
                ? $"Session will lock in {remainingSeconds / 60} minute(s) due to inactivity."
                : $"Session will lock in {remainingSeconds} seconds due to inactivity.";

            // Use a toast-style notification if available, otherwise log
            AppLogger.LogInfo(message);

            // You can integrate with a toast notification system here
            // For now, we'll show it in the status bar or as a brief message
        });
    }

    /// <summary>
    /// Called when session is successfully unlocked.
    /// </summary>
    private static void OnSessionUnlocked()
    {
        UserSession.IsSessionLocked = false;
        AppLogger.LogInfo("Session unlocked successfully.");
    }

    /// <summary>
    /// Show forced password change dialog.
    /// </summary>
    private static void ShowForceChangePassword()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var changeWindow = new ChangePasswordWindow(isForced: true)
            {
                Owner = Application.Current.MainWindow
            };
            changeWindow.ShowDialog();
        });
    }

    /// <summary>
    /// Navigate back to login screen (after logout from lock screen).
    /// </summary>
    private static void NavigateToLogin()
    {
        // Close main window and show login
        if (Application.Current?.MainWindow != null)
        {
            var loginWindow = new Window(); // Replace with actual LoginWindow type
            Application.Current.MainWindow.Close();
            // The login window should be shown by the application startup logic
        }
    }

    /// <summary>
    /// Manually lock the session (e.g., from a menu item or keyboard shortcut).
    /// </summary>
    public static void LockSession()
    {
        SessionTimeoutManager.Instance.LockNow();
    }

    /// <summary>
    /// Open the notification settings window.
    /// </summary>
    public static void OpenNotificationSettings(Window? owner = null)
    {
        if (!Permissions.Has(PermissionKeys.OpenSettings))
        {
            MessageBox.Show("You do not have permission to access settings.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = new NotificationSettingsWindow();
        if (owner != null) window.Owner = owner;
        window.ShowDialog();
    }

    /// <summary>
    /// Open the bulk import window.
    /// </summary>
    public static void OpenBulkImport(Window? owner = null)
    {
        if (!Permissions.Has(PermissionKeys.CreateResidents))
        {
            MessageBox.Show("You do not have permission to import residents.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = new BulkImportWindow();
        if (owner != null) window.Owner = owner;
        window.ShowDialog();
    }

    /// <summary>
    /// Open the security questions setup window.
    /// </summary>
    public static void OpenSecurityQuestions(Window? owner = null)
    {
        var window = new SecurityQuestionsWindow();
        if (owner != null) window.Owner = owner;
        window.ShowDialog();
    }

    /// <summary>
    /// Open the change password window (voluntary).
    /// </summary>
    public static void OpenChangePassword(Window? owner = null)
    {
        var window = new ChangePasswordWindow(isForced: false);
        if (owner != null) window.Owner = owner;
        window.ShowDialog();
    }

    /// <summary>
    /// Open the password reset window (from login screen).
    /// </summary>
    public static void OpenPasswordReset(Window? owner = null)
    {
        var window = new PasswordResetWindow();
        if (owner != null) window.Owner = owner;
        window.ShowDialog();
    }
}
