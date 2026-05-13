using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Pages;

public class ClearancesPage : UserControl, IComponentConnector
{
	private readonly CertificateRequestService _certificateRequestService = new CertificateRequestService();

	private DataTable? _data;

	internal TextBlock recordCountLabel;

	internal TextBlock visibleQueueMetric;

	internal TextBlock pendingQueueMetric;

	internal TextBlock releasedQueueMetric;

	internal TextBlock attentionQueueMetric;

	internal TextBox searchBox;

	internal ComboBox typeFilter;

	internal ComboBox statusFilter;

	internal TextBlock tableMetaLabel;

	internal TextBlock tableVisibleLabel;

	internal TextBlock tableSelectionLabel;

	internal DataGrid mainGrid;

	internal StackPanel emptyState;

	internal TextBlock emptyLabel;

	internal Border contextActionBar;

	internal TextBlock selectedRecordLabel;

	internal TextBlock selectedRecordMetaLabel;

	internal TextBlock selectedVerificationLabel;

	internal Button btnRelease;

	internal TextBlock btnReleaseLabel;

	internal Button btnCancel;

	internal Button btnVerifySelected;

	internal TextBlock footerCountLabel;

	private bool _contentLoaded;

	public ClearancesPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public ClearancesPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		try
		{
			_data = await _certificateRequestService.GetQueueAsync();
			EnrichDisplayTable(_data);
			ApplyDataToGrid(_data);
			PopulateFilterOptions(_data);
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("ClearancesPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load clearance requests. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Unable to load queue.";
			recordCountLabel.Text = "Queue unavailable";
			visibleQueueMetric.Text = "0";
			pendingQueueMetric.Text = "0";
			releasedQueueMetric.Text = "0";
			attentionQueueMetric.Text = "0";
			tableVisibleLabel.Text = "0 visible";
			tableSelectionLabel.Text = "No selection";
			tableMetaLabel.Text = "Queue data is temporarily unavailable.";
			UpdateSelectionState(null);
		}
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		if (table == null || table.Rows.Count == 0)
		{
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "No clearance or certification requests found.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "No clearance requests found.";
			recordCountLabel.Text = "No requests on record.";
			visibleQueueMetric.Text = "0";
			pendingQueueMetric.Text = "0";
			releasedQueueMetric.Text = "0";
			attentionQueueMetric.Text = "0";
			tableVisibleLabel.Text = "0 visible";
			tableSelectionLabel.Text = "No selection";
			tableMetaLabel.Text = "Submit a request to start the queue.";
			UpdateSelectionState(null);
		}
		else
		{
			emptyState.Visibility = Visibility.Collapsed;
			mainGrid.ItemsSource = table.DefaultView;
		}
	}

