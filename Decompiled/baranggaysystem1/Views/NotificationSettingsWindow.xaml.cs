using System;
using System.Windows;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views;

/// <summary>
/// UI for configuring notification settings (SMTP, SMS, reminders).
/// Replaces the need for environment variable configuration.
/// </summary>
public partial class NotificationSettingsWindow : Window
{
    public NotificationSettingsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        try
        {
            // Load SMTP
            var smtp = NotificationSettingsService.LoadSmtpSettings();
            smtpHostBox.Text = smtp.Host;
            smtpPortBox.Text = smtp.Port.ToString();
            smtpFromBox.Text = smtp.FromEmail;
            smtpFromNameBox.Text = smtp.FromName;
            smtpUserBox.Text = smtp.Username;
            smtpPassBox.Password = smtp.Password;
            smtpSslCheck.IsChecked = smtp.UseSsl;
            smtpEnabledCheck.IsChecked = smtp.IsEnabled;

            // Load SMS
            var sms = NotificationSettingsService.LoadSmsSettings();
            smsApiUrlBox.Text = sms.ApiUrl;
            smsTokenBox.Password = sms.ApiToken;
            smsSenderBox.Text = sms.SenderName;
            smsEnabledCheck.IsChecked = sms.IsEnabled;

            // Load Reminders
            var reminders = NotificationSettingsService.LoadReminderSettings();
            renewalReminderCheck.IsChecked = reminders.RenewalReminderEnabled;
            renewalDaysBox.Text = reminders.RenewalReminderDays.ToString();
            blotterReminderCheck.IsChecked = reminders.BlotterReminderEnabled;
            blotterAgeDaysBox.Text = reminders.BlotterReminderAgeDays.ToString();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error loading settings: {ex.Message}", isError: true);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Save SMTP
            var smtp = new SmtpSettings
            {
                Host = smtpHostBox.Text?.Trim() ?? "",
                Port = int.TryParse(smtpPortBox.Text, out int port) ? port : 587,
                FromEmail = smtpFromBox.Text?.Trim() ?? "",
                FromName = smtpFromNameBox.Text?.Trim() ?? "Barangay System",
                Username = smtpUserBox.Text?.Trim() ?? "",
                Password = smtpPassBox.Password ?? "",
                UseSsl = smtpSslCheck.IsChecked == true,
                IsEnabled = smtpEnabledCheck.IsChecked == true
            };
            NotificationSettingsService.SaveSmtpSettings(smtp);

            // Save SMS
            var sms = new SmsSettings
            {
                ApiUrl = smsApiUrlBox.Text?.Trim() ?? "",
                ApiToken = smsTokenBox.Password ?? "",
                SenderName = smsSenderBox.Text?.Trim() ?? "Barangay",
                IsEnabled = smsEnabledCheck.IsChecked == true
            };
            NotificationSettingsService.SaveSmsSettings(sms);

            // Save Reminders
            var reminders = new ReminderSettings
            {
                RenewalReminderEnabled = renewalReminderCheck.IsChecked == true,
                RenewalReminderDays = int.TryParse(renewalDaysBox.Text, out int rd) ? rd : 30,
                BlotterReminderEnabled = blotterReminderCheck.IsChecked == true,
                BlotterReminderAgeDays = int.TryParse(blotterAgeDaysBox.Text, out int bd) ? bd : 12
            };
            NotificationSettingsService.SaveReminderSettings(reminders);

            ShowStatus("Settings saved successfully.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error saving settings: {ex.Message}", isError: true);
        }
    }

    private void TestSmtp_Click(object sender, RoutedEventArgs e)
    {
        string testEmail = smtpTestEmailBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(testEmail))
        {
            ShowStatus("Enter a test email address first.", isError: true);
            return;
        }

        var smtp = new SmtpSettings
        {
            Host = smtpHostBox.Text?.Trim() ?? "",
            Port = int.TryParse(smtpPortBox.Text, out int port) ? port : 587,
            FromEmail = smtpFromBox.Text?.Trim() ?? "",
            FromName = smtpFromNameBox.Text?.Trim() ?? "Barangay System",
            Username = smtpUserBox.Text?.Trim() ?? "",
            Password = smtpPassBox.Password ?? "",
            UseSsl = smtpSslCheck.IsChecked == true
        };

        var result = NotificationSettingsService.TestSmtp(smtp, testEmail);
        ShowStatus(result.Message, isError: !result.IsSuccess);
    }

    private void TestSms_Click(object sender, RoutedEventArgs e)
    {
        string testNumber = smsTestNumberBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(testNumber))
        {
            ShowStatus("Enter a test phone number first.", isError: true);
            return;
        }

        var sms = new SmsSettings
        {
            ApiUrl = smsApiUrlBox.Text?.Trim() ?? "",
            ApiToken = smsTokenBox.Password ?? "",
            SenderName = smsSenderBox.Text?.Trim() ?? "Barangay"
        };

        var result = NotificationSettingsService.TestSms(sms, testNumber);
        ShowStatus(result.Message, isError: !result.IsSuccess);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShowStatus(string message, bool isError)
    {
        statusLabel.Text = message;
        statusLabel.Foreground = isError
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
        statusLabel.Visibility = Visibility.Visible;
    }
}
