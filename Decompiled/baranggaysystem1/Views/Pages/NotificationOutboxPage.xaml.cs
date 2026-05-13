using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Views.Pages;

public partial class NotificationOutboxPage : UserControl
{
	internal sealed class NotificationRow
	{
		public long NotificationId { get; init; }

		public string DedupeKey { get; init; } = string.Empty;


		public string Channel { get; init; } = string.Empty;


		public string Recipient { get; init; } = string.Empty;


		public string Subject { get; init; } = string.Empty;


		public string Message { get; init; } = string.Empty;


		public string Status { get; init; } = string.Empty;


		public string SourceModule { get; init; } = string.Empty;


		public int SourceRecordId { get; init; }

		public string TemplateKey { get; init; } = string.Empty;


		public string ScheduledAt { get; init; } = string.Empty;


		public string SentAt { get; init; } = string.Empty;


		public int Attempts { get; init; }

		public string LastError { get; init; } = string.Empty;


		public string CreatedAt { get; init; } = string.Empty;

	}

	private List<NotificationRow> _allRows = new List<NotificationRow>();

	private List<NotificationRow> _filteredRows = new List<NotificationRow>();

	private bool _isLoading;































	public NotificationOutboxPage()
	{
		InitializeComponent();
		statusFilterCombo.Items.Add("All Statuses");
		statusFilterCombo.Items.Add("PENDING");
		statusFilterCombo.Items.Add("SENT");
		statusFilterCombo.Items.Add("FAILED");
		statusFilterCombo.Items.Add("SKIPPED");
		statusFilterCombo.SelectedIndex = 0;
		channelFilterCombo.Items.Add("All Channels");
		channelFilterCombo.Items.Add("EMAIL");
		channelFilterCombo.Items.Add("SMS");
		channelFilterCombo.SelectedIndex = 0;
		moduleFilterCombo.Items.Add("All Modules");
		moduleFilterCombo.SelectedIndex = 0;
		base.Loaded += async delegate
		{
			await LoadDataAsync();
		};
	}

	private async Task LoadDataAsync()
	{
		if (_isLoading)
		{
			return;
		}
		_isLoading = true;
		try
		{
			_allRows = await Task.Run((Func<List<NotificationRow>>)LoadNotifications);
			List<string> list = (from m in (from r in _allRows
					select r.SourceModule into m
					where !string.IsNullOrWhiteSpace(m)
					select m).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby m
				select m).ToList();
			string text = (moduleFilterCombo.SelectedItem as string) ?? "All Modules";
			moduleFilterCombo.Items.Clear();
			moduleFilterCombo.Items.Add("All Modules");
			foreach (string item in list)
			{
				moduleFilterCombo.Items.Add(item);
			}
			moduleFilterCombo.SelectedItem = (moduleFilterCombo.Items.Contains(text) ? text : "All Modules");
			ApplyFilters();
			UpdateMetrics();
			footerRefreshLabel.Text = $"Last refreshed: {DateTime.Now:hh:mm:ss tt}";
		}
		catch (Exception ex)
		{
			AppLogger.LogError("NotificationOutboxPage load failed.", ex);
			footerCountLabel.Text = "Failed to load notification data.";
		}
		finally
		{
			_isLoading = false;
		}
	}

