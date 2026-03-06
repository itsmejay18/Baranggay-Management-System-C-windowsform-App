using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using baranggaysystem1.Database;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal static class OutboundNotificationService
{
    private static readonly HttpClient HttpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly object AutomationSync = new object();
    private static bool _automationRunning;

    public static bool TryRunScheduledAutomation(bool includeReminderQueue, int maxDispatch = 20)
    {
        lock (AutomationSync)
        {
            if (_automationRunning)
            {
                return false;
            }

            _automationRunning = true;
        }

        try
        {
            if (includeReminderQueue)
            {
                QueueRenewalReminders();
                QueueBlotterReminders();
            }

            DispatchPending(maxDispatch);
            return true;
        }
        catch (Exception ex)
        {
            helper.AppLogger.LogWarning("Notification automation failed.", ex);
            return false;
        }
        finally
        {
            lock (AutomationSync)
            {
                _automationRunning = false;
            }
        }
    }

    public static void QueueCertificateRelease(int certificateId)
    {
        if (certificateId <= 0)
        {
            return;
        }

        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var release = LoadCertificateReleaseContext(conn, tx, certificateId);
        if (release == null)
        {
            tx.Rollback();
            return;
        }

        int queued = 0;
        if (!string.IsNullOrWhiteSpace(release.Email))
        {
            string message = BuildReleaseMessage(release);
            if (TryEnqueue(
                    conn,
                    tx,
                    dedupeKey: $"cert_release_email:{release.RequestId}",
                    channel: "EMAIL",
                    recipient: release.Email.Trim(),
                    subject: $"{release.DocumentType} Ready for Release",
                    message: message,
                    sourceModule: "Certificates",
                    sourceRecordId: release.RequestId,
                    templateKey: "CERTIFICATE_RELEASE"))
            {
                queued++;
            }
        }

        if (!string.IsNullOrWhiteSpace(release.MobileNo))
        {
            string message = BuildReleaseMessage(release, compact: true);
            if (TryEnqueue(
                    conn,
                    tx,
                    dedupeKey: $"cert_release_sms:{release.RequestId}",
                    channel: "SMS",
                    recipient: release.MobileNo.Trim(),
                    subject: null,
                    message: message,
                    sourceModule: "Certificates",
                    sourceRecordId: release.RequestId,
                    templateKey: "CERTIFICATE_RELEASE"))
            {
                queued++;
            }
        }

        if (queued > 0)
        {
            MarkReleaseNotificationQueued(conn, tx, release.RequestId);
        }

        tx.Commit();
    }

    public static void QueueRenewalReminders()
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        using var cmd = new MySqlCommand(
            @"SELECT dr.doc_request_id,
                     dr.document_no,
                     dr.expires_at,
                     dr.renewal_notified_at,
                     dt.name AS document_type,
                     COALESCE(NULLIF(r.first_name, ''), 'Resident') AS first_name,
                     COALESCE(NULLIF(r.last_name, ''), '') AS last_name,
                     r.contact_no,
                     r.email,
                     IFNULL(dt.renewal_reminder_days, 30) AS reminder_days,
                     TIMESTAMPDIFF(DAY, CURRENT_DATE(), DATE(dr.expires_at)) AS days_left
              FROM document_request dr
              INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
              INNER JOIN resident r ON r.resident_id = dr.resident_id
              WHERE dr.status = 'RELEASED'
                AND dr.expires_at IS NOT NULL
                AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
                AND (
                    TIMESTAMPDIFF(DAY, CURRENT_DATE(), DATE(dr.expires_at)) < 0
                    OR TIMESTAMPDIFF(DAY, CURRENT_DATE(), DATE(dr.expires_at)) <= IFNULL(dt.renewal_reminder_days, 30)
                )
                AND (
                    dr.renewal_notified_at IS NULL
                    OR DATE(dr.renewal_notified_at) < CURRENT_DATE()
                )
              ORDER BY dr.expires_at ASC, dr.doc_request_id ASC",
            conn,
            tx);

        var candidates = new List<RenewalReminderCandidate>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                candidates.Add(new RenewalReminderCandidate
                {
                    RequestId = Convert.ToInt32(reader["doc_request_id"]),
                    ResidentName = BuildResidentName(
                        Convert.ToString(reader["first_name"]),
                        Convert.ToString(reader["last_name"])),
                    DocumentNo = Convert.ToString(reader["document_no"]) ?? string.Empty,
                    DocumentType = Convert.ToString(reader["document_type"]) ?? "Barangay Clearance",
                    Email = Convert.ToString(reader["email"]) ?? string.Empty,
                    MobileNo = Convert.ToString(reader["contact_no"]) ?? string.Empty,
                    ExpiresAt = reader["expires_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["expires_at"]),
                    DaysLeft = reader["days_left"] == DBNull.Value ? 0 : Convert.ToInt32(reader["days_left"])
                });
            }
        }

        var notifiedIds = new List<int>();
        string dayKey = DateTime.Now.ToString("yyyyMMdd");
        foreach (RenewalReminderCandidate candidate in candidates)
        {
            int requestId = candidate.RequestId;
            string documentNo = string.IsNullOrWhiteSpace(candidate.DocumentNo) ? $"#{requestId}" : candidate.DocumentNo;
            string headline = candidate.DaysLeft < 0
                ? $"Your {candidate.DocumentType} has expired."
                : candidate.DaysLeft == 0
                    ? $"Your {candidate.DocumentType} expires today."
                    : $"Your {candidate.DocumentType} will expire in {candidate.DaysLeft} day(s).";

            string message =
                $"Hello {candidate.ResidentName},\n\n" +
                $"{headline}\n" +
                $"Document No: {documentNo}\n" +
                (candidate.ExpiresAt.HasValue ? $"Expiry Date: {candidate.ExpiresAt:MMM dd, yyyy}\n" : string.Empty) +
                "Please visit the barangay office to process renewal.\n\n" +
                "This is an automated reminder.";

            bool queued = false;
            if (!string.IsNullOrWhiteSpace(candidate.Email))
            {
                queued |= TryEnqueue(
                    conn,
                    tx,
                    dedupeKey: $"clearance_renewal_email:{requestId}:{dayKey}",
                    channel: "EMAIL",
                    recipient: candidate.Email.Trim(),
                    subject: $"{candidate.DocumentType} Renewal Reminder",
                    message: message,
                    sourceModule: "Certificates",
                    sourceRecordId: requestId,
                    templateKey: "CLEARANCE_RENEWAL");
            }

            if (!string.IsNullOrWhiteSpace(candidate.MobileNo))
            {
                queued |= TryEnqueue(
                    conn,
                    tx,
                    dedupeKey: $"clearance_renewal_sms:{requestId}:{dayKey}",
                    channel: "SMS",
                    recipient: candidate.MobileNo.Trim(),
                    subject: null,
                    message: message.Replace("\n", " "),
                    sourceModule: "Certificates",
                    sourceRecordId: requestId,
                    templateKey: "CLEARANCE_RENEWAL");
            }

            if (queued)
            {
                notifiedIds.Add(requestId);
            }
        }

        foreach (int id in notifiedIds)
        {
            using var updateCmd = new MySqlCommand(
                "UPDATE document_request SET renewal_notified_at = NOW() WHERE doc_request_id = @id",
                conn,
                tx);
            updateCmd.Parameters.AddWithValue("@id", id);
            updateCmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public static void QueueBlotterReminders()
    {
        using var conn = DBConnection.GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        using var cmd = new MySqlCommand(
            $@"SELECT cr.case_id,
                      COALESCE(NULLIF(cr.incident_type, ''), 'Blotter Case') AS incident_type,
                      cr.status,
                      cr.created_at,
                      COALESCE(NULLIF(r.first_name, ''), 'Resident') AS first_name,
                      COALESCE(NULLIF(r.last_name, ''), '') AS last_name,
                      r.contact_no,
                      r.email,
                      TIMESTAMPDIFF(DAY, DATE(cr.created_at), CURRENT_DATE()) AS age_days
               FROM case_record cr
               LEFT JOIN resident r ON r.resident_id = cr.complainant_id
               WHERE UPPER(cr.status) IN ('OPEN', 'ONGOING')
                 AND TIMESTAMPDIFF(DAY, DATE(cr.created_at), CURRENT_DATE()) >= @minimumAge
               ORDER BY cr.created_at ASC, cr.case_id ASC",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@minimumAge", Math.Max(0, SlaRules.BlotterResolutionSlaDays - SlaRules.BlotterDueSoonDays));

        var candidates = new List<BlotterReminderCandidate>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                candidates.Add(new BlotterReminderCandidate
                {
                    CaseId = Convert.ToInt32(reader["case_id"]),
                    IncidentType = Convert.ToString(reader["incident_type"]) ?? "Blotter Case",
                    ResidentName = BuildResidentName(
                        Convert.ToString(reader["first_name"]) ?? "Resident",
                        Convert.ToString(reader["last_name"]) ?? string.Empty),
                    Email = Convert.ToString(reader["email"]) ?? string.Empty,
                    MobileNo = Convert.ToString(reader["contact_no"]) ?? string.Empty,
                    AgeDays = reader["age_days"] == DBNull.Value ? 0 : Convert.ToInt32(reader["age_days"]),
                    Status = Convert.ToString(reader["status"]) ?? "ONGOING"
                });
            }
        }

        string dayKey = DateTime.Now.ToString("yyyyMMdd");
        foreach (BlotterReminderCandidate candidate in candidates)
        {
            string headline = candidate.AgeDays > SlaRules.BlotterResolutionSlaDays
                ? "This blotter case is overdue for resolution."
                : "This blotter case is due soon for resolution.";

            string message =
                $"Hello {candidate.ResidentName},\n\n" +
                $"{headline}\n" +
                $"Case ID: {candidate.CaseId}\n" +
                $"Type: {candidate.IncidentType}\n" +
                $"Current Status: {candidate.Status}\n\n" +
                "Please coordinate with the barangay office for follow-up.";

            if (!string.IsNullOrWhiteSpace(candidate.Email))
            {
                TryEnqueue(
                    conn,
                    tx,
                    dedupeKey: $"blotter_reminder_email:{candidate.CaseId}:{dayKey}",
                    channel: "EMAIL",
                    recipient: candidate.Email.Trim(),
                    subject: "Blotter Case Reminder",
                    message: message,
                    sourceModule: "Blotter",
                    sourceRecordId: candidate.CaseId,
                    templateKey: "BLOTTER_REMINDER");
            }

            if (!string.IsNullOrWhiteSpace(candidate.MobileNo))
            {
                TryEnqueue(
                    conn,
                    tx,
                    dedupeKey: $"blotter_reminder_sms:{candidate.CaseId}:{dayKey}",
                    channel: "SMS",
                    recipient: candidate.MobileNo.Trim(),
                    subject: null,
                    message: message.Replace("\n", " "),
                    sourceModule: "Blotter",
                    sourceRecordId: candidate.CaseId,
                    templateKey: "BLOTTER_REMINDER");
            }
        }

        tx.Commit();
    }

    public static void DispatchPending(int maxBatch = 20)
    {
        if (maxBatch <= 0)
        {
            return;
        }

        using var conn = DBConnection.GetConnection();
        conn.Open();

        var pending = new List<PendingNotification>();
        using (var cmd = new MySqlCommand(
                   @"SELECT notification_id,
                            channel,
                            recipient,
                            subject,
                            message,
                            attempts
                     FROM outbound_notification
                     WHERE status = 'PENDING'
                       AND scheduled_at <= NOW()
                     ORDER BY scheduled_at ASC, notification_id ASC
                     LIMIT @take",
                   conn))
        {
            cmd.Parameters.AddWithValue("@take", maxBatch);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                pending.Add(new PendingNotification
                {
                    NotificationId = Convert.ToInt64(reader["notification_id"]),
                    Channel = Convert.ToString(reader["channel"]) ?? string.Empty,
                    Recipient = Convert.ToString(reader["recipient"]) ?? string.Empty,
                    Subject = Convert.ToString(reader["subject"]) ?? string.Empty,
                    Message = Convert.ToString(reader["message"]) ?? string.Empty,
                    Attempts = reader["attempts"] == DBNull.Value ? 0 : Convert.ToInt32(reader["attempts"])
                });
            }
        }

        foreach (PendingNotification item in pending)
        {
            NotificationDispatchResult result = DispatchSingle(item);
            SaveDispatchResult(conn, item, result);
        }
    }

    private static NotificationDispatchResult DispatchSingle(PendingNotification notification)
    {
        try
        {
            if (string.Equals(notification.Channel, "EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                return SendEmail(notification.Recipient, notification.Subject, notification.Message);
            }

            if (string.Equals(notification.Channel, "SMS", StringComparison.OrdinalIgnoreCase))
            {
                return SendSms(notification.Recipient, notification.Message);
            }

            return NotificationDispatchResult.Failed("Unsupported channel.");
        }
        catch (Exception ex)
        {
            helper.AppLogger.LogWarning("Notification dispatch failed.", ex);
            return NotificationDispatchResult.Failed(ex.Message);
        }
    }

    private static NotificationDispatchResult SendEmail(string recipient, string subject, string message)
    {
        string host = Environment.GetEnvironmentVariable("BARANGAY_SMTP_HOST") ?? string.Empty;
        string fromAddress = Environment.GetEnvironmentVariable("BARANGAY_SMTP_FROM") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            return NotificationDispatchResult.Skipped("SMTP not configured.");
        }

        int port = ParseIntOrDefault(Environment.GetEnvironmentVariable("BARANGAY_SMTP_PORT"), 587);
        bool enableSsl = ParseBoolOrDefault(Environment.GetEnvironmentVariable("BARANGAY_SMTP_SSL"), true);
        string username = Environment.GetEnvironmentVariable("BARANGAY_SMTP_USER") ?? string.Empty;
        string password = Environment.GetEnvironmentVariable("BARANGAY_SMTP_PASS") ?? string.Empty;
        string fromName = Environment.GetEnvironmentVariable("BARANGAY_SMTP_FROM_NAME") ?? "Barangay System";

        using var mail = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = string.IsNullOrWhiteSpace(subject) ? "Barangay Notification" : subject,
            Body = message,
            IsBodyHtml = false
        };
        mail.To.Add(recipient);

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            smtp.Credentials = new NetworkCredential(username, password);
        }
        else
        {
            smtp.UseDefaultCredentials = true;
        }

        smtp.Send(mail);
        return NotificationDispatchResult.Sent("OK");
    }

    private static NotificationDispatchResult SendSms(string recipient, string message)
    {
        string apiUrl = Environment.GetEnvironmentVariable("BARANGAY_SMS_API_URL") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return NotificationDispatchResult.Skipped("SMS API not configured.");
        }

        string token = Environment.GetEnvironmentVariable("BARANGAY_SMS_API_TOKEN") ?? string.Empty;
        string sender = Environment.GetEnvironmentVariable("BARANGAY_SMS_SENDER") ?? "Barangay";

        var payload = new
        {
            to = recipient,
            message,
            sender
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using HttpResponseMessage response = HttpClient.Send(request);
        string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (response.IsSuccessStatusCode)
        {
            return NotificationDispatchResult.Sent($"HTTP {(int)response.StatusCode}");
        }

        return NotificationDispatchResult.Failed(
            $"HTTP {(int)response.StatusCode}: {Truncate(responseText, 320)}");
    }

    private static void SaveDispatchResult(MySqlConnection conn, PendingNotification source, NotificationDispatchResult result)
    {
        string status = result.State switch
        {
            DispatchState.Sent => "SENT",
            DispatchState.Skipped => "SKIPPED",
            _ => "FAILED"
        };

        int attemptNo = source.Attempts + 1;

        using (var updateCmd = new MySqlCommand(
                   @"UPDATE outbound_notification
                     SET status = @status,
                         attempts = attempts + 1,
                         sent_at = CASE WHEN @status IN ('SENT','SKIPPED') THEN NOW() ELSE sent_at END,
                         last_error = CASE WHEN @status = 'FAILED' THEN @error ELSE NULL END
                     WHERE notification_id = @id",
                   conn))
        {
            updateCmd.Parameters.AddWithValue("@status", status);
            updateCmd.Parameters.AddWithValue("@error", string.IsNullOrWhiteSpace(result.Message) ? DBNull.Value : result.Message);
            updateCmd.Parameters.AddWithValue("@id", source.NotificationId);
            updateCmd.ExecuteNonQuery();
        }

        using (var attemptCmd = new MySqlCommand(
                   @"INSERT INTO outbound_notification_attempt
                        (notification_id, attempt_no, attempted_at, success, response_code, response_message)
                     VALUES
                        (@id, @attemptNo, NOW(), @success, @code, @message)",
                   conn))
        {
            attemptCmd.Parameters.AddWithValue("@id", source.NotificationId);
            attemptCmd.Parameters.AddWithValue("@attemptNo", attemptNo);
            attemptCmd.Parameters.AddWithValue("@success", result.State == DispatchState.Sent ? 1 : 0);
            attemptCmd.Parameters.AddWithValue("@code", result.State.ToString().ToUpperInvariant());
            attemptCmd.Parameters.AddWithValue("@message", string.IsNullOrWhiteSpace(result.Message) ? DBNull.Value : result.Message);
            attemptCmd.ExecuteNonQuery();
        }
    }

    private static void MarkReleaseNotificationQueued(MySqlConnection conn, MySqlTransaction tx, int requestId)
    {
        using var cmd = new MySqlCommand(
            "UPDATE document_request SET release_notified_at = NOW() WHERE doc_request_id = @id",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@id", requestId);
        cmd.ExecuteNonQuery();
    }

    private static bool TryEnqueue(
        MySqlConnection conn,
        MySqlTransaction tx,
        string dedupeKey,
        string channel,
        string recipient,
        string? subject,
        string message,
        string sourceModule,
        int sourceRecordId,
        string templateKey)
    {
        if (string.IsNullOrWhiteSpace(dedupeKey) ||
            string.IsNullOrWhiteSpace(channel) ||
            string.IsNullOrWhiteSpace(recipient) ||
            string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        using var cmd = new MySqlCommand(
            @"INSERT INTO outbound_notification
                (dedupe_key, channel, recipient, subject, message, status, source_module, source_record_id, template_key, scheduled_at, created_by_user_id, created_at)
              VALUES
                (@dedupe, @channel, @recipient, @subject, @message, 'PENDING', @sourceModule, @sourceRecordId, @templateKey, NOW(), @createdBy, NOW())
              ON DUPLICATE KEY UPDATE
                subject = VALUES(subject),
                message = VALUES(message),
                updated_at = CURRENT_TIMESTAMP",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@dedupe", dedupeKey);
        cmd.Parameters.AddWithValue("@channel", channel);
        cmd.Parameters.AddWithValue("@recipient", recipient.Trim());
        cmd.Parameters.AddWithValue("@subject", string.IsNullOrWhiteSpace(subject) ? DBNull.Value : subject.Trim());
        cmd.Parameters.AddWithValue("@message", message.Trim());
        cmd.Parameters.AddWithValue("@sourceModule", sourceModule);
        cmd.Parameters.AddWithValue("@sourceRecordId", sourceRecordId);
        cmd.Parameters.AddWithValue("@templateKey", templateKey);
        cmd.Parameters.AddWithValue("@createdBy", helper.UserSession.UserId > 0 ? helper.UserSession.UserId : DBNull.Value);

        int affected = cmd.ExecuteNonQuery();
        return affected > 0;
    }

    private static CertificateReleaseContext? LoadCertificateReleaseContext(
        MySqlConnection conn,
        MySqlTransaction tx,
        int certificateId)
    {
        using var cmd = new MySqlCommand(
            @"SELECT dr.doc_request_id,
                     dr.document_no,
                     dr.released_at,
                     dr.expires_at,
                     dt.name AS document_type,
                     COALESCE(NULLIF(r.first_name, ''), 'Resident') AS first_name,
                     COALESCE(NULLIF(r.last_name, ''), '') AS last_name,
                     r.contact_no,
                     r.email
              FROM document_request dr
              INNER JOIN resident r ON r.resident_id = dr.resident_id
              LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
              WHERE dr.doc_request_id = @id
                AND dr.status = 'RELEASED'
              LIMIT 1",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@id", certificateId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new CertificateReleaseContext
        {
            RequestId = Convert.ToInt32(reader["doc_request_id"]),
            DocumentNo = Convert.ToString(reader["document_no"]) ?? string.Empty,
            DocumentType = Convert.ToString(reader["document_type"]) ?? "Certificate",
            ResidentName = BuildResidentName(Convert.ToString(reader["first_name"]), Convert.ToString(reader["last_name"])),
            Email = Convert.ToString(reader["email"]) ?? string.Empty,
            MobileNo = Convert.ToString(reader["contact_no"]) ?? string.Empty,
            ReleasedAt = reader["released_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["released_at"]),
            ExpiresAt = reader["expires_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["expires_at"])
        };
    }

    private static string BuildReleaseMessage(CertificateReleaseContext context, bool compact = false)
    {
        string documentNo = string.IsNullOrWhiteSpace(context.DocumentNo) ? $"#{context.RequestId}" : context.DocumentNo;
        if (compact)
        {
            return $"Hello {context.ResidentName}. Your {context.DocumentType} ({documentNo}) is ready for release."
                + (context.ExpiresAt.HasValue ? $" Expires: {context.ExpiresAt:MMM dd, yyyy}." : string.Empty);
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Hello {context.ResidentName},");
        builder.AppendLine();
        builder.AppendLine($"Your {context.DocumentType} is now released and ready for pickup.");
        builder.AppendLine($"Document No: {documentNo}");
        if (context.ReleasedAt != DateTime.MinValue)
        {
            builder.AppendLine($"Released On: {context.ReleasedAt:MMM dd, yyyy}");
        }
        if (context.ExpiresAt.HasValue)
        {
            builder.AppendLine($"Valid Until: {context.ExpiresAt:MMM dd, yyyy}");
        }
        builder.AppendLine();
        builder.Append("Please bring your valid ID when claiming your document.");
        return builder.ToString();
    }

    private static string BuildResidentName(string? firstName, string? lastName)
    {
        string first = firstName?.Trim() ?? string.Empty;
        string last = lastName?.Trim() ?? string.Empty;
        string name = string.Join(" ", new[] { first, last }).Trim();
        return string.IsNullOrWhiteSpace(name) ? "Resident" : name;
    }

    private static int ParseIntOrDefault(string? raw, int fallback)
    {
        return int.TryParse(raw, out int value) ? value : fallback;
    }

    private static bool ParseBoolOrDefault(string? raw, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (bool.TryParse(raw, out bool boolValue))
        {
            return boolValue;
        }

        return raw.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ => fallback
        };
    }

    private static string Truncate(string? text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string value = text.Trim();
        if (value.Length <= maxChars)
        {
            return value;
        }

        return value.Substring(0, maxChars) + "...";
    }

    private sealed class RenewalReminderCandidate
    {
        public int RequestId { get; init; }
        public string ResidentName { get; init; } = string.Empty;
        public string DocumentNo { get; init; } = string.Empty;
        public string DocumentType { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string MobileNo { get; init; } = string.Empty;
        public DateTime? ExpiresAt { get; init; }
        public int DaysLeft { get; init; }
    }

    private sealed class BlotterReminderCandidate
    {
        public int CaseId { get; init; }
        public string IncidentType { get; init; } = string.Empty;
        public string ResidentName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string MobileNo { get; init; } = string.Empty;
        public int AgeDays { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    private sealed class PendingNotification
    {
        public long NotificationId { get; init; }
        public string Channel { get; init; } = string.Empty;
        public string Recipient { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public int Attempts { get; init; }
    }

    private sealed class CertificateReleaseContext
    {
        public int RequestId { get; init; }
        public string DocumentNo { get; init; } = string.Empty;
        public string DocumentType { get; init; } = string.Empty;
        public string ResidentName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string MobileNo { get; init; } = string.Empty;
        public DateTime ReleasedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
    }

    private enum DispatchState
    {
        Sent,
        Failed,
        Skipped
    }

    private sealed class NotificationDispatchResult
    {
        public DispatchState State { get; private init; }
        public string Message { get; private init; } = string.Empty;

        public static NotificationDispatchResult Sent(string message)
        {
            return new NotificationDispatchResult { State = DispatchState.Sent, Message = message };
        }

        public static NotificationDispatchResult Failed(string message)
        {
            return new NotificationDispatchResult { State = DispatchState.Failed, Message = message };
        }

        public static NotificationDispatchResult Skipped(string message)
        {
            return new NotificationDispatchResult { State = DispatchState.Skipped, Message = message };
        }
    }
}