	private void PopulateFilterOptions(DataTable? table)
	{
		typeFilter.Items.Clear();
		typeFilter.Items.Add("All Document Types");
		statusFilter.Items.Clear();
		statusFilter.Items.Add("All Status");
		if (table != null)
		{
			foreach (string item in from value in (from row in table.AsEnumerable()
					select Convert.ToString(row["certification_type"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				typeFilter.Items.Add(item);
			}
			foreach (string item2 in from value in (from row in table.AsEnumerable()
					select Convert.ToString(row["status"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				statusFilter.Items.Add(item2);
			}
		}
		typeFilter.SelectedIndex = 0;
		statusFilter.SelectedIndex = 0;
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded)
		{
			ApplyFilters();
		}
	}

	private void ApplyFilters()
	{
		if (_data == null)
		{
			return;
		}
		List<string> list = new List<string>();
		string value = searchBox.Text.Trim();
		if (!string.IsNullOrWhiteSpace(value))
		{
			string escaped = EscapeForRowFilter(value);
			string[] source = new string[12]
			{
				"tracking_code", "document_no", "verification_token", "resident_name", "certification_type", "purpose_display", "status", "requested_on", "released_on_display", "expires_on",
				"or_number_display", "fee_display"
			};
			list.Add("(" + string.Join(" OR ", source.Select((string column) => $"Convert([{column}], 'System.String') LIKE '%{escaped}%'")) + ")");
		}
		if (typeFilter.SelectedItem is string text && !string.Equals(text, "All Document Types", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[certification_type] = '" + EscapeForRowFilter(text) + "'");
		}
		if (statusFilter.SelectedItem is string text2 && !string.Equals(text2, "All Status", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[status] = '" + EscapeForRowFilter(text2) + "'");
		}
		_data.DefaultView.RowFilter = string.Join(" AND ", list);
		List<DataRowView> list2 = _data.DefaultView.Cast<DataRowView>().ToList();
		int count = list2.Count;
		int count2 = _data.Rows.Count;
		int num = list2.Count((DataRowView row) => IsPendingStatus(Convert.ToString(row["status"])));
		int num2 = list2.Count((DataRowView row) => IsStatus(row, "RELEASED"));
		int num3 = list2.Count((DataRowView row) => IsStatus(row, "REJECTED") || IsStatus(row, "CANCELLED"));
		emptyLabel.Text = ((count == 0) ? "No requests match the current filters." : "No clearance or certification requests found.");
		emptyState.Visibility = ((count != 0) ? Visibility.Collapsed : Visibility.Visible);
		visibleQueueMetric.Text = count.ToString("N0", CultureInfo.InvariantCulture);
		pendingQueueMetric.Text = num.ToString("N0", CultureInfo.InvariantCulture);
		releasedQueueMetric.Text = num2.ToString("N0", CultureInfo.InvariantCulture);
		attentionQueueMetric.Text = num3.ToString("N0", CultureInfo.InvariantCulture);
		footerCountLabel.Text = ((count == count2) ? $"Showing {count:N0} request(s)" : $"Showing {count:N0} of {count2:N0} request(s)");
		recordCountLabel.Text = ((count == count2) ? $"{count:N0} request(s) in the queue" : $"{count:N0} filtered request(s) in view");
		tableVisibleLabel.Text = $"{count:N0} visible";
		tableMetaLabel.Text = ((count == count2) ? "Browse the live queue, then select a request to release or cancel." : $"Filters are showing a focused subset from {count2:N0} loaded request(s).");
		DataRowView selectedRow = mainGrid.SelectedItem as DataRowView;
		if (selectedRow == null || !list2.Any((DataRowView row) => row.Row == selectedRow.Row))
		{
			mainGrid.SelectedItem = null;
			UpdateSelectionState(null);
		}
		else
		{
			UpdateSelectionState(selectedRow);
		}
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateSelectionState(mainGrid.SelectedItem as DataRowView);
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.UnselectAll();
		UpdateSelectionState(null);
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnIssue_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select a request to release first.");
			return;
		}
		string text = Convert.ToString(dataRowView["status"]) ?? "SUBMITTED";
		if (text.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
		{
			DialogService.Instance.ShowWarning("Cancelled requests cannot be released.");
			return;
		}
		if (text.Equals("RELEASED", StringComparison.OrdinalIgnoreCase))
		{
			DialogService.Instance.ShowInfo("This document has already been released.");
			return;
		}
		CertificationWindow window = new CertificationWindow(Convert.ToInt32(dataRowView["doc_request_id"]), CertificateDialogMode.Issue, loadExistingRequest: true);
		if (DialogService.Instance.ShowDialog(window) == true)
		{
			await LoadAsync();
		}
	}

	private async void BtnCancel_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select a request to cancel first.");
			return;
		}
		string text = Convert.ToString(dataRowView["status"]) ?? "SUBMITTED";
		if (text.Equals("RELEASED", StringComparison.OrdinalIgnoreCase))
		{
			DialogService.Instance.ShowWarning("Released documents can no longer be cancelled.");
			return;
		}
		if (text.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
		{
			DialogService.Instance.ShowInfo("This request is already cancelled.");
			return;
		}
		string text2 = Convert.ToString(dataRowView["tracking_code"]) ?? "the selected request";
		if (!DialogService.Instance.Confirm("Cancel " + text2 + " from the clearance queue?", "Cancel Request"))
		{
			return;
		}
		try
		{
			await _certificateRequestService.CancelRequestAsync(Convert.ToInt32(dataRowView["doc_request_id"]), text2);
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to cancel certificate request.", ex);
			DialogService.Instance.ShowError("Could not cancel the selected request.");
		}
	}

	private async void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		CertificationWindow window = new CertificationWindow(CertificateDialogMode.Request);
		if (DialogService.Instance.ShowDialog(window) == true)
		{
			await LoadAsync();
		}
	}

	private void BtnVerify_Click(object sender, RoutedEventArgs e)
	{
		DocumentVerificationWindow window = new DocumentVerificationWindow();
		DialogService.Instance.ShowDialog(window);
	}

	private void BtnVerifySelected_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select a request to open its verification details.");
			return;
		}
		DocumentVerificationWindow window = new DocumentVerificationWindow(Convert.ToInt32(dataRowView["doc_request_id"]));
		DialogService.Instance.ShowDialog(window);
	}

	private void UpdateSelectionState(DataRowView? row)
	{
		if (row == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			tableSelectionLabel.Text = "No selection";
			selectedRecordLabel.Text = "REQ-000001";
			selectedRecordMetaLabel.Text = "Select a queue record to manage release or cancellation.";
			selectedVerificationLabel.Text = "Verification details appear here once a request is selected.";
			btnReleaseLabel.Text = "Release and Print";
			btnRelease.IsEnabled = false;
			btnCancel.IsEnabled = false;
			btnVerifySelected.IsEnabled = false;
			return;
		}
		string text = Convert.ToString(row["status"]) ?? "SUBMITTED";
		string text2 = Convert.ToString(row["tracking_code"]) ?? "Unknown Request";
		string value = Convert.ToString(row["resident_name"]) ?? "Resident";
		string value2 = Convert.ToString(row["certification_type"]) ?? "Certificate";
		string value3 = Convert.ToString(row["requested_on"]) ?? "Unknown request time";
		string documentNo = Convert.ToString(row["document_no"]) ?? string.Empty;
		string verificationToken = Convert.ToString(row["verification_token"]) ?? string.Empty;
		string expiresOn = Convert.ToString(row["expires_on"]) ?? string.Empty;
		selectedRecordLabel.Text = text2;
		selectedRecordMetaLabel.Text = $"{value} | {value2} | Requested {value3}";
		selectedVerificationLabel.Text = BuildVerificationSummary(text, documentNo, verificationToken, expiresOn);
		tableSelectionLabel.Text = "Selected: " + text2;
		btnReleaseLabel.Text = (text.Equals("RELEASED", StringComparison.OrdinalIgnoreCase) ? "Already Released" : (text.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) ? "Release Disabled" : "Release and Print"));
		btnRelease.IsEnabled = !text.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) && !text.Equals("RELEASED", StringComparison.OrdinalIgnoreCase);
		btnCancel.IsEnabled = !text.Equals("RELEASED", StringComparison.OrdinalIgnoreCase) && !text.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);
		btnVerifySelected.IsEnabled = true;
		contextActionBar.Visibility = Visibility.Visible;
	}

