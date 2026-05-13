using System;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Manages notification settings (SMTP, SMS) via the system_config table
/// instead of requiring environment variables. Provides a UI-friendly
/// interface for non-technical staff to configure notifications.
/// </summary>
public static class NotificationSettingsService
{
    // SMTP Configuration Keys
    public const string KeySmtpHost = "smtp_host";
    public const string KeySmtpPort = "smtp_port";
    public const string KeySmtpFromEmail = "smtp_from_email";
    public const string KeySmtpFromName = "smtp_from_name";
    public const string KeySmtpUsername = "smtp_username";
    public const string KeySmtpPassword = "smtp_password";
    public const string KeySmtpUseSsl = "smtp_use_ssl";
    public const string KeySmtpEnabled = "smtp_enabled";

    // SMS Configuration Keys
    public const string KeySmsApiUrl = "sms_api_url";
    public const string KeySmsApiToken = "sms_api_token";
    public const string KeySmsSenderName = "sms_sender_name";
    public const string KeySmsEnabled = "sms_enabled";

    // Reminder Configuration Keys
    public const string KeyRenewalReminderEnabled = "renewal_reminder_enabled";
    public const string KeyRenewalReminderDays = "renewal_reminder_days";
    public const string KeyBlotterReminderEnabled = "blotter_reminder_enabled";
    public const string KeyBlotterReminderAgeDays = "blotter_reminder_age_days";

    /// <summary>
    /// Load all SMTP settings from the database.
    /// Falls back to environment variables if DB values are empty (backward compatibility).
    /// </summary>
    public static SmtpSettings LoadSmtpSettings()
    {
        return new SmtpSettings
        {
            Host = GetWithEnvFallback(KeySmtpHost, "BARANGAY_SMTP_HOST"),
            Port = ParseInt(GetWithEnvFallback(KeySmtpPort, "BARANGAY_SMTP_PORT"), 587),
            FromEmail = GetWithEnvFallback(KeySmtpFromEmail, "BARANGAY_SMTP_FROM"),
            FromName = GetWithEnvFallback(KeySmtpFromName, "BARANGAY_SMTP_FROM_NAME", "Barangay System"),
            Username = GetWithEnvFallback(KeySmtpUsername, "BARANGAY_SMTP_USER"),
            Password = GetWithEnvFallback(KeySmtpPassword, "BARANGAY_SMTP_PASS"),
            UseSsl = ParseBool(GetWithEnvFallback(KeySmtpUseSsl, "BARANGAY_SMTP_SSL"), true),
            IsEnabled = ParseBool(SystemConfigService.Get(KeySmtpEnabled, "false"), false)
        };
    }

    /// <summary>
    /// Save SMTP settings to the database.
    /// </summary>
    public static void SaveSmtpSettings(SmtpSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        SystemConfigService.Set(KeySmtpHost, settings.Host?.Trim());
        SystemConfigService.Set(KeySmtpPort, settings.Port.ToString());
        SystemConfigService.Set(KeySmtpFromEmail, settings.FromEmail?.Trim());
        SystemConfigService.Set(KeySmtpFromName, settings.FromName?.Trim());
        SystemConfigService.Set(KeySmtpUsername, settings.Username?.Trim());
        SystemConfigService.Set(KeySmtpPassword, settings.Password);
        SystemConfigService.Set(KeySmtpUseSsl, settings.UseSsl.ToString().ToLower());
        SystemConfigService.Set(KeySmtpEnabled, settings.IsEnabled.ToString().ToLower());

        AppLogger.LogInfo("SMTP settings updated.");
    }

    /// <summary>
    /// Load all SMS settings from the database.
    /// </summary>
    public static SmsSettings LoadSmsSettings()
    {
        return new SmsSettings
        {
            ApiUrl = GetWithEnvFallback(KeySmsApiUrl, "BARANGAY_SMS_API_URL"),
            ApiToken = GetWithEnvFallback(KeySmsApiToken, "BARANGAY_SMS_API_TOKEN"),
            SenderName = GetWithEnvFallback(KeySmsSenderName, "BARANGAY_SMS_SENDER", "Barangay"),
            IsEnabled = ParseBool(SystemConfigService.Get(KeySmsEnabled, "false"), false)
        };
    }

    /// <summary>
    /// Save SMS settings to the database.
    /// </summary>
    public static void SaveSmsSettings(SmsSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        SystemConfigService.Set(KeySmsApiUrl, settings.ApiUrl?.Trim());
        SystemConfigService.Set(KeySmsApiToken, settings.ApiToken);
        SystemConfigService.Set(KeySmsSenderName, settings.SenderName?.Trim());
        SystemConfigService.Set(KeySmsEnabled, settings.IsEnabled.ToString().ToLower());

        AppLogger.LogInfo("SMS settings updated.");
    }

    /// <summary>
    /// Load reminder automation settings.
    /// </summary>
    public static ReminderSettings LoadReminderSettings()
    {
        return new ReminderSettings
        {
            RenewalReminderEnabled = ParseBool(SystemConfigService.Get(KeyRenewalReminderEnabled, "true"), true),
            RenewalReminderDays = ParseInt(SystemConfigService.Get(KeyRenewalReminderDays, "30"), 30),
            BlotterReminderEnabled = ParseBool(SystemConfigService.Get(KeyBlotterReminderEnabled, "true"), true),
            BlotterReminderAgeDays = ParseInt(SystemConfigService.Get(KeyBlotterReminderAgeDays, "12"), 12)
        };
    }

