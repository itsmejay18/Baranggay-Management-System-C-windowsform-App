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
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;

namespace baranggaysystem1.Views.Pages;

public partial class AyudaPage : UserControl
{
	private readonly AyudaService _ayudaService = new AyudaService();

	private DataTable? _programData;

	private DataTable? _releaseData;






















	public AyudaPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public AyudaPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		_ = 1;
		try
		{
			_programData = await _ayudaService.GetProgramLedgerAsync();
			_releaseData = await _ayudaService.GetReleaseLedgerAsync();
			EnrichProgramTable(_programData);
			EnrichReleaseTable(_releaseData);
			ApplyDataSources();
			PopulateFilterOptions();
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("AyudaPage load failed.", ex);
			programGrid.ItemsSource = null;
			releaseGrid.ItemsSource = null;
			programEmptyLabel.Text = "Failed to load ayuda budgets.";
			releaseEmptyLabel.Text = "Failed to load ayuda releases.";
			programEmptyState.Visibility = Visibility.Visible;
			releaseEmptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Unable to load ayuda records.";
			recordCountLabel.Text = "Ayuda module unavailable";
			totalBudgetMetric.Text = "PHP 0.00";
			spentBudgetMetric.Text = "PHP 0.00";
			remainingBudgetMetric.Text = "PHP 0.00";
			beneficiaryMetric.Text = "0";
		}
	}

	private void ApplyDataSources()
	{
		programGrid.ItemsSource = _programData?.DefaultView;
		releaseGrid.ItemsSource = _releaseData?.DefaultView;
		programEmptyState.Visibility = ((_programData != null && _programData.Rows.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		releaseEmptyState.Visibility = ((_releaseData != null && _releaseData.Rows.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void PopulateFilterOptions()
	{
		programFilter.Items.Clear();
		programFilter.Items.Add("All Programs");
		statusFilter.Items.Clear();
		statusFilter.Items.Add("All Release Status");
		if (_programData != null)
		{
			foreach (string item in from value in (from row in _programData.AsEnumerable()
					select Convert.ToString(row["program_name"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				programFilter.Items.Add(item);
			}
		}
		if (_releaseData != null)
		{
			foreach (string item2 in from value in (from row in _releaseData.AsEnumerable()
					select Convert.ToString(row["release_status"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				statusFilter.Items.Add(item2);
			}
		}
		programFilter.SelectedIndex = 0;
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
		if (_programData == null || _releaseData == null)
		{
			return;
		}
		string value = searchBox.Text.Trim();
		string text = programFilter.SelectedItem as string;
		string text2 = statusFilter.SelectedItem as string;
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (!string.IsNullOrWhiteSpace(value))
		{
			string escaped = EscapeForRowFilter(value);
			string[] source = new string[7] { "program_name", "category", "status_display", "allocated_budget_display", "budget_meta", "schedule_display", "notes" };
			string[] source2 = new string[9] { "reference_no", "batch_reference", "reference_meta", "resident_name", "program_name", "category", "amount_display", "released_at", "notes_display" };
			list.Add("(" + string.Join(" OR ", source.Select((string column) => $"Convert([{column}], 'System.String') LIKE '%{escaped}%'")) + ")");
			list2.Add("(" + string.Join(" OR ", source2.Select((string column) => $"Convert([{column}], 'System.String') LIKE '%{escaped}%'")) + ")");
		}
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "All Programs", StringComparison.OrdinalIgnoreCase))
		{
			string text3 = EscapeForRowFilter(text);
			list.Add("[program_name] = '" + text3 + "'");
			list2.Add("[program_name] = '" + text3 + "'");
		}
		if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, "All Release Status", StringComparison.OrdinalIgnoreCase))
		{
			list2.Add("[release_status] = '" + EscapeForRowFilter(text2) + "'");
		}
		_programData.DefaultView.RowFilter = string.Join(" AND ", list);
		_releaseData.DefaultView.RowFilter = string.Join(" AND ", list2);
		List<DataRowView> list3 = _programData.DefaultView.Cast<DataRowView>().ToList();
		List<DataRowView> list4 = _releaseData.DefaultView.Cast<DataRowView>().ToList();
		decimal amount = list3.Sum((DataRowView row) => GetDecimal(row, "allocated_budget"));
		decimal amount2 = list3.Sum((DataRowView row) => GetDecimal(row, "spent_budget"));
		decimal amount3 = list3.Sum((DataRowView row) => GetDecimal(row, "remaining_budget"));
		int num = (from row in list4
			where !string.Equals(Convert.ToString(row["release_status"]), "CANCELLED", StringComparison.OrdinalIgnoreCase)
			select Convert.ToInt32(row["resident_id"], CultureInfo.InvariantCulture)).Distinct().Count();
		totalBudgetMetric.Text = FormatCurrency(amount);
		spentBudgetMetric.Text = FormatCurrency(amount2);
		remainingBudgetMetric.Text = FormatCurrency(amount3);
		beneficiaryMetric.Text = num.ToString("N0", CultureInfo.InvariantCulture);
		programTableVisibleLabel.Text = $"{list3.Count:N0} visible";
		releaseTableVisibleLabel.Text = $"{list4.Count:N0} visible";
		recordCountLabel.Text = $"{list3.Count:N0} program(s) and {list4.Count:N0} release(s) in view";
		footerCountLabel.Text = $"Showing {list3.Count:N0} ayuda program(s) and {list4.Count:N0} release ledger item(s)";
		programEmptyState.Visibility = ((list3.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		releaseEmptyState.Visibility = ((list4.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		programEmptyLabel.Text = ((list3.Count == 0) ? "No ayuda programs match the current filters." : "No ayuda programs found.");
		releaseEmptyLabel.Text = ((list4.Count == 0) ? "No ayuda releases match the current filters." : "No ayuda releases found.");
		UpdateProgramSelectionState(programGrid.SelectedItem as DataRowView);
		UpdateReleaseSelectionState(releaseGrid.SelectedItem as DataRowView);
	}

	private void ProgramGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateProgramSelectionState(programGrid.SelectedItem as DataRowView);
	}

	private void ReleaseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateReleaseSelectionState(releaseGrid.SelectedItem as DataRowView);
	}

	private void UpdateProgramSelectionState(DataRowView? row)
	{
		if (row == null)
		{
			programTableSelectionLabel.Text = "No selection";
			selectedProgramFooterLabel.Text = "No budget selected";
			return;
		}
		string text = Convert.ToString(row["program_name"]) ?? "Program";
		string text2 = Convert.ToString(row["remaining_budget_display"]) ?? "PHP 0.00";
		programTableSelectionLabel.Text = "Selected: " + text;
		selectedProgramFooterLabel.Text = text + " | Remaining " + text2;
	}

	private void UpdateReleaseSelectionState(DataRowView? row)
	{
		if (row == null)
		{
			releaseTableSelectionLabel.Text = "No selection";
			return;
		}
		string text = Convert.ToString(row["reference_no"]) ?? "Release";
		string text2 = Convert.ToString(row["batch_reference"]) ?? string.Empty;
		releaseTableSelectionLabel.Text = (string.IsNullOrWhiteSpace(text2) ? ("Selected: " + text) : ("Selected: " + text + " | " + text2));
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnAddProgram_Click(object sender, RoutedEventArgs e)
	{
		AyudaProgramWindow window = new AyudaProgramWindow();
		if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
		{
			await LoadAsync();
		}
	}

	private async void BtnEditProgram_Click(object sender, RoutedEventArgs e)
	{
		if (!(programGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda budget program to edit first.");
			return;
		}
		AyudaProgramWindow window = new AyudaProgramWindow(Convert.ToInt32(dataRowView["program_id"], CultureInfo.InvariantCulture));
		if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
		{
			await LoadAsync();
		}
	}

	private async void BtnDeleteProgram_Click(object sender, RoutedEventArgs e)
	{
		if (!(programGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda budget program to delete first.");
			return;
		}
		int programId = Convert.ToInt32(dataRowView["program_id"], CultureInfo.InvariantCulture);
		string text = Convert.ToString(dataRowView["program_name"], CultureInfo.InvariantCulture) ?? "this program";
		if (!DialogService.Instance.Confirm("Delete ayuda budget program \"" + text + "\"?\n\nPrograms with saved releases cannot be deleted.", "Delete Ayuda Program"))
		{
			return;
		}
		try
		{
			await _ayudaService.DeleteProgramAsync(programId);
			DialogService.Instance.ShowInfo("Ayuda budget program deleted successfully.");
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda program deletion failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Delete Ayuda Program");
		}
	}

	private async void BtnReleaseAyuda_Click(object sender, RoutedEventArgs e)
	{
		int? initialProgramId = null;
		if (programGrid.SelectedItem is DataRowView dataRowView)
		{
			initialProgramId = Convert.ToInt32(dataRowView["program_id"], CultureInfo.InvariantCulture);
		}
		else if (releaseGrid.SelectedItem is DataRowView dataRowView2)
		{
			initialProgramId = Convert.ToInt32(dataRowView2["program_id"], CultureInfo.InvariantCulture);
		}
		AyudaReleaseWindow window = new AyudaReleaseWindow(initialProgramId);
		if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
		{
			await LoadAsync();
		}
	}

	private async void BtnEditRelease_Click(object sender, RoutedEventArgs e)
	{
		if (!(releaseGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda release to edit first.");
			return;
		}
		int value = Convert.ToInt32(dataRowView["release_id"], CultureInfo.InvariantCulture);
		AyudaReleaseWindow window = new AyudaReleaseWindow(null, value);
		if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
		{
			await LoadAsync();
		}
	}

	private async void BtnDeleteRelease_Click(object sender, RoutedEventArgs e)
	{
		if (!(releaseGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda release to delete first.");
			return;
		}
		int releaseId = Convert.ToInt32(dataRowView["release_id"], CultureInfo.InvariantCulture);
		string text = Convert.ToString(dataRowView["resident_name"], CultureInfo.InvariantCulture) ?? "this beneficiary";
		string value = Convert.ToString(dataRowView["batch_reference"], CultureInfo.InvariantCulture) ?? string.Empty;
		string message = (string.IsNullOrWhiteSpace(value) ? ("Delete the ayuda release for " + text + "?") : $"Delete the ayuda release for {text} from batch {value}?");
		if (!DialogService.Instance.Confirm(message, "Delete Ayuda Release"))
		{
			return;
		}
		try
		{
			await _ayudaService.DeleteReleaseAsync(releaseId);
			DialogService.Instance.ShowInfo("Ayuda release deleted successfully.");
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda release deletion failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Delete Ayuda Release");
		}
	}

	private void BtnOpenReport_Click(object sender, RoutedEventArgs e)
	{
		if (!(releaseGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Select an ayuda release first.");
			return;
		}
		string text = Convert.ToString(dataRowView["report_file_path"], CultureInfo.InvariantCulture) ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			DialogService.Instance.ShowWarning("No generated report is linked to the selected release.");
		}
		else
		{
			AyudaReleaseReportService.TryOpenGeneratedFile(text);
		}
	}

	private static void EnrichProgramTable(DataTable table)
	{
		EnsureStringColumn(table, "allocated_budget_display");
		EnsureStringColumn(table, "remaining_budget_display");
		EnsureStringColumn(table, "status_display");
		EnsureStringColumn(table, "budget_meta");
		EnsureStringColumn(table, "schedule_display");
		foreach (DataRow row in table.Rows)
		{
			decimal amount = ((row["spent_budget"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["spent_budget"], CultureInfo.InvariantCulture));
			decimal amount2 = ((row["remaining_budget"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["remaining_budget"], CultureInfo.InvariantCulture));
			int value = ((row["release_count"] != DBNull.Value) ? Convert.ToInt32(row["release_count"], CultureInfo.InvariantCulture) : 0);
			int value2 = ((row["beneficiary_count"] != DBNull.Value) ? Convert.ToInt32(row["beneficiary_count"], CultureInfo.InvariantCulture) : 0);
			string text = Convert.ToString(row["start_date_display"]) ?? string.Empty;
			string text2 = Convert.ToString(row["end_date_display"]) ?? string.Empty;
			string value3 = Convert.ToString(row["status"]) ?? "ACTIVE";
			row["allocated_budget_display"] = FormatCurrency(GetDecimal(row, "allocated_budget"));
			row["remaining_budget_display"] = FormatCurrency(amount2);
			row["status_display"] = ToTitleCase(value3);
			row["budget_meta"] = $"Released {FormatCurrency(amount)} | {value2:N0} beneficiary(ies) | {value:N0} release(s)";
			row["schedule_display"] = ((string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2)) ? "No schedule range set" : (string.IsNullOrWhiteSpace(text2) ? ("Started " + text) : (string.IsNullOrWhiteSpace(text) ? ("Until " + text2) : (text + " to " + text2))));
		}
	}

	private static void EnrichReleaseTable(DataTable table)
	{
		EnsureStringColumn(table, "amount_display");
		EnsureStringColumn(table, "release_status_display");
		EnsureStringColumn(table, "resident_meta");
		EnsureStringColumn(table, "notes_display");
		EnsureStringColumn(table, "reference_meta");
		foreach (DataRow row in table.Rows)
		{
			int num = ((row["resident_id"] != DBNull.Value) ? Convert.ToInt32(row["resident_id"], CultureInfo.InvariantCulture) : 0);
			int value = ((row["beneficiary_count"] == DBNull.Value) ? 1 : Convert.ToInt32(row["beneficiary_count"], CultureInfo.InvariantCulture));
			string text = Convert.ToString(row["notes"]) ?? string.Empty;
			string value2 = Convert.ToString(row["release_status"]) ?? "RELEASED";
			string value3 = Convert.ToString(row["batch_reference"]) ?? string.Empty;
			string text2 = Convert.ToString(row["contact_no"]) ?? string.Empty;
			string text3 = Convert.ToString(row["released_at"]) ?? string.Empty;
			row["amount_display"] = FormatCurrency(GetDecimal(row, "amount"));
			row["release_status_display"] = ToTitleCase(value2);
			row["resident_meta"] = ((!string.IsNullOrWhiteSpace(text2)) ? text2 : ((num > 0) ? $"Resident ID #{num}" : "Resident record"));
			row["notes_display"] = (string.IsNullOrWhiteSpace(text) ? "No notes recorded" : text.Trim());
			row["reference_meta"] = (string.IsNullOrWhiteSpace(value3) ? text3 : $"{text3} | Batch {value3} | {value:N0} beneficiary(ies)");
		}
	}

	private static void EnsureStringColumn(DataTable table, string columnName)
	{
		if (!table.Columns.Contains(columnName))
		{
			table.Columns.Add(columnName, typeof(string));
		}
	}

	private static decimal GetDecimal(DataRowView row, string columnName)
	{
		if (row[columnName] != DBNull.Value)
		{
			return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
		}
		return 0m;
	}

	private static decimal GetDecimal(DataRow row, string columnName)
	{
		if (row[columnName] != DBNull.Value)
		{
			return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
		}
		return 0m;
	}

	private static string FormatCurrency(decimal amount)
	{
		return $"PHP {amount:N2}";
	}

	private static string ToTitleCase(string value)
	{
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase((value ?? string.Empty).ToLowerInvariant());
	}

	private static string EscapeForRowFilter(string value)
	{
		return value.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]")
			.Replace("*", "[*]");
	}}