	private static List<NotificationRow> LoadNotifications()
	{
		DataTable dataTable = DbHelper.LoadTable("SELECT notification_id, dedupe_key, channel, recipient, subject,\n                         message, status, source_module, source_record_id,\n                         template_key, scheduled_at, sent_at, attempts,\n                         last_error, created_at\n                  FROM outbound_notification\n                  ORDER BY created_at DESC, notification_id DESC\n                  LIMIT 500");
		List<NotificationRow> list = new List<NotificationRow>();
		foreach (DataRow row in dataTable.Rows)
		{
			list.Add(new NotificationRow
			{
				NotificationId = Convert.ToInt64(row["notification_id"]),
				DedupeKey = (Convert.ToString(row["dedupe_key"]) ?? string.Empty),
				Channel = (Convert.ToString(row["channel"]) ?? string.Empty),
				Recipient = (Convert.ToString(row["recipient"]) ?? string.Empty),
				Subject = (Convert.ToString(row["subject"]) ?? string.Empty),
				Message = (Convert.ToString(row["message"]) ?? string.Empty),
				Status = (Convert.ToString(row["status"]) ?? string.Empty),
				SourceModule = (Convert.ToString(row["source_module"]) ?? string.Empty),
				SourceRecordId = ((row["source_record_id"] != DBNull.Value) ? Convert.ToInt32(row["source_record_id"]) : 0),
				TemplateKey = (Convert.ToString(row["template_key"]) ?? string.Empty),
				ScheduledAt = ((row["scheduled_at"] == DBNull.Value) ? string.Empty : Convert.ToDateTime(row["scheduled_at"]).ToString("MMM dd, yyyy hh:mm tt")),
				SentAt = ((row["sent_at"] == DBNull.Value) ? string.Empty : Convert.ToDateTime(row["sent_at"]).ToString("MMM dd, yyyy hh:mm tt")),
				Attempts = ((row["attempts"] != DBNull.Value) ? Convert.ToInt32(row["attempts"]) : 0),
				LastError = (Convert.ToString(row["last_error"]) ?? string.Empty),
				CreatedAt = ((row["created_at"] == DBNull.Value) ? string.Empty : Convert.ToDateTime(row["created_at"]).ToString("MMM dd, yyyy hh:mm tt"))
			});
		}
		return list;
	}

