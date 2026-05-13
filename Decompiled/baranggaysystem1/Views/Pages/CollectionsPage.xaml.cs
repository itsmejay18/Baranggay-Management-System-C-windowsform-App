using System;
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
using baranggaysystem1.helper;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;

namespace baranggaysystem1.Views.Pages;

public partial class CollectionsPage : UserControl
{
	private enum FinanceSection
	{
		Expenses,
		Inventory,
		Assets
	}

	private readonly FinanceOperationsService _financeService = new FinanceOperationsService();

	private DataTable? _expenseData;

	private DataTable? _inventoryData;

	private DataTable? _assetData;

	private FinanceSection _activeSection;

	private bool _isUpdatingFilters;




























	public CollectionsPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public CollectionsPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		try
		{
			Task<DataTable> expenseTask = _financeService.GetExpenseLedgerAsync();
			Task<DataTable> inventoryTask = _financeService.GetInventoryLedgerAsync();
			Task<DataTable> assetTask = _financeService.GetAssetLedgerAsync();
			await Task.WhenAll<DataTable>(expenseTask, inventoryTask, assetTask);
			_expenseData = expenseTask.Result;
			_inventoryData = inventoryTask.Result;
			_assetData = assetTask.Result;
			EnrichExpenseTable(_expenseData);
			EnrichInventoryTable(_inventoryData);
			EnrichAssetTable(_assetData);
			expenseGrid.ItemsSource = _expenseData.DefaultView;
			inventoryGrid.ItemsSource = _inventoryData.DefaultView;
			assetGrid.ItemsSource = _assetData.DefaultView;
			UpdateSectionChrome();
			UpdateMetrics();
			PopulateFilterOptions();
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("CollectionsPage load failed.", ex);
			expenseGrid.ItemsSource = null;
			inventoryGrid.ItemsSource = null;
			assetGrid.ItemsSource = null;
			expenseEmptyLabel.Text = "Failed to load expense entries.";
			inventoryEmptyLabel.Text = "Failed to load inventory records.";
			assetEmptyLabel.Text = "Failed to load asset records.";
			expenseEmptyState.Visibility = Visibility.Visible;
			inventoryEmptyState.Visibility = Visibility.Visible;
			assetEmptyState.Visibility = Visibility.Visible;
			totalExpenseMetric.Text = "PHP 0.00";
			pendingExpenseMetric.Text = "0";
			inventoryValueMetric.Text = "PHP 0.00";
			lowStockMetric.Text = "0";
			activeAssetMetric.Text = "PHP 0.00";
			recordCountLabel.Text = "Finance workspace is temporarily unavailable.";
			footerCountLabel.Text = "Unable to load finance records.";
			footerHintLabel.Text = "Refresh after checking database connectivity.";
			contextActionBar.Visibility = Visibility.Collapsed;
		}
	}

	private void UpdateMetrics()
	{
		decimal amount = SumVisibleOrAll(_expenseData, "amount");
		int num = CountRows(_expenseData, (DataRow row) => string.Equals(Convert.ToString(row["status"]), "PENDING", StringComparison.OrdinalIgnoreCase));
		decimal amount2 = SumVisibleOrAll(_inventoryData, "stock_value");
		int num2 = CountRows(_inventoryData, (DataRow row) => string.Equals(Convert.ToString(row["stock_state"]), "LOW STOCK", StringComparison.OrdinalIgnoreCase) || string.Equals(Convert.ToString(row["stock_state"]), "OUT OF STOCK", StringComparison.OrdinalIgnoreCase));
		decimal amount3 = SumRows(_assetData, (DataRow row) => string.Equals(Convert.ToString(row["lifecycle_status"]), "ACTIVE", StringComparison.OrdinalIgnoreCase), "acquisition_cost");
		totalExpenseMetric.Text = FormatCurrency(amount);
		pendingExpenseMetric.Text = num.ToString("N0", CultureInfo.InvariantCulture);
		inventoryValueMetric.Text = FormatCurrency(amount2);
		lowStockMetric.Text = num2.ToString("N0", CultureInfo.InvariantCulture);
		activeAssetMetric.Text = FormatCurrency(amount3);
	}

	private void UpdateSectionChrome()
	{
		switch (_activeSection)
		{
		case FinanceSection.Inventory:
			sectionBadgeText.Text = "Inventory Stock";
			btnAddRecord.Content = "Add Inventory Item";
			btnEditSelected.Content = "Edit Item";
			footerHintLabel.Text = "Monitor stock levels and reorder points for barangay supplies.";
			break;
		case FinanceSection.Assets:
			sectionBadgeText.Text = "Assets Registry";
			btnAddRecord.Content = "Register Asset";
			btnEditSelected.Content = "Edit Asset";
			footerHintLabel.Text = "Keep the barangay asset registry current for inspection and audit readiness.";
			break;
		default:
			sectionBadgeText.Text = "Expense Ledger";
			btnAddRecord.Content = "Record Expense";
			btnEditSelected.Content = "Edit Expense";
			footerHintLabel.Text = "Record outgoing disbursements, reimbursements, and operational expenses.";
			break;
		}
	}

	private void PopulateFilterOptions()
	{
		_isUpdatingFilters = true;
		try
		{
			categoryFilter.Items.Clear();
			categoryFilter.Items.Add("All Categories");
			statusFilter.Items.Clear();
			statusFilter.Items.Add(GetAllStatusLabel());
			DataTable activeData = GetActiveData();
			if (activeData != null)
			{
				foreach (string item in from value in (from row in activeData.AsEnumerable()
						select Convert.ToString(row[GetCategoryColumn()]) ?? string.Empty into value
						where !string.IsNullOrWhiteSpace(value)
						select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
					orderby value
					select value)
				{
					categoryFilter.Items.Add(item);
				}
				foreach (string item2 in from value in (from row in activeData.AsEnumerable()
						select Convert.ToString(row[GetStatusDisplayColumn()]) ?? string.Empty into value
						where !string.IsNullOrWhiteSpace(value)
						select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
					orderby value
					select value)
				{
					statusFilter.Items.Add(item2);
				}
			}
			categoryFilter.SelectedIndex = 0;
			statusFilter.SelectedIndex = 0;
		}
		finally
		{
			_isUpdatingFilters = false;
		}
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && !_isUpdatingFilters)
		{
			ApplyFilters();
		}
	}

	private void FinanceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && e.Source == financeTabs)
		{
			_activeSection = financeTabs.SelectedIndex switch
			{
				1 => FinanceSection.Inventory, 
				2 => FinanceSection.Assets, 
				_ => FinanceSection.Expenses, 
			};
			UpdateSectionChrome();
			PopulateFilterOptions();
			ApplyFilters();
		}
	}

	private void ApplyFilters()
	{
		DataTable activeData = GetActiveData();
		if (activeData == null)
		{
			return;
		}
		List<string> list = new List<string>();
		string value = searchBox.Text.Trim();
		if (!string.IsNullOrWhiteSpace(value))
		{
			string escaped = EscapeForRowFilter(value);
			list.Add("(" + string.Join(" OR ", from column in GetSearchColumns()
				select $"Convert([{column}], 'System.String') LIKE '%{escaped}%'") + ")");
		}
		if (categoryFilter.SelectedItem is string text && !string.Equals(text, "All Categories", StringComparison.OrdinalIgnoreCase))
		{
			list.Add($"[{GetCategoryColumn()}] = '{EscapeForRowFilter(text)}'");
		}
		if (statusFilter.SelectedItem is string text2 && !string.Equals(text2, GetAllStatusLabel(), StringComparison.OrdinalIgnoreCase))
		{
			list.Add($"[{GetStatusDisplayColumn()}] = '{EscapeForRowFilter(text2)}'");
		}
		activeData.DefaultView.RowFilter = string.Join(" AND ", list);
		UpdateEmptyStates();
		DataGrid activeGrid = GetActiveGrid();
		DataRowView selectedRow = activeGrid.SelectedItem as DataRowView;
		if (selectedRow == null || !activeData.DefaultView.Cast<DataRowView>().Any((DataRowView view) => view.Row == selectedRow.Row))
		{
			activeGrid.UnselectAll();
			UpdateSelectionState(null);
		}
		else
		{
			UpdateSelectionState(selectedRow);
		}
		int count = activeData.DefaultView.Count;
		int count2 = activeData.Rows.Count;
		TextBlock textBlock = recordCountLabel;
		textBlock.Text = _activeSection switch
		{
			FinanceSection.Inventory => (count == count2) ? $"{count:N0} inventory item(s) loaded." : $"{count:N0} of {count2:N0} inventory item(s) match the current filters.", 
			FinanceSection.Assets => (count == count2) ? $"{count:N0} asset record(s) loaded." : $"{count:N0} of {count2:N0} asset record(s) match the current filters.", 
			_ => (count == count2) ? $"{count:N0} expense entr{((count == 1) ? "y" : "ies")} loaded." : $"{count:N0} of {count2:N0} expense entr{((count2 == 1) ? "y" : "ies")} match the current filters.", 
		};
		textBlock = footerCountLabel;
		textBlock.Text = _activeSection switch
		{
			FinanceSection.Inventory => $"Showing {count:N0} inventory item(s)", 
			FinanceSection.Assets => $"Showing {count:N0} asset record(s)", 
			_ => $"Showing {count:N0} expense entr{((count == 1) ? "y" : "ies")}", 
		};
	}

	private void UpdateEmptyStates()
	{
		UpdateEmptyState(_expenseData, expenseEmptyState, expenseEmptyLabel, "No expense entries found.", "No expense entries match the current filters.");
		UpdateEmptyState(_inventoryData, inventoryEmptyState, inventoryEmptyLabel, "No inventory items found.", "No inventory items match the current filters.");
		UpdateEmptyState(_assetData, assetEmptyState, assetEmptyLabel, "No asset records found.", "No asset records match the current filters.");
	}

	private static void UpdateEmptyState(DataTable? table, UIElement stateElement, TextBlock label, string noDataMessage, string noMatchMessage)
	{
		if (table == null)
		{
			stateElement.Visibility = Visibility.Visible;
			label.Text = noDataMessage;
		}
		else if (table.Rows.Count == 0)
		{
			stateElement.Visibility = Visibility.Visible;
			label.Text = noDataMessage;
		}
		else if (table.DefaultView.Count == 0)
		{
			stateElement.Visibility = Visibility.Visible;
			label.Text = noMatchMessage;
		}
		else
		{
			stateElement.Visibility = Visibility.Collapsed;
			label.Text = noDataMessage;
		}
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		_ = 5;
		try
		{
			switch (_activeSection)
			{
			case FinanceSection.Inventory:
			{
				InventoryItemWindow inventoryItemWindow = new InventoryItemWindow();
				if (DialogService.Instance.ShowDialog(inventoryItemWindow).GetValueOrDefault())
				{
					await _financeService.SaveInventoryItemAsync(inventoryItemWindow.Draft);
					DialogService.Instance.ShowInfo("Inventory item saved successfully.");
					await LoadAsync();
				}
				break;
			}
			case FinanceSection.Assets:
			{
				AssetRecordWindow assetRecordWindow = new AssetRecordWindow();
				if (DialogService.Instance.ShowDialog(assetRecordWindow).GetValueOrDefault())
				{
					await _financeService.SaveAssetAsync(assetRecordWindow.Draft);
					DialogService.Instance.ShowInfo("Asset record saved successfully.");
					await LoadAsync();
				}
				break;
			}
			default:
			{
				ExpenseEntryWindow expenseEntryWindow = new ExpenseEntryWindow();
				if (DialogService.Instance.ShowDialog(expenseEntryWindow).GetValueOrDefault())
				{
					await _financeService.SaveExpenseAsync(expenseEntryWindow.Draft);
					DialogService.Instance.ShowInfo("Expense entry saved successfully.");
					await LoadAsync();
				}
				break;
			}
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Finance record creation failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
		}
	}

	private async void BtnEditSelected_Click(object sender, RoutedEventArgs e)
	{
		if (!(GetActiveGrid().SelectedItem is DataRowView row))
		{
			DialogService.Instance.ShowWarning("Select a record to edit first.");
			return;
		}
		try
		{
			switch (_activeSection)
			{
			case FinanceSection.Inventory:
			{
				InventoryItemWindow inventoryItemWindow = new InventoryItemWindow(ToInventoryRecord(row));
				if (DialogService.Instance.ShowDialog(inventoryItemWindow).GetValueOrDefault())
				{
					await _financeService.SaveInventoryItemAsync(inventoryItemWindow.Draft);
					DialogService.Instance.ShowInfo("Inventory item updated successfully.");
					await LoadAsync();
				}
				break;
			}
			case FinanceSection.Assets:
			{
				AssetRecordWindow assetRecordWindow = new AssetRecordWindow(ToAssetRecord(row));
				if (DialogService.Instance.ShowDialog(assetRecordWindow).GetValueOrDefault())
				{
					await _financeService.SaveAssetAsync(assetRecordWindow.Draft);
					DialogService.Instance.ShowInfo("Asset record updated successfully.");
					await LoadAsync();
				}
				break;
			}
			default:
			{
				ExpenseEntryWindow expenseEntryWindow = new ExpenseEntryWindow(ToExpenseRecord(row));
				if (DialogService.Instance.ShowDialog(expenseEntryWindow).GetValueOrDefault())
				{
					await _financeService.SaveExpenseAsync(expenseEntryWindow.Draft);
					DialogService.Instance.ShowInfo("Expense entry updated successfully.");
					await LoadAsync();
				}
				break;
			}
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Finance record update failed.", ex);
			DialogService.Instance.ShowError(ex.Message);
		}
	}

	private void BtnViewSelected_Click(object sender, RoutedEventArgs e)
	{
		if (!(GetActiveGrid().SelectedItem is DataRowView row))
		{
			DialogService.Instance.ShowWarning("Select a record first.");
		}
		else
		{
			DialogService.Instance.ShowInfo(BuildDetailMessage(row), "Finance Record Details");
		}
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		GetActiveGrid().UnselectAll();
		UpdateSelectionState(null);
	}

	private void ExpenseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_activeSection == FinanceSection.Expenses)
		{
			UpdateSelectionState(expenseGrid.SelectedItem as DataRowView);
		}
	}

	private void InventoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_activeSection == FinanceSection.Inventory)
		{
			UpdateSelectionState(inventoryGrid.SelectedItem as DataRowView);
		}
	}

	private void AssetGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_activeSection == FinanceSection.Assets)
		{
			UpdateSelectionState(assetGrid.SelectedItem as DataRowView);
		}
	}

	private void UpdateSelectionState(DataRowView? row)
	{
		if (row == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			selectedRecordLabel.Text = "No record selected";
			selectedRecordMetaLabel.Text = "Select a finance record to view details or edit it.";
			return;
		}
		switch (_activeSection)
		{
		case FinanceSection.Inventory:
			selectedRecordLabel.Text = Convert.ToString(row["item_name"]) ?? "Inventory Item";
			selectedRecordMetaLabel.Text = $"{Convert.ToString(row["stock_state_display"]) ?? "Stock"} | {Convert.ToString(row["quantity_display"]) ?? "0"} | {Convert.ToString(row["stock_value_display"]) ?? "PHP 0.00"}";
			break;
		case FinanceSection.Assets:
			selectedRecordLabel.Text = Convert.ToString(row["asset_name"]) ?? "Asset Record";
			selectedRecordMetaLabel.Text = $"{Convert.ToString(row["condition_status_display"]) ?? "Condition"} | {Convert.ToString(row["lifecycle_status_display"]) ?? "Lifecycle"} | {Convert.ToString(row["acquisition_cost_display"]) ?? "PHP 0.00"}";
			break;
		default:
			selectedRecordLabel.Text = Convert.ToString(row["expense_title"]) ?? "Expense Entry";
			selectedRecordMetaLabel.Text = $"{Convert.ToString(row["expense_category"]) ?? "Category"} | {Convert.ToString(row["amount_display"]) ?? "PHP 0.00"} | {Convert.ToString(row["status_display"]) ?? "Status"}";
			break;
		}
		contextActionBar.Visibility = Visibility.Visible;
	}

	private string BuildDetailMessage(DataRowView row)
	{
		return _activeSection switch
		{
			FinanceSection.Inventory => $"Item: {row["item_name"]}\nCategory: {row["category"]}\nQuantity: {row["quantity_display"]}\nReorder Level: {row["reorder_display"]}\nUnit Cost: {row["unit_cost_display"]}\nStock Value: {row["stock_value_display"]}\nStock State: {row["stock_state_display"]}\nRecord Status: {row["item_status_display"]}\nLocation: {row["location_display"]}\nLast Restocked: {row["last_restocked_display"]}\n\n{row["notes_display"]}", 
			FinanceSection.Assets => $"Asset: {row["asset_name"]}\nCategory: {row["asset_category"]}\nAsset Tag: {row["asset_tag_display"]}\nAcquisition Date: {row["acquisition_date_display"]}\nAcquisition Cost: {row["acquisition_cost_display"]}\nCondition: {row["condition_status_display"]}\nLifecycle: {row["lifecycle_status_display"]}\nLocation: {row["assigned_location_display"]}\nCustodian: {row["custodian_name_display"]}\n\n{row["notes_display"]}", 
			_ => $"Expense: {row["expense_title"]}\nDate: {row["expense_date_display"]}\nCategory: {row["expense_category"]}\nPayee: {row["payee_display"]}\nAmount: {row["amount_display"]}\nPayment Method: {row["payment_method_display"]}\nStatus: {row["status_display"]}\nReference: {row["reference_display"]}\n\n{row["notes_display"]}", 
		};
	}

	private DataTable? GetActiveData()
	{
		return _activeSection switch
		{
			FinanceSection.Inventory => _inventoryData, 
			FinanceSection.Assets => _assetData, 
			_ => _expenseData, 
		};
	}

	private DataGrid GetActiveGrid()
	{
		return _activeSection switch
		{
			FinanceSection.Inventory => inventoryGrid, 
			FinanceSection.Assets => assetGrid, 
			_ => expenseGrid, 
		};
	}

	private string[] GetSearchColumns()
	{
		return _activeSection switch
		{
			FinanceSection.Inventory => new string[11]
			{
				"item_name", "category", "quantity_display", "reorder_display", "unit_cost_display", "stock_value_display", "stock_state_display", "location_display", "item_status_display", "last_restocked_display",
				"notes_display"
			}, 
			FinanceSection.Assets => new string[10] { "asset_name", "asset_category", "asset_tag_display", "acquisition_date_display", "acquisition_cost_display", "condition_status_display", "lifecycle_status_display", "assigned_location_display", "custodian_name_display", "notes_display" }, 
			_ => new string[9] { "expense_date_display", "expense_title", "expense_category", "payee_display", "amount_display", "payment_method_display", "status_display", "reference_display", "notes_display" }, 
		};
	}

	private string GetCategoryColumn()
	{
		return _activeSection switch
		{
			FinanceSection.Inventory => "category", 
			FinanceSection.Assets => "asset_category", 
			_ => "expense_category", 
		};
	}

	private string GetStatusDisplayColumn()
	{
		return _activeSection switch
		{
			FinanceSection.Inventory => "stock_state_display", 
			FinanceSection.Assets => "lifecycle_status_display", 
			_ => "status_display", 
		};
	}

	private string GetAllStatusLabel()
	{
		return _activeSection switch
		{
			FinanceSection.Inventory => "All Stock States", 
			FinanceSection.Assets => "All Lifecycle Status", 
			_ => "All Expense Status", 
		};
	}

	private static void EnrichExpenseTable(DataTable table)
	{
		EnsureStringColumn(table, "amount_display");
		EnsureStringColumn(table, "payment_method_display");
		EnsureStringColumn(table, "status_display");
		EnsureStringColumn(table, "payee_display");
		EnsureStringColumn(table, "reference_display");
		EnsureStringColumn(table, "notes_display");
		foreach (DataRow row in table.Rows)
		{
			row["amount_display"] = FormatCurrency(ReadDecimal(row, "amount"));
			row["payment_method_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["payment_method"])) ? "Cash" : Convert.ToString(row["payment_method"]));
			row["status_display"] = ToTitleCase(Convert.ToString(row["status"]) ?? "POSTED");
			row["payee_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["payee_name"])) ? "No payee listed" : Convert.ToString(row["payee_name"]));
			row["reference_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["reference_no"])) ? "No reference" : Convert.ToString(row["reference_no"]));
			row["notes_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["notes"])) ? "No notes recorded." : Convert.ToString(row["notes"]));
		}
	}

	private static void EnrichInventoryTable(DataTable table)
	{
		EnsureStringColumn(table, "quantity_display");
		EnsureStringColumn(table, "reorder_display");
		EnsureStringColumn(table, "unit_cost_display");
		EnsureStringColumn(table, "stock_value_display");
		EnsureStringColumn(table, "stock_state_display");
		EnsureStringColumn(table, "location_display");
		EnsureStringColumn(table, "item_status_display");
		EnsureStringColumn(table, "notes_display");
		foreach (DataRow row in table.Rows)
		{
			string value = Convert.ToString(row["unit"]) ?? "pcs";
			row["quantity_display"] = $"{ReadDecimal(row, "quantity_on_hand"):N2} {value}";
			row["reorder_display"] = $"{ReadDecimal(row, "reorder_level"):N2} {value}";
			row["unit_cost_display"] = FormatCurrency(ReadDecimal(row, "unit_cost"));
			row["stock_value_display"] = FormatCurrency(ReadDecimal(row, "stock_value"));
			row["stock_state_display"] = ToTitleCase(Convert.ToString(row["stock_state"]) ?? "IN STOCK");
			row["location_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["location"])) ? "No location assigned" : Convert.ToString(row["location"]));
			row["item_status_display"] = ToTitleCase(Convert.ToString(row["item_status"]) ?? "ACTIVE");
			row["notes_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["notes"])) ? "No notes recorded." : Convert.ToString(row["notes"]));
		}
	}

	private static void EnrichAssetTable(DataTable table)
	{
		EnsureStringColumn(table, "asset_tag_display");
		EnsureStringColumn(table, "acquisition_cost_display");
		EnsureStringColumn(table, "condition_status_display");
		EnsureStringColumn(table, "lifecycle_status_display");
		EnsureStringColumn(table, "assigned_location_display");
		EnsureStringColumn(table, "custodian_name_display");
		EnsureStringColumn(table, "notes_display");
		foreach (DataRow row in table.Rows)
		{
			row["asset_tag_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["asset_tag"])) ? "No tag" : Convert.ToString(row["asset_tag"]));
			row["acquisition_cost_display"] = FormatCurrency(ReadDecimal(row, "acquisition_cost"));
			row["condition_status_display"] = ToTitleCase(Convert.ToString(row["condition_status"]) ?? "GOOD");
			row["lifecycle_status_display"] = ToTitleCase(Convert.ToString(row["lifecycle_status"]) ?? "ACTIVE");
			row["assigned_location_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["assigned_location"])) ? "No location assigned" : Convert.ToString(row["assigned_location"]));
			row["custodian_name_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["custodian_name"])) ? "No custodian listed" : Convert.ToString(row["custodian_name"]));
			row["notes_display"] = (string.IsNullOrWhiteSpace(Convert.ToString(row["notes"])) ? "No notes recorded." : Convert.ToString(row["notes"]));
		}
	}

	private static void EnsureStringColumn(DataTable table, string columnName)
	{
		if (!table.Columns.Contains(columnName))
		{
			table.Columns.Add(columnName, typeof(string));
		}
	}

	private static decimal ReadDecimal(DataRow row, string columnName)
	{
		if (row[columnName] != DBNull.Value)
		{
			return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
		}
		return 0m;
	}

	private static decimal SumVisibleOrAll(DataTable? table, string columnName)
	{
		string columnName2 = columnName;
		return table?.AsEnumerable().Sum((DataRow row) => ReadDecimal(row, columnName2)) ?? 0m;
	}

	private static decimal SumRows(DataTable? table, Func<DataRow, bool> predicate, string columnName)
	{
		string columnName2 = columnName;
		return table?.AsEnumerable().Where(predicate).Sum((DataRow row) => ReadDecimal(row, columnName2)) ?? 0m;
	}

	private static int CountRows(DataTable? table, Func<DataRow, bool> predicate)
	{
		return table?.AsEnumerable().Count(predicate) ?? 0;
	}

	private static string FormatCurrency(decimal amount)
	{
		return $"PHP {amount:N2}";
	}

	private static string ToTitleCase(string value)
	{
		string text = value ?? string.Empty;
		text = text.Replace('_', ' ');
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
	}

	private static string EscapeForRowFilter(string value)
	{
		return value.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]")
			.Replace("*", "[*]");
	}

	private static DateTime? ReadNullableDate(object value)
	{
		if (value == DBNull.Value)
		{
			return null;
		}
		if (value is DateTime)
		{
			return (DateTime)value;
		}
		if (DateTime.TryParse(Convert.ToString(value), out var result))
		{
			return result;
		}
		return null;
	}

	private static ExpenseEntryRecord ToExpenseRecord(DataRowView row)
	{
		return new ExpenseEntryRecord
		{
			ExpenseId = Convert.ToInt32(row["expense_id"], CultureInfo.InvariantCulture),
			ExpenseDate = (ReadNullableDate(row["expense_date"]) ?? DateTime.Today),
			ExpenseCategory = (Convert.ToString(row["expense_category"]) ?? string.Empty),
			ExpenseTitle = (Convert.ToString(row["expense_title"]) ?? string.Empty),
			PayeeName = (Convert.ToString(row["payee_name"]) ?? string.Empty),
			Amount = ((row["amount"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["amount"], CultureInfo.InvariantCulture)),
			PaymentMethod = (Convert.ToString(row["payment_method"]) ?? "Cash"),
			Status = (Convert.ToString(row["status"]) ?? "POSTED"),
			ReferenceNo = (Convert.ToString(row["reference_no"]) ?? string.Empty),
			Notes = (Convert.ToString(row["notes"]) ?? string.Empty)
		};
	}

	private static InventoryItemRecord ToInventoryRecord(DataRowView row)
	{
		return new InventoryItemRecord
		{
			ItemId = Convert.ToInt32(row["item_id"], CultureInfo.InvariantCulture),
			ItemName = (Convert.ToString(row["item_name"]) ?? string.Empty),
			Category = (Convert.ToString(row["category"]) ?? string.Empty),
			Unit = (Convert.ToString(row["unit"]) ?? "pcs"),
			QuantityOnHand = ((row["quantity_on_hand"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["quantity_on_hand"], CultureInfo.InvariantCulture)),
			ReorderLevel = ((row["reorder_level"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["reorder_level"], CultureInfo.InvariantCulture)),
			UnitCost = ((row["unit_cost"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["unit_cost"], CultureInfo.InvariantCulture)),
			Location = (Convert.ToString(row["location"]) ?? string.Empty),
			ItemStatus = (Convert.ToString(row["item_status"]) ?? "ACTIVE"),
			LastRestockedAt = ReadNullableDate(row["last_restocked_at"]),
			Notes = (Convert.ToString(row["notes"]) ?? string.Empty)
		};
	}

	private static AssetRecord ToAssetRecord(DataRowView row)
	{
		return new AssetRecord
		{
			AssetId = Convert.ToInt32(row["asset_id"], CultureInfo.InvariantCulture),
			AssetName = (Convert.ToString(row["asset_name"]) ?? string.Empty),
			AssetCategory = (Convert.ToString(row["asset_category"]) ?? string.Empty),
			AssetTag = (Convert.ToString(row["asset_tag"]) ?? string.Empty),
			AcquisitionDate = ReadNullableDate(row["acquisition_date"]),
			AcquisitionCost = ((row["acquisition_cost"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["acquisition_cost"], CultureInfo.InvariantCulture)),
			AssignedLocation = (Convert.ToString(row["assigned_location"]) ?? string.Empty),
			CustodianName = (Convert.ToString(row["custodian_name"]) ?? string.Empty),
			ConditionStatus = (Convert.ToString(row["condition_status"]) ?? "GOOD"),
			LifecycleStatus = (Convert.ToString(row["lifecycle_status"]) ?? "ACTIVE"),
			Notes = (Convert.ToString(row["notes"]) ?? string.Empty)
		};
	}}
