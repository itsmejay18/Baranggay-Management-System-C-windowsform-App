using System;
using System.Windows;
using System.Windows.Input;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views;
using baranggaysystem1.Views.Controls;

namespace baranggaysystem1.helper;

/// <summary>
/// Integrates session security features (timeout, lock screen, forced password change)
/// with the main application lifecycle. Call methods from MainWindow or App.xaml.cs.
/// </summary>
internal static class SessionSecurityIntegration
{
    private static bool _initialized;

    /// <summary>
    /// Stores the focused element before session lock so it can be restored after unlock.
    /// Used to restore focus to the previously active field in fullscreen views.
    /// Requirement 10.1: Restore focus to previously active field after re-authentication.
    /// </summary>
    private static IInputElement? _focusedElementBeforeLock;

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
    /// Retains all unsaved form field values during session lock by keeping the
    /// fullscreen view controls in the visual tree (hidden behind the modal lock screen).
    /// After successful re-authentication, restores focus to the previously active field.
    /// On logout, discards in-memory form data and navigates to login.
    /// 
    /// Requirements: 10.1, 10.2
    /// </summary>
    private static void OnSessionLocked()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            UserSession.IsSessionLocked = true;

            // Requirement 10.1: Save the currently focused element before showing the lock screen.
            // The form field values are retained in memory because the lock screen is a modal
            // overlay — the main window's visual tree (including any active fullscreen view
            // with form data) remains intact behind the lock screen.
            _focusedElementBeforeLock = Keyboard.FocusedElement;

            var lockScreen = new LockScreenWindow
            {
                Owner = Application.Current.MainWindow
            };

            bool? result = lockScreen.ShowDialog();

            if (result == true && lockScreen.WasUnlocked)
            {
                // Requirement 10.1: Restore focus to previously active field after
                // successful re-authentication via the LockScreenWindow.
                RestoreFocusAfterUnlock();
            }
            else
            {
                // Requirement 10.2: User chose to log out from the LockScreenWindow.
                // Discard in-memory form data and navigate to login without submitting
                // any pending changes.
                DiscardFullscreenFormData();
                OnLogout();
                NavigateToLogin();
            }
        });
    }

    /// <summary>
    /// Restores keyboard focus to the element that was focused before the session was locked.
    /// Requirement 10.1: Restore focus to previously active field after re-authentication.
    /// </summary>
    private static void RestoreFocusAfterUnlock()
    {
        if (_focusedElementBeforeLock == null)
        {
            return;
        }

        // Use Dispatcher.BeginInvoke to restore focus after the lock screen window
        // has fully closed and the main window has regained activation.
        Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            new Action(() =>
            {
                try
                {
                    // Verify the element is still in the visual tree and focusable
                    if (_focusedElementBeforeLock is UIElement uiElement
                        && uiElement.IsVisible
                        && uiElement.Focusable)
                    {
                        Keyboard.Focus(_focusedElementBeforeLock);
                    }
                    else if (_focusedElementBeforeLock is FrameworkElement fe
                             && fe.IsLoaded
                             && fe.Focusable)
                    {
                        Keyboard.Focus(_focusedElementBeforeLock);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogWarning("Failed to restore focus after session unlock.", ex);
                }
                finally
                {
                    _focusedElementBeforeLock = null;
                }
            }));
    }

    /// <summary>
    /// Discards in-memory form data from any active fullscreen view.
    /// Called when the user logs out from the LockScreenWindow.
    /// Requirement 10.2: Discard the in-memory form data and navigate to login
    /// without submitting any pending changes.
    /// </summary>
    private static void DiscardFullscreenFormData()
    {
        try
        {
            var nav = NavigationService.Instance;
            var currentView = nav.CurrentView;

            if (currentView is FullscreenViewHost host)
            {
                // If the content is a FullscreenFormBase, discard all in-memory form data
                // to prevent any accidental submission on logout.
                // Requirement 10.2: Discard in-memory form data without submitting pending changes.
                if (host.ContentArea is FullscreenFormBase form)
                {
                    form.DiscardFormData();
                }

                // Clear the host-level unsaved changes flag
                host.SetUnsavedChanges(false);
            }

            // Clear the page cache to release all in-memory page state
            nav.ClearCache();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Error discarding fullscreen form data on logout.", ex);
        }
        finally
        {
            _focusedElementBeforeLock = null;
        }
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
    /// Requirement 10.2: Navigate to the login screen without submitting any pending changes.
    /// </summary>
    private static void NavigateToLogin()
    {
        if (Application.Current?.MainWindow != null)
        {
            var mainWindow = Application.Current.MainWindow;
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Application.Current.MainWindow = loginWindow;
            mainWindow.Close();
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