    /// <summary>
    /// Save reminder automation settings.
    /// </summary>
    public static void SaveReminderSettings(ReminderSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        SystemConfigService.Set(KeyRenewalReminderEnabled, settings.RenewalReminderEnabled.ToString().ToLower());
        SystemConfigService.Set(KeyRenewalReminderDays, Math.Clamp(settings.RenewalReminderDays, 1, 365).ToString());
        SystemConfigService.Set(KeyBlotterReminderEnabled, settings.BlotterReminderEnabled.ToString().ToLower());
        SystemConfigService.Set(KeyBlotterReminderAgeDays, Math.Clamp(settings.BlotterReminderAgeDays, 1, 90).ToString());

        AppLogger.LogInfo("Reminder settings updated.");
    }

    /// <summary>
    /// Test SMTP connection by sending a test email.
    /// </summary>
    public static NotificationTestResult TestSmtp(SmtpSettings settings, string testRecipient)
    {
        if (settings == null || string.IsNullOrWhiteSpace(settings.Host))
            return NotificationTestResult.Failure("SMTP host is not configured.");

        if (string.IsNullOrWhiteSpace(testRecipient))
            return NotificationTestResult.Failure("Test recipient email is required.");

        try
        {
            using var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(
                    settings.FromEmail ?? "test@barangay.local",
                    settings.FromName ?? "Barangay System"),
                Subject = "Barangay System - SMTP Test",
                Body = "This is a test email from the Barangay Management System notification settings.",
                IsBodyHtml = false
            };
            message.To.Add(testRecipient);

            using var client = new System.Net.Mail.SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                client.Credentials = new System.Net.NetworkCredential(settings.Username, settings.Password ?? "");
            }

            client.Send(message);
            return NotificationTestResult.Success("Test email sent successfully.");
        }
        catch (Exception ex)
        {
            return NotificationTestResult.Failure($"SMTP test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test SMS API by sending a test message.
    /// </summary>
    public static NotificationTestResult TestSms(SmsSettings settings, string testNumber)
    {
        if (settings == null || string.IsNullOrWhiteSpace(settings.ApiUrl))
            return NotificationTestResult.Failure("SMS API URL is not configured.");

        if (string.IsNullOrWhiteSpace(testNumber))
            return NotificationTestResult.Failure("Test phone number is required.");

        try
        {
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                to = testNumber.Trim(),
                message = "Barangay System SMS test. If you received this, SMS notifications are working.",
                sender = settings.SenderName ?? "Barangay"
            });

            using var request = new System.Net.Http.HttpRequestMessage(
                System.Net.Http.HttpMethod.Post, settings.ApiUrl);
            request.Content = new System.Net.Http.StringContent(
                payload, System.Text.Encoding.UTF8, "application/json");

            if (!string.IsNullOrWhiteSpace(settings.ApiToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiToken);
            }

            using var response = httpClient.Send(request);
            if (response.IsSuccessStatusCode)
            {
                return NotificationTestResult.Success("Test SMS sent successfully.");
            }

            string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return NotificationTestResult.Failure($"SMS API returned {(int)response.StatusCode}: {Truncate(body, 200)}");
        }
        catch (Exception ex)
        {
            return NotificationTestResult.Failure($"SMS test failed: {ex.Message}");
        }
    }

    private static string GetWithEnvFallback(string configKey, string envKey, string defaultValue = "")
    {
        string dbValue = SystemConfigService.Get(configKey);
        if (!string.IsNullOrWhiteSpace(dbValue)) return dbValue;

        string envValue = Environment.GetEnvironmentVariable(envKey) ?? string.Empty;
        return string.IsNullOrWhiteSpace(envValue) ? defaultValue : envValue;
    }

    private static int ParseInt(string? value, int fallback)
    {
        if (int.TryParse(value, out int result)) return result;
        return fallback;
    }

    private static bool ParseBool(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (bool.TryParse(value, out bool result)) return result;
        if (string.Equals(value, "1")) return true;
        if (string.Equals(value, "0")) return false;
        return fallback;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}

/// <summary>
/// SMTP email configuration settings.
/// </summary>
public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Barangay System";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public bool IsEnabled { get; set; }
}

/// <summary>
/// SMS API configuration settings.
/// </summary>
public sealed class SmsSettings
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Barangay";
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Automated reminder configuration.
/// </summary>
public sealed class ReminderSettings
{
    public bool RenewalReminderEnabled { get; set; } = true;
    public int RenewalReminderDays { get; set; } = 30;
    public bool BlotterReminderEnabled { get; set; } = true;
    public int BlotterReminderAgeDays { get; set; } = 12;
}

/// <summary>
/// Result of a notification test (SMTP or SMS).
/// </summary>
public sealed class NotificationTestResult
{
    public bool IsSuccess { get; private init; }
    public string Message { get; private init; } = string.Empty;

    public static NotificationTestResult Success(string message) =>
        new() { IsSuccess = true, Message = message };

    public static NotificationTestResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}