	private static void EnrichDisplayTable(DataTable table)
	{
		EnsureStringColumn(table, "purpose_display");
		EnsureStringColumn(table, "released_on_display");
		EnsureStringColumn(table, "request_meta");
		EnsureStringColumn(table, "resident_meta");
		EnsureStringColumn(table, "fee_display");
		EnsureStringColumn(table, "or_number_display");
		EnsureStringColumn(table, "status_display");
		EnsureStringColumn(table, "status_meta");
		foreach (DataRow row in table.Rows)
		{
			string text = Convert.ToString(row["purpose"]) ?? string.Empty;
			string text2 = Convert.ToString(row["released_on"]) ?? string.Empty;
			string text3 = Convert.ToString(row["or_number"]) ?? string.Empty;
			string status = Convert.ToString(row["status"]) ?? "SUBMITTED";
			int value = ((row["doc_request_id"] != DBNull.Value) ? Convert.ToInt32(row["doc_request_id"], CultureInfo.InvariantCulture) : 0);
			int num = ((row["resident_id"] != DBNull.Value) ? Convert.ToInt32(row["resident_id"], CultureInfo.InvariantCulture) : 0);
			row["purpose_display"] = (string.IsNullOrWhiteSpace(text) ? "No purpose provided." : text.Trim());
			row["released_on_display"] = (string.IsNullOrWhiteSpace(text2) ? "Awaiting release" : text2.Trim());
			row["request_meta"] = (string.IsNullOrWhiteSpace(text3) ? $"Request #{value}" : ("OR " + text3.Trim()));
			row["resident_meta"] = ((num > 0) ? $"Resident ID #{num}" : "Resident record");
			row["fee_display"] = FormatCurrency(row["fee"]);
			row["or_number_display"] = (string.IsNullOrWhiteSpace(text3) ? "No OR number recorded" : ("OR " + text3.Trim()));
			row["status_display"] = ToStatusLabel(status);
			row["status_meta"] = GetStatusMeta(status);
		}
	}

	private static void EnsureStringColumn(DataTable table, string columnName)
	{
		if (!table.Columns.Contains(columnName))
		{
			table.Columns.Add(columnName, typeof(string));
		}
	}

