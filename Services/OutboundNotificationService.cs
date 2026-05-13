using System;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

/// <summary>
/// Handles outbound notifications (email, SMS) for certificate releases, reminders, etc.
/// </summary>
public static class OutboundNotificationService
{
    /// <summary>
    /// Runs scheduled notification automation (certificate reminders, SLA alerts, etc.).
    /// </summary>
    public static void TryRunScheduledAutomation(bool includeReminderQueue, int maxDispatch = 20)
    {
        try
        {
            if (includeReminderQueue)
            {
                ProcessReminderQueue(maxDispatch);
            }

            ProcessSlaAlerts(maxDispatch);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Notification automation failed.", ex);
        }
    }

    /// <summary>
    /// Queues a notification for a certificate release event.
    /// </summary>
    public static void QueueCertificateRelease(int certificateId)
    {
        try
        {
            DbHelper.ExecuteNonQuery(
                @"INSERT INTO notification_queue (event_type, entity_id, status, created_at)
                  VALUES ('CERTIFICATE_RELEASED', @id, 'PENDING', NOW())",
                cmd => cmd.Parameters.AddWithValue("@id", certificateId));
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Failed to queue certificate release notification for ID {certificateId}: {ex.Message}");
        }
    }

    private static void ProcessReminderQueue(int maxDispatch)
    {
        var pending = DbHelper.LoadTable(
            $@"SELECT notification_id, event_type, entity_id
               FROM notification_queue
               WHERE status = 'PENDING'
               ORDER BY created_at ASC
               LIMIT {maxDispatch}");

        foreach (System.Data.DataRow row in pending.Rows)
        {
            int notificationId = Convert.ToInt32(row["notification_id"]);
            try
            {
                // Mark as dispatched (actual sending would integrate with email/SMS provider)
                DbHelper.ExecuteNonQuery(
                    @"UPDATE notification_queue SET status = 'DISPATCHED', dispatched_at = NOW()
                      WHERE notification_id = @id",
                    cmd => cmd.Parameters.AddWithValue("@id", notificationId));
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Failed to dispatch notification {notificationId}: {ex.Message}");
                DbHelper.ExecuteNonQuery(
                    @"UPDATE notification_queue SET status = 'FAILED', error_message = @msg
                      WHERE notification_id = @id",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@id", notificationId);
                        cmd.Parameters.AddWithValue("@msg", ex.Message);
                    });
            }
        }
    }

    private static void ProcessSlaAlerts(int maxDispatch)
    {
        // Check for SLA breaches and generate alerts
        int overdueApprovals = DbHelper.ExecuteScalar<int>(
            $@"SELECT COUNT(*) FROM document_request
               WHERE status = 'SUBMITTED'
                 AND requested_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY)") ;

        if (overdueApprovals > 0)
        {
            AppLogger.LogWarning($"SLA Alert: {overdueApprovals} certificate approvals are overdue.");
        }
    }
}
