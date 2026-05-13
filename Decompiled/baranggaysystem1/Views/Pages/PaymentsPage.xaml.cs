using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;

namespace baranggaysystem1.Views.Pages;

public partial class PaymentsPage : UserControl
{
	private readonly PaymentLedgerService _paymentLedgerService = new PaymentLedgerService();

	private DataTable? _data;













	public PaymentsPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public PaymentsPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		try
		{
			_data = await _paymentLedgerService.GetLedgerAsync();
			ApplyDataToGrid(_data);
			PopulateFilterOptions(_data);
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("PaymentsPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load payment data. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Unable to load ledger.";
			recordCountLabel.Text = "Ledger unavailable";
		}
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		if (table == null || table.Rows.Count == 0)
		{
			mainGrid.ItemsSource = null;
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "No payment records found.";
			recordCountLabel.Text = "No transactions.";
			return;
		}
		emptyState.Visibility = Visibility.Collapsed;
		mainGrid.Columns.Clear();
		(string, string, double)[] array = new(string, string, double)[7]
		{
			("OR No", "or_no", 120.0),
			("Resident", "resident_name", 190.0),
			("Item", "item_type", 150.0),
			("Amount", "amount", 120.0),
			("Method", "payment_method", 110.0),
			("Status", "payment_status", 95.0),
			("Date Paid", "paid_at", 160.0)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (header, path, value) = array[i];
			mainGrid.Columns.Add(new DataGridTextColumn
			{
				Header = header,
				Binding = new Binding(path),
				Width = new DataGridLength(value, DataGridLengthUnitType.Auto)
			});
		}
		mainGrid.ItemsSource = table.DefaultView;
	}

	private void PopulateFilterOptions(DataTable? table)
	{
		typeFilter.Items.Clear();
		typeFilter.Items.Add("All Types");
		methodFilter.Items.Clear();
		methodFilter.Items.Add("All Methods");
		if (table != null)
		{
			foreach (string item in from value in (from row in table.AsEnumerable()
					select Convert.ToString(row["item_type"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				typeFilter.Items.Add(item);
			}
			foreach (string item2 in from value in (from row in table.AsEnumerable()
					select Convert.ToString(row["payment_method"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				methodFilter.Items.Add(item2);
			}
		}
		typeFilter.SelectedIndex = 0;
		methodFilter.SelectedIndex = 0;
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
			string[] source = new string[6] { "or_no", "resident_name", "item_type", "amount", "payment_method", "paid_at" };
			list.Add("(" + string.Join(" OR ", source.Select((string column) => $"Convert([{column}], 'System.String') LIKE '%{escaped}%'")) + ")");
		}
		if (typeFilter.SelectedItem is string text && !string.Equals(text, "All Types", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[item_type] = '" + EscapeForRowFilter(text) + "'");
		}
		if (methodFilter.SelectedItem is string text2 && !string.Equals(text2, "All Methods", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[payment_method] = '" + EscapeForRowFilter(text2) + "'");
		}
		_data.DefaultView.RowFilter = string.Join(" AND ", list);
		int count = _data.DefaultView.Count;
		emptyState.Visibility = ((count != 0) ? Visibility.Collapsed : Visibility.Visible);
		emptyLabel.Text = ((count == 0) ? "No transactions match the current filters." : "No payment history found for current filters.");
		footerCountLabel.Text = $"Showing {count:N0} transaction(s)";
		recordCountLabel.Text = $"{count:N0} payment transaction(s)";
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			return;
		}
		contextActionBar.Visibility = Visibility.Visible;
		selectedRecordLabel.Text = Convert.ToString(dataRowView["or_no"]) ?? "Unknown OR";
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.SelectedItem = null;
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		PaymentWindow window = new PaymentWindow();
		if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
		{
			await LoadAsync();
		}
	}

	private void BtnPrintOR_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select a payment first.");
			return;
		}
		DialogService.Instance.ShowInfo($"Official Receipt: {dataRowView["or_no"]}\nResident: {dataRowView["resident_name"]}\nAmount: {dataRowView["amount"]}\nPaid: {dataRowView["paid_at"]}", "Payment Receipt");
	}

	private void BtnView_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select a payment row first.");
			return;
		}
		DialogService.Instance.ShowInfo($"OR No: {dataRowView["or_no"]}\nResident: {dataRowView["resident_name"]}\nItem: {dataRowView["item_type"]}\nAmount: {dataRowView["amount"]}\nMethod: {dataRowView["payment_method"]}\nDate Paid: {dataRowView["paid_at"]}\nReference: {dataRowView["document_no"]}\n\n{dataRowView["remarks"]}", "Payment Details");
	}

	private void BtnVoid_Click(object sender, RoutedEventArgs e)
	{
		DialogService.Instance.ShowWarning("Payment entries are immutable in the current ledger flow, so voiding is not available.");
	}

	private static string EscapeForRowFilter(string value)
	{
		return value.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]")
			.Replace("*", "[*]");
	}}