	private void ApplyFilters()
	{
		string search = searchBox.Text?.Trim() ?? string.Empty;
		string statusFilter = (statusFilterCombo.SelectedItem as string) ?? "All Statuses";
		string channelFilter = (channelFilterCombo.SelectedItem as string) ?? "All Channels";
		string moduleFilter = (moduleFilterCombo.SelectedItem as string) ?? "All Modules";
		_filteredRows = _allRows.Where(delegate(NotificationRow r)
		{
			if (statusFilter != "All Statuses" && !string.Equals(r.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (channelFilter != "All Channels" && !string.Equals(r.Channel, channelFilter, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (moduleFilter != "All Modules" && !string.Equals(r.SourceModule, moduleFilter, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (!string.IsNullOrWhiteSpace(search))
			{
				string recipient = r.Recipient;
				if (recipient == null || recipient.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
				{
					string subject = r.Subject;
					if (subject == null || subject.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
					{
						string message = r.Message;
						if (message == null || message.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
						{
							string templateKey = r.TemplateKey;
							if ((templateKey == null || templateKey.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) && !(r.NotificationId.ToString() == search))
							{
								return false;
							}
						}
					}
				}
			}
			return true;
		}).ToList();
		mainGrid.ItemsSource = _filteredRows;
		tableVisibleLabel.Text = $"{_filteredRows.Count} visible";
		footerCountLabel.Text = $"{_filteredRows.Count} of {_allRows.Count} notifications shown.";
		bool flag = _filteredRows.Count > 0;
		mainGrid.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		emptyState.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateMetrics()
	{
		int count = _allRows.Count;
		int value = _allRows.Count((NotificationRow r) => string.Equals(r.Status, "PENDING", StringComparison.OrdinalIgnoreCase));
		int num = _allRows.Count((NotificationRow r) => string.Equals(r.Status, "SENT", StringComparison.OrdinalIgnoreCase));
		int num2 = _allRows.Count((NotificationRow r) => string.Equals(r.Status, "FAILED", StringComparison.OrdinalIgnoreCase));
		totalCountValue.Text = count.ToString("N0");
		pendingCountValue.Text = value.ToString("N0");
		sentCountValue.Text = num.ToString("N0");
		failedCountValue.Text = num2.ToString("N0");
		totalChip.Text = $"{count} notification{((count != 1) ? "s" : "")}";
		pendingChip.Text = $"{value} pending";
		failedChip.Text = $"{num2} failed";
		btnRetryFailed.IsEnabled = num2 > 0;
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
	{
		searchBox.Text = string.Empty;
		statusFilterCombo.SelectedIndex = 0;
		channelFilterCombo.SelectedIndex = 0;
		moduleFilterCombo.SelectedIndex = 0;
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadDataAsync();
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is NotificationRow notificationRow))
		{
			ClearDetailPanel();
			return;
		}
		selectedSummaryLabel.Text = (string.IsNullOrWhiteSpace(notificationRow.Subject) ? notificationRow.TemplateKey : notificationRow.Subject);
		selectedChannelChip.Text = "Channel: " + notificationRow.Channel;
		selectedStatusChip.Text = "Status: " + notificationRow.Status;
		selectedModuleChip.Text = "Module: " + notificationRow.SourceModule;
		selectedRecipientLabel.Text = notificationRow.Recipient;
		selectedScheduledLabel.Text = notificationRow.ScheduledAt;
		selectedSentAtLabel.Text = (string.IsNullOrWhiteSpace(notificationRow.SentAt) ? "-" : notificationRow.SentAt);
		selectedAttemptsLabel.Text = notificationRow.Attempts.ToString();
		selectedMessageBox.Text = notificationRow.Message;
		selectedErrorBox.Text = (string.IsNullOrWhiteSpace(notificationRow.LastError) ? "(none)" : notificationRow.LastError);
		btnRetrySingle.IsEnabled = string.Equals(notificationRow.Status, "FAILED", StringComparison.OrdinalIgnoreCase) || string.Equals(notificationRow.Status, "SKIPPED", StringComparison.OrdinalIgnoreCase);
	}

	private void ClearDetailPanel()
	{
		selectedSummaryLabel.Text = "Select a notification to inspect its details.";
		selectedChannelChip.Text = "Channel: -";
		selectedStatusChip.Text = "Status: -";
		selectedModuleChip.Text = "Module: -";
		selectedRecipientLabel.Text = "-";
		selectedScheduledLabel.Text = "-";
		selectedSentAtLabel.Text = "-";
		selectedAttemptsLabel.Text = "-";
		selectedMessageBox.Text = string.Empty;
		selectedErrorBox.Text = string.Empty;
		btnRetrySingle.IsEnabled = false;
	}

	private async void BtnRetrySingle_Click(object sender, RoutedEventArgs e)
	{
		object selectedItem = mainGrid.SelectedItem;
		NotificationRow row = selectedItem as NotificationRow;
		if (row == null)
		{
			return;
		}
		btnRetrySingle.IsEnabled = false;
		try
		{
			if (await Task.Run(() => ResetNotificationToPending(row.NotificationId)) > 0)
			{
				DialogService.Instance.ShowInfo($"Notification #{row.NotificationId} has been reset to PENDING and will be retried on next dispatch cycle.", "Notification Retry");
			}
			await LoadDataAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Retry single notification failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Retry Failed");
		}
	}

	private async void BtnRetryFailed_Click(object sender, RoutedEventArgs e)
	{
		btnRetryFailed.IsEnabled = false;
		try
		{
			int value = await Task.Run((Func<int>)ResetAllFailedToPending);
			DialogService.Instance.ShowInfo($"{value} failed notification(s) have been reset to PENDING for retry.", "Retry All Failed");
			await LoadDataAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Retry all failed notifications failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Retry Failed");
		}
	}

	private static int ResetNotificationToPending(long notificationId)
	{
		return DbHelper.ExecuteNonQuery("UPDATE outbound_notification\n                  SET status = 'PENDING', last_error = NULL, scheduled_at = NOW()\n                  WHERE notification_id = @id AND status IN ('FAILED','SKIPPED')", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@id", (object)notificationId);
		});
	}

	private static int ResetAllFailedToPending()
	{
		return DbHelper.ExecuteNonQuery("UPDATE outbound_notification\n                  SET status = 'PENDING', last_error = NULL, scheduled_at = NOW()\n                  WHERE status = 'FAILED'");
	}}