	private static bool IsPendingStatus(string? status)
	{
		if (!string.Equals(status, "SUBMITTED", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool IsStatus(DataRowView row, string status)
	{
		return string.Equals(Convert.ToString(row["status"]), status, StringComparison.OrdinalIgnoreCase);
	}

	private static string ToStatusLabel(string? status)
	{
		string text = (string.IsNullOrWhiteSpace(status) ? "SUBMITTED" : status.Trim());
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
	}

	private static string GetStatusMeta(string? status)
	{
		if (string.IsNullOrWhiteSpace(status))
		{
			return "Awaiting queue review.";
		}
		return status.Trim().ToUpperInvariant() switch
		{
			"SUBMITTED" => "Queued for review and release preparation.", 
			"APPROVED" => "Approved and ready for release handling.", 
			"RELEASED" => "Completed, printed, and released.", 
			"REJECTED" => "Needs follow-up or request correction.", 
			"CANCELLED" => "Closed and removed from active processing.", 
			_ => "Being tracked in the clearance queue.", 
		};
	}

	private static string FormatCurrency(object value)
	{
		if (value == DBNull.Value)
		{
			return "PHP 0.00";
		}
		decimal value2 = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		return $"PHP {value2:N2}";
	}

	private static string EscapeForRowFilter(string value)
	{
		return value.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]")
			.Replace("*", "[*]");
	}

	private static string BuildVerificationSummary(string status, string documentNo, string verificationToken, string expiresOn)
	{
		if (!string.Equals(status, "RELEASED", StringComparison.OrdinalIgnoreCase))
		{
			return "Verification QR becomes available once the request is released.";
		}
		string value = (string.IsNullOrWhiteSpace(documentNo) ? "Released without a stored document number." : ("Document No: " + documentNo.Trim()));
		string value2 = (string.IsNullOrWhiteSpace(verificationToken) ? "No verification token saved." : "Verification token and QR payload are available.");
		string value3 = (string.IsNullOrWhiteSpace(expiresOn) ? "No expiry recorded." : ("Expires " + expiresOn.Trim() + "."));
		return $"{value} | {value2} | {value3}";
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/clearancespage.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			recordCountLabel = (TextBlock)target;
			break;
		case 2:
			visibleQueueMetric = (TextBlock)target;
			break;
		case 3:
			pendingQueueMetric = (TextBlock)target;
			break;
		case 4:
			releasedQueueMetric = (TextBlock)target;
			break;
		case 5:
			attentionQueueMetric = (TextBlock)target;
			break;
		case 6:
			((Button)target).Click += BtnVerify_Click;
			break;
		case 7:
			((Button)target).Click += BtnAdd_Click;
			break;
		case 8:
			searchBox = (TextBox)target;
			searchBox.TextChanged += SearchBox_TextChanged;
			break;
		case 9:
			typeFilter = (ComboBox)target;
			typeFilter.SelectionChanged += Filter_SelectionChanged;
			break;
		case 10:
			statusFilter = (ComboBox)target;
			statusFilter.SelectionChanged += Filter_SelectionChanged;
			break;
		case 11:
			((Button)target).Click += BtnRefresh_Click;
			break;
		case 12:
			tableMetaLabel = (TextBlock)target;
			break;
		case 13:
			tableVisibleLabel = (TextBlock)target;
			break;
		case 14:
			tableSelectionLabel = (TextBlock)target;
			break;
		case 15:
			mainGrid = (DataGrid)target;
			mainGrid.SelectionChanged += MainGrid_SelectionChanged;
			break;
		case 16:
			emptyState = (StackPanel)target;
			break;
		case 17:
			emptyLabel = (TextBlock)target;
			break;
		case 18:
			contextActionBar = (Border)target;
			break;
		case 19:
			selectedRecordLabel = (TextBlock)target;
			break;
		case 20:
			selectedRecordMetaLabel = (TextBlock)target;
			break;
		case 21:
			selectedVerificationLabel = (TextBlock)target;
			break;
		case 22:
			btnRelease = (Button)target;
			btnRelease.Click += BtnIssue_Click;
			break;
		case 23:
			btnReleaseLabel = (TextBlock)target;
			break;
		case 24:
			btnCancel = (Button)target;
			btnCancel.Click += BtnCancel_Click;
			break;
		case 25:
			btnVerifySelected = (Button)target;
			btnVerifySelected.Click += BtnVerifySelected_Click;
			break;
		case 26:
			((Button)target).Click += BtnClearSelection_Click;
			break;
		case 27:
			footerCountLabel = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
