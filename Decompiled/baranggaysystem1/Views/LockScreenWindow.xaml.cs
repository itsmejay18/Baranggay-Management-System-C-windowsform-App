using System;
using System.Windows;
using System.Windows.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views;

/// <summary>
/// Lock screen window displayed when the session times out due to inactivity.
/// Requires password re-entry to unlock.
/// </summary>
public partial class LockScreenWindow : Window
{
    private int _failedAttempts;
    private const int MaxFailedAttempts = 5;

    public bool WasUnlocked { get; private set; }

    public LockScreenWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Focus the password field
        if (passwordBox != null)
            passwordBox.Focus();

        // Display username
        if (usernameLabel != null)
            usernameLabel.Text = string.IsNullOrWhiteSpace(UserSession.Username)
                ? "User"
                : UserSession.Username;

        if (lockMessageLabel != null)
            lockMessageLabel.Text = "Session locked due to inactivity. Enter your password to continue.";
    }

    private void UnlockButton_Click(object sender, RoutedEventArgs e)
    {
        AttemptUnlock();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AttemptUnlock();
        }
    }

    private void AttemptUnlock()
    {
        string password = passwordBox?.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter your password.");
            return;
        }

        if (_failedAttempts >= MaxFailedAttempts)
        {
            ShowError("Too many failed attempts. Please contact an administrator.");
            return;
        }

        bool unlocked = SessionTimeoutManager.Instance.TryUnlock(password);

        if (unlocked)
        {
            WasUnlocked = true;
            UserSession.IsSessionLocked = false;
            DialogResult = true;
            Close();
        }
        else
        {
            _failedAttempts++;
            int remaining = MaxFailedAttempts - _failedAttempts;
            ShowError(remaining > 0
                ? $"Incorrect password. {remaining} attempt(s) remaining."
                : "Too many failed attempts. Please contact an administrator.");

            passwordBox?.Clear();
            passwordBox?.Focus();
        }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to log out? Any unsaved work will be lost.",
            "Confirm Logout",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            WasUnlocked = false;
            UserSession.Clear();
            SessionTimeoutManager.Instance.Stop();
            DialogResult = false;
            Close();
        }
    }

    private void ShowError(string message)
    {
        if (errorLabel != null)
        {
            errorLabel.Text = message;
            errorLabel.Visibility = Visibility.Visible;
        }
    }

    // Prevent closing the lock screen without authentication
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!WasUnlocked && DialogResult != false)
        {
            e.Cancel = true;
        }
        base.OnClosing(e);
    }
}
