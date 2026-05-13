using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class OutboundNotificationService
{
	private sealed class RenewalReminderCandidate
	{
		public int RequestId { get; set; }

		public string ResidentName { get; set; } = string.Empty;

		public string DocumentNo { get; set; } = string.Empty;

		public string DocumentType { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public string MobileNo { get; set; } = string.Empty;

		public DateTime? ExpiresAt { get; set; }

		public int DaysLeft { get; set; }
	}

	private sealed class BlotterReminderCandidate
	{
		public int CaseId { get; set; }

		public string IncidentType { get; set; } = string.Empty;

		public string ResidentName { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public string MobileNo { get; set; } = string.Empty;

		public int AgeDays { get; set; }

		public string Status { get; set; } = string.Empty;
	}

	private sealed class PendingNotification
	{
		public long NotificationId { get; set; }

		public string Channel { get; set; } = string.Empty;

		public string Recipient { get; set; } = string.Empty;

		public string Subject { get; set; } = string.Empty;

		public string Message { get; set; } = string.Empty;

		public int Attempts { get; set; }
	}

	private sealed class CertificateReleaseContext
	{
		public int RequestId { get; set; }

		public string DocumentNo { get; set; } = string.Empty;

		public string DocumentType { get; set; } = string.Empty;

		public string ResidentName { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public string MobileNo { get; set; } = string.Empty;

		public DateTime ReleasedAt { get; set; }

		public DateTime? ExpiresAt { get; set; }
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
			return new NotificationDispatchResult
			{
				State = DispatchState.Sent,
				Message = message
			};
		}

		public static NotificationDispatchResult Failed(string message)
		{
			return new NotificationDispatchResult
			{
				State = DispatchState.Failed,
				Message = message
			};
		}

		public static NotificationDispatchResult Skipped(string message)
		{
			return new NotificationDispatchResult
			{
				State = DispatchState.Skipped,
				Message = message
			};
		}
	}

	private static readonly HttpClient HttpClient = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10.0)
	};

	private static readonly object AutomationSync = new object();

	private static bool _automationRunning;

	public static bool TryRunScheduledAutomation(bool includeReminderQueue, int maxDispatch = 20)
	{
		if (ShouldSkipRemoteDispatch())
		{
			return false;
		}
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
			DBConnection.RegisterConnectivityFailure(ex);
			AppLogger.LogWarning("Notification automation failed.", ex);
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
		if (certificateId <= 0 || ShouldSkipRemoteDispatch())
		{
			return;
		}
		try
		{
			MySqlConnection connection = DBConnection.GetConnection();
			try
			{
				((DbConnection)(object)connection).Open();
				DBConnection.RegisterConnectivitySuccess();
				MySqlTransaction val = connection.BeginTransaction();
				try
				{
					CertificateReleaseContext certificateReleaseContext = LoadCertificateReleaseContext(connection, val, certificateId);
					if (certificateReleaseContext == null)
					{
						((DbTransaction)(object)val).Rollback();
						return;
					}
					int num = 0;
					if (!string.IsNullOrWhiteSpace(certificateReleaseContext.Email))
					{
						string message = BuildReleaseMessage(certificateReleaseContext);
						if (TryEnqueue(connection, val, $"cert_release_email:{certificateReleaseContext.RequestId}", "EMAIL", certificateReleaseContext.Email.Trim(), certificateReleaseContext.DocumentType + " Ready for Release", message, "Certificates", certificateReleaseContext.RequestId, "CERTIFICATE_RELEASE"))
						{
							num++;
						}
					}
					if (!string.IsNullOrWhiteSpace(certificateReleaseContext.MobileNo))
					{
						string message2 = BuildReleaseMessage(certificateReleaseContext, compact: true);
						if (TryEnqueue(connection, val, $"cert_release_sms:{certificateReleaseContext.RequestId}", "SMS", certificateReleaseContext.MobileNo.Trim(), null, message2, "Certificates", certificateReleaseContext.RequestId, "CERTIFICATE_RELEASE"))
						{
							num++;
						}
					}
					if (num > 0)
					{
						MarkReleaseNotificationQueued(connection, val, certificateReleaseContext.RequestId);
					}
					((DbTransaction)(object)val).Commit();
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			DBConnection.RegisterConnectivityFailure(ex);
			AppLogger.LogWarning("Certificate release notification queue skipped.", ex);
		}
	}

	public static void QueueRenewalReminders()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c5: Expected O, but got Unknown
		if (ShouldSkipRemoteDispatch())
		{
			return;
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			DBConnection.RegisterConnectivitySuccess();
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				MySqlCommand val2 = new MySqlCommand("SELECT dr.doc_request_id,\n                     dr.document_no,\n                     dr.expires_at,\n                     dr.renewal_notified_at,\n                     dt.name AS document_type,\n                     COALESCE(NULLIF(r.first_name, ''), 'Resident') AS first_name,\n                     COALESCE(NULLIF(r.last_name, ''), '') AS last_name,\n                     r.contact_no,\n                     r.email,\n                     IFNULL(dt.renewal_reminder_days, 30) AS reminder_days,\n                     TIMESTAMPDIFF(DAY, CURRENT_DATE(), DATE(dr.expires_at)) AS days_left\n              FROM document_request dr\n              INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n              INNER JOIN resident r ON r.resident_id = dr.resident_id\n              WHERE dr.status = 'RELEASED'\n                AND dr.expires_at IS NOT NULL\n                AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')\n                AND (\n                    TIMESTAMPDIFF(DAY, CURRENT_DATE(), DATE(dr.expires_at)) < 0\n                    OR TIMESTAMPDIFF(DAY, CURRENT_DATE(), DATE(dr.expires_at)) <= IFNULL(dt.renewal_reminder_days, 30)\n                )\n                AND (\n                    dr.renewal_notified_at IS NULL\n                    OR DATE(dr.renewal_notified_at) < CURRENT_DATE()\n                )\n              ORDER BY dr.expires_at ASC, dr.doc_request_id ASC", connection, val);
				try
				{
					List<RenewalReminderCandidate> list = new List<RenewalReminderCandidate>();
					MySqlDataReader val3 = val2.ExecuteReader();
					try
					{
						while (((DbDataReader)(object)val3).Read())
						{
							list.Add(new RenewalReminderCandidate
							{
								RequestId = Convert.ToInt32(((DbDataReader)(object)val3)["doc_request_id"]),
								ResidentName = BuildResidentName(Convert.ToString(((DbDataReader)(object)val3)["first_name"]), Convert.ToString(((DbDataReader)(object)val3)["last_name"])),
								DocumentNo = (Convert.ToString(((DbDataReader)(object)val3)["document_no"]) ?? string.Empty),
								DocumentType = (Convert.ToString(((DbDataReader)(object)val3)["document_type"]) ?? "Barangay Clearance"),
								Email = (Convert.ToString(((DbDataReader)(object)val3)["email"]) ?? string.Empty),
								MobileNo = (Convert.ToString(((DbDataReader)(object)val3)["contact_no"]) ?? string.Empty),
								ExpiresAt = ((((DbDataReader)(object)val3)["expires_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(((DbDataReader)(object)val3)["expires_at"]))),
								DaysLeft = ((((DbDataReader)(object)val3)["days_left"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)val3)["days_left"]) : 0)
							});
						}
					}
					finally
					{
						((IDisposable)val3)?.Dispose();
					}
					List<int> list2 = new List<int>();
					string value = DateTime.Now.ToString("yyyyMMdd");
					foreach (RenewalReminderCandidate item in list)
					{
						int requestId = item.RequestId;
						string value2 = (string.IsNullOrWhiteSpace(item.DocumentNo) ? $"#{requestId}" : item.DocumentNo);
						string value3 = ((item.DaysLeft < 0) ? ("Your " + item.DocumentType + " has expired.") : ((item.DaysLeft == 0) ? ("Your " + item.DocumentType + " expires today.") : $"Your {item.DocumentType} will expire in {item.DaysLeft} day(s)."));
						string text = $"Hello {item.ResidentName},\n\n{value3}\nDocument No: {value2}\n" + (item.ExpiresAt.HasValue ? $"Expiry Date: {item.ExpiresAt:MMM dd, yyyy}\n" : string.Empty) + "Please visit the barangay office to process renewal.\n\nThis is an automated reminder.";
						bool flag = false;
						if (!string.IsNullOrWhiteSpace(item.Email))
						{
							flag |= TryEnqueue(connection, val, $"clearance_renewal_email:{requestId}:{value}", "EMAIL", item.Email.Trim(), item.DocumentType + " Renewal Reminder", text, "Certificates", requestId, "CLEARANCE_RENEWAL");
						}
						if (!string.IsNullOrWhiteSpace(item.MobileNo))
						{
							flag |= TryEnqueue(connection, val, $"clearance_renewal_sms:{requestId}:{value}", "SMS", item.MobileNo.Trim(), null, text.Replace("\n", " "), "Certificates", requestId, "CLEARANCE_RENEWAL");
						}
						if (flag)
						{
							list2.Add(requestId);
						}
					}
					foreach (int item2 in list2)
					{
						MySqlCommand val4 = new MySqlCommand("UPDATE document_request SET renewal_notified_at = NOW() WHERE doc_request_id = @id", connection, val);
						try
						{
							val4.Parameters.AddWithValue("@id", (object)item2);
							((DbCommand)(object)val4).ExecuteNonQuery();
						}
						finally
						{
							((IDisposable)val4)?.Dispose();
						}
					}
					((DbTransaction)(object)val).Commit();
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public static void QueueBlotterReminders()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		if (ShouldSkipRemoteDispatch())
		{
			return;
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			DBConnection.RegisterConnectivitySuccess();
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				MySqlCommand val2 = new MySqlCommand("SELECT cr.case_id,\n                      COALESCE(NULLIF(cr.incident_type, ''), 'Blotter Case') AS incident_type,\n                      cr.status,\n                      cr.created_at,\n                      COALESCE(NULLIF(r.first_name, ''), 'Resident') AS first_name,\n                      COALESCE(NULLIF(r.last_name, ''), '') AS last_name,\n                      r.contact_no,\n                      r.email,\n                      TIMESTAMPDIFF(DAY, DATE(cr.created_at), CURRENT_DATE()) AS age_days\n               FROM case_record cr\n               LEFT JOIN resident r ON r.resident_id = cr.complainant_id\n               WHERE UPPER(cr.status) IN ('OPEN', 'ONGOING')\n                 AND TIMESTAMPDIFF(DAY, DATE(cr.created_at), CURRENT_DATE()) >= @minimumAge\n               ORDER BY cr.created_at ASC, cr.case_id ASC", connection, val);
				try
				{
					val2.Parameters.AddWithValue("@minimumAge", (object)Math.Max(0, 12));
					List<BlotterReminderCandidate> list = new List<BlotterReminderCandidate>();
					MySqlDataReader val3 = val2.ExecuteReader();
					try
					{
						while (((DbDataReader)(object)val3).Read())
						{
							list.Add(new BlotterReminderCandidate
							{
								CaseId = Convert.ToInt32(((DbDataReader)(object)val3)["case_id"]),
								IncidentType = (Convert.ToString(((DbDataReader)(object)val3)["incident_type"]) ?? "Blotter Case"),
								ResidentName = BuildResidentName(Convert.ToString(((DbDataReader)(object)val3)["first_name"]) ?? "Resident", Convert.ToString(((DbDataReader)(object)val3)["last_name"]) ?? string.Empty),
								Email = (Convert.ToString(((DbDataReader)(object)val3)["email"]) ?? string.Empty),
								MobileNo = (Convert.ToString(((DbDataReader)(object)val3)["contact_no"]) ?? string.Empty),
								AgeDays = ((((DbDataReader)(object)val3)["age_days"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)val3)["age_days"]) : 0),
								Status = (Convert.ToString(((DbDataReader)(object)val3)["status"]) ?? "ONGOING")
							});
						}
					}
					finally
					{
						((IDisposable)val3)?.Dispose();
					}
					string value = DateTime.Now.ToString("yyyyMMdd");
					foreach (BlotterReminderCandidate item in list)
					{
						string value2 = ((item.AgeDays > 15) ? "This blotter case is overdue for resolution." : "This blotter case is due soon for resolution.");
						string text = $"Hello {item.ResidentName},\n\n{value2}\nCase ID: {item.CaseId}\nType: {item.IncidentType}\nCurrent Status: {item.Status}\n\n" + "Please coordinate with the barangay office for follow-up.";
						if (!string.IsNullOrWhiteSpace(item.Email))
						{
							TryEnqueue(connection, val, $"blotter_reminder_email:{item.CaseId}:{value}", "EMAIL", item.Email.Trim(), "Blotter Case Reminder", text, "Blotter", item.CaseId, "BLOTTER_REMINDER");
						}
						if (!string.IsNullOrWhiteSpace(item.MobileNo))
						{
							TryEnqueue(connection, val, $"blotter_reminder_sms:{item.CaseId}:{value}", "SMS", item.MobileNo.Trim(), null, text.Replace("\n", " "), "Blotter", item.CaseId, "BLOTTER_REMINDER");
						}
					}
					((DbTransaction)(object)val).Commit();
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	public static void DispatchPending(int maxBatch = 20)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		if (maxBatch <= 0 || ShouldSkipRemoteDispatch())
		{
			return;
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			DBConnection.RegisterConnectivitySuccess();
			List<PendingNotification> list = new List<PendingNotification>();
			MySqlCommand val = new MySqlCommand("SELECT notification_id,\n                            channel,\n                            recipient,\n                            subject,\n                            message,\n                            attempts\n                     FROM outbound_notification\n                     WHERE status = 'PENDING'\n                       AND scheduled_at <= NOW()\n                     ORDER BY scheduled_at ASC, notification_id ASC\n                     LIMIT @take", connection);
			try
			{
				val.Parameters.AddWithValue("@take", (object)maxBatch);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					while (((DbDataReader)(object)val2).Read())
					{
						list.Add(new PendingNotification
						{
							NotificationId = Convert.ToInt64(((DbDataReader)(object)val2)["notification_id"]),
							Channel = (Convert.ToString(((DbDataReader)(object)val2)["channel"]) ?? string.Empty),
							Recipient = (Convert.ToString(((DbDataReader)(object)val2)["recipient"]) ?? string.Empty),
							Subject = (Convert.ToString(((DbDataReader)(object)val2)["subject"]) ?? string.Empty),
							Message = (Convert.ToString(((DbDataReader)(object)val2)["message"]) ?? string.Empty),
							Attempts = ((((DbDataReader)(object)val2)["attempts"] != DBNull.Value) ? Convert.ToInt32(((DbDataReader)(object)val2)["attempts"]) : 0)
						});
					}
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			foreach (PendingNotification item in list)
			{
				NotificationDispatchResult result = DispatchSingle(item);
				SaveDispatchResult(connection, item, result);
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
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
			AppLogger.LogWarning("Notification dispatch failed.", ex);
			return NotificationDispatchResult.Failed(ex.Message);
		}
	}

	private static bool ShouldSkipRemoteDispatch()
	{
		if (!OfflineDatabaseSupport.IsOffline)
		{
			return DBConnection.ShouldThrottleOnlineAccess(includeOfflineMode: false);
		}
		return true;
	}

	private static NotificationDispatchResult SendEmail(string recipient, string subject, string message)
	{
		string text = Environment.GetEnvironmentVariable("BARANGAY_SMTP_HOST") ?? string.Empty;
		string text2 = Environment.GetEnvironmentVariable("BARANGAY_SMTP_FROM") ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return NotificationDispatchResult.Skipped("SMTP not configured.");
		}
		int port = ParseIntOrDefault(Environment.GetEnvironmentVariable("BARANGAY_SMTP_PORT"), 587);
		bool enableSsl = ParseBoolOrDefault(Environment.GetEnvironmentVariable("BARANGAY_SMTP_SSL"), fallback: true);
		string text3 = Environment.GetEnvironmentVariable("BARANGAY_SMTP_USER") ?? string.Empty;
		string password = Environment.GetEnvironmentVariable("BARANGAY_SMTP_PASS") ?? string.Empty;
		string displayName = Environment.GetEnvironmentVariable("BARANGAY_SMTP_FROM_NAME") ?? "Barangay System";
		using MailMessage mailMessage = new MailMessage
		{
			From = new MailAddress(text2, displayName),
			Subject = (string.IsNullOrWhiteSpace(subject) ? "Barangay Notification" : subject),
			Body = message,
			IsBodyHtml = false
		};
		mailMessage.To.Add(recipient);
		using SmtpClient smtpClient = new SmtpClient(text, port)
		{
			EnableSsl = enableSsl,
			DeliveryMethod = SmtpDeliveryMethod.Network
		};
		if (!string.IsNullOrWhiteSpace(text3))
		{
			smtpClient.Credentials = new NetworkCredential(text3, password);
		}
		else
		{
			smtpClient.UseDefaultCredentials = true;
		}
		smtpClient.Send(mailMessage);
		return NotificationDispatchResult.Sent("OK");
	}

	private static NotificationDispatchResult SendSms(string recipient, string message)
	{
		string text = Environment.GetEnvironmentVariable("BARANGAY_SMS_API_URL") ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			return NotificationDispatchResult.Skipped("SMS API not configured.");
		}
		string text2 = Environment.GetEnvironmentVariable("BARANGAY_SMS_API_TOKEN") ?? string.Empty;
		string sender = Environment.GetEnvironmentVariable("BARANGAY_SMS_SENDER") ?? "Barangay";
		var value = new
		{
			to = recipient,
			message = message,
			sender = sender
		};
		using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, text);
		httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
		if (!string.IsNullOrWhiteSpace(text2))
		{
			httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", text2);
		}
		using HttpResponseMessage httpResponseMessage = HttpClient.Send(httpRequestMessage);
		string result = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		if (httpResponseMessage.IsSuccessStatusCode)
		{
			return NotificationDispatchResult.Sent($"HTTP {(int)httpResponseMessage.StatusCode}");
		}
		return NotificationDispatchResult.Failed($"HTTP {(int)httpResponseMessage.StatusCode}: {Truncate(result, 320)}");
	}

	private static void SaveDispatchResult(MySqlConnection conn, PendingNotification source, NotificationDispatchResult result)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		string text = result.State switch
		{
			DispatchState.Sent => "SENT", 
			DispatchState.Skipped => "SKIPPED", 
			_ => "FAILED", 
		};
		int num = source.Attempts + 1;
		MySqlCommand val = new MySqlCommand("UPDATE outbound_notification\n                     SET status = @status,\n                         attempts = attempts + 1,\n                         sent_at = CASE WHEN @status IN ('SENT','SKIPPED') THEN NOW() ELSE sent_at END,\n                         last_error = CASE WHEN @status = 'FAILED' THEN @error ELSE NULL END\n                     WHERE notification_id = @id", conn);
		try
		{
			val.Parameters.AddWithValue("@status", (object)text);
			val.Parameters.AddWithValue("@error", (object)(string.IsNullOrWhiteSpace(result.Message) ? ((IConvertible)DBNull.Value) : ((IConvertible)result.Message)));
			val.Parameters.AddWithValue("@id", (object)source.NotificationId);
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		MySqlCommand val2 = new MySqlCommand("INSERT INTO outbound_notification_attempt\n                        (notification_id, attempt_no, attempted_at, success, response_code, response_message)\n                     VALUES\n                        (@id, @attemptNo, NOW(), @success, @code, @message)", conn);
		try
		{
			val2.Parameters.AddWithValue("@id", (object)source.NotificationId);
			val2.Parameters.AddWithValue("@attemptNo", (object)num);
			val2.Parameters.AddWithValue("@success", (object)((result.State == DispatchState.Sent) ? 1 : 0));
			val2.Parameters.AddWithValue("@code", (object)result.State.ToString().ToUpperInvariant());
			val2.Parameters.AddWithValue("@message", (object)(string.IsNullOrWhiteSpace(result.Message) ? ((IConvertible)DBNull.Value) : ((IConvertible)result.Message)));
			((DbCommand)(object)val2).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	private static void MarkReleaseNotificationQueued(MySqlConnection conn, MySqlTransaction tx, int requestId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("UPDATE document_request SET release_notified_at = NOW() WHERE doc_request_id = @id", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@id", (object)requestId);
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool TryEnqueue(MySqlConnection conn, MySqlTransaction tx, string dedupeKey, string channel, string recipient, string? subject, string message, string sourceModule, int sourceRecordId, string templateKey)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		if (string.IsNullOrWhiteSpace(dedupeKey) || string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(message))
		{
			return false;
		}
		MySqlCommand val = new MySqlCommand("INSERT INTO outbound_notification\n                (dedupe_key, channel, recipient, subject, message, status, source_module, source_record_id, template_key, scheduled_at, created_by_user_id, created_at)\n              VALUES\n                (@dedupe, @channel, @recipient, @subject, @message, 'PENDING', @sourceModule, @sourceRecordId, @templateKey, NOW(), @createdBy, NOW())\n              ON DUPLICATE KEY UPDATE\n                subject = VALUES(subject),\n                message = VALUES(message),\n                updated_at = CURRENT_TIMESTAMP", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@dedupe", (object)dedupeKey);
			val.Parameters.AddWithValue("@channel", (object)channel);
			val.Parameters.AddWithValue("@recipient", (object)recipient.Trim());
			val.Parameters.AddWithValue("@subject", (object)(string.IsNullOrWhiteSpace(subject) ? ((IConvertible)DBNull.Value) : ((IConvertible)subject.Trim())));
			val.Parameters.AddWithValue("@message", (object)message.Trim());
			val.Parameters.AddWithValue("@sourceModule", (object)sourceModule);
			val.Parameters.AddWithValue("@sourceRecordId", (object)sourceRecordId);
			val.Parameters.AddWithValue("@templateKey", (object)templateKey);
			val.Parameters.AddWithValue("@createdBy", (UserSession.UserId > 0) ? ((object)UserSession.UserId) : DBNull.Value);
			return ((DbCommand)(object)val).ExecuteNonQuery() > 0;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static CertificateReleaseContext? LoadCertificateReleaseContext(MySqlConnection conn, MySqlTransaction tx, int certificateId)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT dr.doc_request_id,\n                     dr.document_no,\n                     dr.released_at,\n                     dr.expires_at,\n                     dt.name AS document_type,\n                     COALESCE(NULLIF(r.first_name, ''), 'Resident') AS first_name,\n                     COALESCE(NULLIF(r.last_name, ''), '') AS last_name,\n                     r.contact_no,\n                     r.email\n              FROM document_request dr\n              INNER JOIN resident r ON r.resident_id = dr.resident_id\n              LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id\n              WHERE dr.doc_request_id = @id\n                AND dr.status = 'RELEASED'\n              LIMIT 1", conn, tx);
		try
		{
			val.Parameters.AddWithValue("@id", (object)certificateId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					return null;
				}
				return new CertificateReleaseContext
				{
					RequestId = Convert.ToInt32(((DbDataReader)(object)val2)["doc_request_id"]),
					DocumentNo = (Convert.ToString(((DbDataReader)(object)val2)["document_no"]) ?? string.Empty),
					DocumentType = (Convert.ToString(((DbDataReader)(object)val2)["document_type"]) ?? "Certificate"),
					ResidentName = BuildResidentName(Convert.ToString(((DbDataReader)(object)val2)["first_name"]), Convert.ToString(((DbDataReader)(object)val2)["last_name"])),
					Email = (Convert.ToString(((DbDataReader)(object)val2)["email"]) ?? string.Empty),
					MobileNo = (Convert.ToString(((DbDataReader)(object)val2)["contact_no"]) ?? string.Empty),
					ReleasedAt = ((((DbDataReader)(object)val2)["released_at"] == DBNull.Value) ? DateTime.MinValue : Convert.ToDateTime(((DbDataReader)(object)val2)["released_at"])),
					ExpiresAt = ((((DbDataReader)(object)val2)["expires_at"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(((DbDataReader)(object)val2)["expires_at"])))
				};
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static string BuildReleaseMessage(CertificateReleaseContext context, bool compact = false)
	{
		string value = (string.IsNullOrWhiteSpace(context.DocumentNo) ? $"#{context.RequestId}" : context.DocumentNo);
		if (compact)
		{
			return $"Hello {context.ResidentName}. Your {context.DocumentType} ({value}) is ready for release." + (context.ExpiresAt.HasValue ? $" Expires: {context.ExpiresAt:MMM dd, yyyy}." : string.Empty);
		}
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
		handler.AppendLiteral("Hello ");
		handler.AppendFormatted(context.ResidentName);
		handler.AppendLiteral(",");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(43, 1, stringBuilder2);
		handler.AppendLiteral("Your ");
		handler.AppendFormatted(context.DocumentType);
		handler.AppendLiteral(" is now released and ready for pickup.");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("Document No: ");
		handler.AppendFormatted(value);
		stringBuilder5.AppendLine(ref handler);
		if (context.ReleasedAt != DateTime.MinValue)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("Released On: ");
			handler.AppendFormatted(context.ReleasedAt, "MMM dd, yyyy");
			stringBuilder6.AppendLine(ref handler);
		}
		if (context.ExpiresAt.HasValue)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("Valid Until: ");
			handler.AppendFormatted(context.ExpiresAt, "MMM dd, yyyy");
			stringBuilder7.AppendLine(ref handler);
		}
		stringBuilder.AppendLine();
		stringBuilder.Append("Please bring your valid ID when claiming your document.");
		return stringBuilder.ToString();
	}

	private static string BuildResidentName(string? firstName, string? lastName)
	{
		string text = firstName?.Trim() ?? string.Empty;
		string text2 = lastName?.Trim() ?? string.Empty;
		string text3 = string.Join(" ", text, text2).Trim();
		if (!string.IsNullOrWhiteSpace(text3))
		{
			return text3;
		}
		return "Resident";
	}

	private static int ParseIntOrDefault(string? raw, int fallback)
	{
		if (!int.TryParse(raw, out var result))
		{
			return fallback;
		}
		return result;
	}

	private static bool ParseBoolOrDefault(string? raw, bool fallback)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return fallback;
		}
		if (bool.TryParse(raw, out var result))
		{
			return result;
		}
		string text = raw.Trim();
		if (!(text == "1"))
		{
			if (text == "0")
			{
				return false;
			}
			return fallback;
		}
		return true;
	}

	private static string Truncate(string? text, int maxChars)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string text2 = text.Trim();
		if (text2.Length <= maxChars)
		{
			return text2;
		}
		return text2.Substring(0, maxChars) + "...";
	}
}
