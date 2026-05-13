using System.Windows;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views;

/// <summary>
/// Change password dialog. Shown when user must change password (after admin reset)
/// or when user voluntarily changes their password from settings.
/// </summary>
public partial class ChangePasswordWindow : Window
{
    /// <summary>
    /// If true, the user cannot cancel (forced password change after reset).
    /// </summary>
    public bool IsForced { get; set; }

    public bool PasswordChanged { get; private set; }

    public ChangePasswordWindow(bool isForced = false)
    {
        InitializeComponent();
        IsForced = isForced;

        if (IsForced)
        {
            subtitleLabel.Text = "Your password was reset by an administrator. You must set a new password to continue.";
            cancelButton.Visibility = Visibility.Collapsed;
        }
    }

    private void ChangeButton_Click(object sender, RoutedEventArgs e)
    {
        string current = currentPasswordBox.Password;
        string newPass = newPasswordBox.Password;
        string confirm = confirmPasswordBox.Password;

        // Validation
        if (string.IsNullOrWhiteSpace(current))
        {
            ShowError("Please enter your current password.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6)
        {
            ShowError("New password must be at least 6 characters.");
            return;
        }

        if (newPass != confirm)
        {
            ShowError("New password and confirmation do not match.");
            return;
        }

        if (newPass == current)
        {
            ShowError("New password must be different from the current password.");
            return;
        }

        // Attempt change
        var result = PasswordResetService.ChangePassword(UserSession.UserId, current, newPass);

        if (result.IsSuccess)
        {
            PasswordChanged = true;
            UserSession.MustChangePassword = false;
            PasswordResetService.ClearMustChangePassword(UserSession.UserId);

            successLabel.Text = "Password changed successfully. You may now continue.";
            successLabel.Visibility = Visibility.Visible;
            errorLabel.Visibility = Visibility.Collapsed;

            // Close after brief delay
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                DialogResult = true;
                Close();
            };
            timer.Start();
        }
        else
        {
            ShowError(result.Message);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsForced)
        {
            DialogResult = false;
            Close();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Prevent closing if forced and password not changed
        if (IsForced && !PasswordChanged)
        {
            e.Cancel = true;
            ShowError("You must change your password before continuing.");
        }
        base.OnClosing(e);
    }

    private void ShowError(string message)
    {
        errorLabel.Text = message;
        errorLabel.Visibility = Visibility.Visible;
        successLabel.Visibility = Visibility.Collapsed;
    }
}
