using System;
using System.Collections;
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
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.Views.Panels;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class BlotterPage : UserControl, IRefreshable
{
	private readonly BlotterRepository _repository = new BlotterRepository();

	private DataTable? _data;

	private bool _isViewReady;





























	public BlotterPage()
	{
		InitializeComponent();
		_isViewReady = true;
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public BlotterPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		try
		{
			_data = await _repository.LoadCaseListAsync(UserSession.BarangayId);
			EnsureDisplayColumns(_data);
			PopulateCaseTypeFilter(_data);
			mainGrid.ItemsSource = _data?.DefaultView;
			UpdateRefreshTimestamp();
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("BlotterPage load failed.", ex);
			_data = null;
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load blotter records. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
			contextActionBar.Visibility = Visibility.Collapsed;
			ResetDashboardState();
		}
	}

	private static void EnsureDisplayColumns(DataTable? table)
	{
		if (table == null)
		{
			return;
		}
		if (!table.Columns.Contains("incident_date_display"))
		{
			table.Columns.Add("incident_date_display", typeof(string));
		}
		if (!table.Columns.Contains("status_display"))
		{
			table.Columns.Add("status_display", typeof(string));
		}
		foreach (DataRow row in table.Rows)
		{
			row["incident_date_display"] = FormatDisplayDate(Convert.ToString(row["incident_date"]));
			row["status_display"] = WorkflowRules.NormalizeBlotterStatus(Convert.ToString(row["status"]));
		}
	}

	private static string FormatDisplayDate(string? rawValue)
	{
		if (string.IsNullOrWhiteSpace(rawValue))
		{
			return "Date not recorded";
		}
		if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) || DateTime.TryParse(rawValue, out result))
		{
			return result.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
		}
		return rawValue.Trim();
	}

	private void PopulateCaseTypeFilter(DataTable? table)
	{
		string selectedComboText = GetSelectedComboText(caseTypeFilterCombo);
		caseTypeFilterCombo.Items.Clear();
		caseTypeFilterCombo.Items.Add(new ComboBoxItem
		{
			Content = "All Case Types"
		});
		if (table != null && table.Columns.Contains("incident_type"))
		{
			foreach (string item in from value in (from row in table.AsEnumerable()
					select Convert.ToString(row["incident_type"]) ?? string.Empty into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value)
			{
				caseTypeFilterCombo.Items.Add(new ComboBoxItem
				{
					Content = item
				});
			}
		}
		SelectComboValue(caseTypeFilterCombo, selectedComboText);
	}

	private void SelectComboValue(ComboBox comboBox, string value)
	{
		foreach (object item in (IEnumerable)comboBox.Items)
		{
			if (item is ComboBoxItem comboBoxItem && string.Equals(Convert.ToString(comboBoxItem.Content), value, StringComparison.OrdinalIgnoreCase))
			{
				comboBox.SelectedItem = comboBoxItem;
				return;
			}
		}
		comboBox.SelectedIndex = ((comboBox.Items.Count <= 0) ? (-1) : 0);
	}

	private void ApplyFilters()
	{
		if (!_isViewReady)
		{
			return;
		}
		UpdateSearchPlaceholder();
		if (_data == null)
		{
			ResetDashboardState();
			return;
		}
		List<string> list = new List<string>();
		string text = searchBox.Text.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string escaped = text.Replace("'", "''");
			string text2 = string.Join(" OR ", from column in new string[6] { "case_no", "complainant_name", "respondent_name", "incident_type", "incident_date", "status" }
				where _data.Columns.Contains(column)
				select $"[{column}] LIKE '%{escaped}%'");
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add("(" + text2 + ")");
			}
		}
		string selectedComboText = GetSelectedComboText(caseTypeFilterCombo);
		if (!string.IsNullOrWhiteSpace(selectedComboText) && !selectedComboText.StartsWith("All ", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[incident_type] = '" + selectedComboText.Replace("'", "''") + "'");
		}
		string selectedComboText2 = GetSelectedComboText(statusFilterCombo);
		if (!string.IsNullOrWhiteSpace(selectedComboText2) && !selectedComboText2.StartsWith("All ", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("[status_display] = '" + selectedComboText2.Replace("'", "''") + "'");
		}
		string selectedComboText3 = GetSelectedComboText(timeFilterCombo);
		if (!string.IsNullOrWhiteSpace(selectedComboText3) && !selectedComboText3.Equals("All Time", StringComparison.OrdinalIgnoreCase))
		{
			DateTime now = DateTime.Now;
			DateTime dateTime = DateTime.MinValue;
			if (selectedComboText3.Equals("This Week", StringComparison.OrdinalIgnoreCase))
			{
				int num = (int)(7 + (now.DayOfWeek - 1)) % 7;
				dateTime = now.AddDays(-1 * num).Date;
			}
			else if (selectedComboText3.Equals("This Month", StringComparison.OrdinalIgnoreCase))
			{
				dateTime = new DateTime(now.Year, now.Month, 1);
			}
			else if (selectedComboText3.Equals("This Year", StringComparison.OrdinalIgnoreCase))
			{
				dateTime = new DateTime(now.Year, 1, 1);
			}
			if (dateTime != DateTime.MinValue)
			{
				list.Add($"incident_date >= '{dateTime:yyyy-MM-dd}'");
			}
		}
		_data.DefaultView.RowFilter = string.Join(" AND ", list);
		UpdateDashboardState(text, selectedComboText, selectedComboText2, selectedComboText3);
	}

	private void UpdateDashboardState(string search = "", string selectedCaseType = "", string selectedStatus = "", string selectedTime = "")
	{
		if (!_isViewReady)
		{
			return;
		}
		if (_data == null)
		{
			ResetDashboardState();
			return;
		}
		int count = _data.Rows.Count;
		int count2 = _data.DefaultView.Count;
		List<DataRowView> source = _data.DefaultView.Cast<DataRowView>().ToList();
		int value = CountDistinct(source.Select((DataRowView row) => Convert.ToString(row["incident_type"])));
		int value2 = CountDistinct(from row in _data.AsEnumerable()
			select Convert.ToString(row["incident_type"]));
		int value3 = source.Count((DataRowView row) => IsActiveStatus(Convert.ToString(row["status_display"])));
		int num = source.Count((DataRowView row) => IsResolvedStatus(Convert.ToString(row["status_display"])));
		bool flag = HasActiveFilters(search, selectedCaseType, selectedStatus, selectedTime);
		bool flag2 = count2 > 0;
		totalCasesMetric.Text = count.ToString("N0");
		visibleCasesMetric.Text = count2.ToString("N0");
		ongoingCasesMetric.Text = value3.ToString("N0");
		resolvedCasesMetric.Text = num.ToString("N0");
		headerVisibleLabel.Text = $"{count2:N0} visible now";
		headerActiveLabel.Text = $"{value3:N0} active / pending";
		headerTypeLabel.Text = $"{value:N0} incident type(s)";
		if (count == 0)
		{
			recordCountLabel.Text = "No blotter cases on file yet.";
		}
		else if (!flag)
		{
			recordCountLabel.Text = $"{count:N0} blotter case(s) across {value2:N0} incident type(s)";
		}
		else
		{
			recordCountLabel.Text = $"{count2:N0} of {count:N0} case(s) shown";
		}
		toolbarInsightLabel.Text = (flag ? $"Showing {count2:N0} filtered case(s)." : "Showing the full blotter queue.");
		activeFilterLabel.Text = BuildFilterSummary(search, selectedCaseType, selectedStatus, selectedTime);
		tableSummaryLabel.Text = (flag2 ? $"{count2:N0} case(s) ready for review and mediation actions." : "No cases match the current search and filter settings.");
		footerCountLabel.Text = (flag2 ? $"Showing {count2:N0} case(s)" : "No blotter cases found.");
		emptyLabel.Text = ((count == 0) ? "No blotter cases have been filed yet." : "No blotter cases match the current search and filter settings.");
		emptyState.Visibility = (flag2 ? Visibility.Collapsed : Visibility.Visible);
		SyncSelectionState();
	}

	private void ResetDashboardState()
	{
		if (_isViewReady)
		{
			UpdateSearchPlaceholder();
			totalCasesMetric.Text = "0";
			visibleCasesMetric.Text = "0";
			ongoingCasesMetric.Text = "0";
			resolvedCasesMetric.Text = "0";
			headerVisibleLabel.Text = "0 visible now";
			headerActiveLabel.Text = "0 active / pending";
			headerTypeLabel.Text = "0 incident type(s)";
			recordCountLabel.Text = "No blotter cases on file yet.";
			toolbarInsightLabel.Text = "Showing the full blotter queue.";
			activeFilterLabel.Text = "Filters: All search terms | All case types | All statuses | All time";
			tableSummaryLabel.Text = "0 case(s) ready for review and mediation actions.";
			footerCountLabel.Text = "No blotter cases found.";
		}
	}

	private static int CountDistinct(IEnumerable<string?> values)
	{
		return (from value in values
			select value?.Trim() ?? string.Empty into value
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct<string>(StringComparer.OrdinalIgnoreCase).Count();
	}

	private static bool HasActiveFilters(string search, string selectedCaseType, string selectedStatus, string selectedTime)
	{
		if (string.IsNullOrWhiteSpace(search) && (string.IsNullOrWhiteSpace(selectedCaseType) || selectedCaseType.StartsWith("All ", StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(selectedStatus) || selectedStatus.StartsWith("All ", StringComparison.OrdinalIgnoreCase)))
		{
			if (!string.IsNullOrWhiteSpace(selectedTime))
			{
				return !selectedTime.Equals("All Time", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		return true;
	}

	private static bool IsActiveStatus(string? status)
	{
		string text = WorkflowRules.NormalizeBlotterStatus(status);
		if (!(text == "ONGOING"))
		{
			return text == "REFERRED";
		}
		return true;
	}

	private static bool IsResolvedStatus(string? status)
	{
		string text = WorkflowRules.NormalizeBlotterStatus(status);
		if (!(text == "SETTLED"))
		{
			return text == "CLOSED";
		}
		return true;
	}

	private static string BuildFilterSummary(string search, string selectedCaseType, string selectedStatus, string selectedTime)
	{
		string value = (string.IsNullOrWhiteSpace(search) ? "All search terms" : ("Search '" + search + "'"));
		string value2 = ((string.IsNullOrWhiteSpace(selectedCaseType) || selectedCaseType.StartsWith("All ", StringComparison.OrdinalIgnoreCase)) ? "All case types" : selectedCaseType);
		string value3 = ((string.IsNullOrWhiteSpace(selectedStatus) || selectedStatus.StartsWith("All ", StringComparison.OrdinalIgnoreCase)) ? "All statuses" : selectedStatus);
		string value4 = ((string.IsNullOrWhiteSpace(selectedTime) || selectedTime.Equals("All Time", StringComparison.OrdinalIgnoreCase)) ? "All time" : selectedTime);
		return $"Filters: {value} | {value2} | {value3} | {value4}";
	}

	private static string GetSelectedComboText(ComboBox comboBox)
	{
		if (!(comboBox.SelectedItem is ComboBoxItem comboBoxItem))
		{
			return string.Empty;
		}
		return Convert.ToString(comboBoxItem.Content) ?? string.Empty;
	}

	private void UpdateSearchPlaceholder()
	{
		if (_isViewReady)
		{
			searchPlaceholderText.Visibility = ((!string.IsNullOrWhiteSpace(searchBox.Text)) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void UpdateRefreshTimestamp()
	{
		lastRefreshLabel.Text = $"Last refreshed {DateTime.Now:MMM dd, yyyy hh:mm tt}";
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void TimeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void CaseTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SyncSelectionState();
	}

	private void SyncSelectionState()
	{
		if (_isViewReady)
		{
			if (mainGrid.SelectedItem is DataRowView dataRowView)
			{
				contextActionBar.Visibility = Visibility.Visible;
				selectedRecordLabel.Text = Convert.ToString(dataRowView["case_no"]) ?? "Unknown Case";
				selectedPartiesLabel.Text = GetRowText(dataRowView, "complainant_name", "Unassigned complainant") + " vs " + GetRowText(dataRowView, "respondent_name", "Unspecified respondent");
				selectedRecordMetaLabel.Text = $"{GetRowText(dataRowView, "incident_type", "General")} | {GetRowText(dataRowView, "status_display", "ONGOING")} | {GetRowText(dataRowView, "incident_date_display", "Date not recorded")}";
			}
			else
			{
				contextActionBar.Visibility = Visibility.Collapsed;
			}
		}
	}

	private static string GetRowText(DataRowView row, string columnName, string fallback)
	{
		if (!row.Row.Table.Columns.Contains(columnName))
		{
			return fallback;
		}
		return Convert.ToString(row[columnName]) ?? fallback;
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.UnselectAll();
		contextActionBar.Visibility = Visibility.Collapsed;
	}

	private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
	{
		searchBox.Text = string.Empty;
		timeFilterCombo.SelectedIndex = ((timeFilterCombo.Items.Count <= 0) ? (-1) : 0);
		caseTypeFilterCombo.SelectedIndex = ((caseTypeFilterCombo.Items.Count <= 0) ? (-1) : 0);
		statusFilterCombo.SelectedIndex = ((statusFilterCombo.Items.Count <= 0) ? (-1) : 0);
		ApplyFilters();
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		OpenSelectedCaseFullscreen();
	}

	private void OpenSelectedCaseFullscreen()
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a case first.");
			return;
		}

		int caseId = Convert.ToInt32(dataRowView["case_id"]);
		string caseNo = Convert.ToString(dataRowView["case_no"]) ?? string.Empty;
		string status = Convert.ToString(dataRowView["status_display"]) ?? "ONGOING";

		var editForm = new BlotterFormPanel(FormMode.Edit, new BlotterDto
		{
			CaseId = caseId,
			CaseNo = caseNo,
			Status = status
		});

		var toolbarItems = new List<UIElement>();

		toolbarItems.Add(CreateFullscreenToolbarButton("Save Changes", IconChar.Save,
			async (s, args) =>
			{
				if (await editForm.TrySaveAsync())
				{
					NavigationService.Instance.NavigateBackFromFullscreen(
						"ResidentCases", refreshOnReturn: true);
				}
			}));

		toolbarItems.Add(CreateFullscreenToolbarButton("Resolve Case", IconChar.CheckCircle,
			async (s, args) =>
			{
				if (await editForm.TryResolveAsync())
				{
					NavigationService.Instance.NavigateBackFromFullscreen(
						"ResidentCases", refreshOnReturn: true);
				}
			}));

		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = $"Case {caseNo}",
			Subtitle = "Edit case details and manage resolution",
			OriginRoute = "ResidentCases",
			Content = editForm,
			Icon = IconChar.Gavel,
			ToolbarItems = toolbarItems,
			ShowSideToolbar = false,
			OnSaved = () => RefreshData()
		});
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		var addForm = new BlotterFormPanel(FormMode.Create);

		var saveButton = CreateFullscreenToolbarButton("Save Case", IconChar.Save,
			async (s, args) =>
			{
				if (await addForm.TrySaveAsync())
				{
					NavigationService.Instance.NavigateBackFromFullscreen(
						"ResidentCases", refreshOnReturn: true);
				}
			});

		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "New Blotter Case",
			Subtitle = "File a new blotter case record",
			OriginRoute = "ResidentCases",
			Content = addForm,
			Icon = IconChar.Gavel,
			ToolbarItems = new List<UIElement> { saveButton },
			ShowSideToolbar = false,
			OnSaved = () => RefreshData()
		});
	}

	private void BtnExportCase_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a case to export.");
			return;
		}
		string caseNo = Convert.ToString(dataRowView["case_no"]) ?? "case";
		DialogService.Instance.ShowInfo($"Export for case {caseNo} is being prepared.");
	}

	#region IRefreshable Implementation

	/// <summary>
	/// Refreshes the page data after returning from a fullscreen view.
	/// Implements IRefreshable to support automatic data refresh on back navigation.
	/// Requirement 2.6: Module page refreshes data via IRefreshable.RefreshData().
	/// </summary>
	public void RefreshData()
	{
		_ = LoadAsync();
	}

	#endregion

	#region Fullscreen View Helpers

	/// <summary>
	/// Creates a styled toolbar button for use in fullscreen view toolbars.
	/// Follows the same pattern as ResidentModulePage.CreateFullscreenToolbarButton.
	/// </summary>
	private static Button CreateFullscreenToolbarButton(string label, IconChar icon, RoutedEventHandler clickHandler)
	{
		var iconBlock = new IconBlock
		{
			Icon = icon,
			FontSize = 14,
			Margin = new Thickness(0, 0, 6, 0),
			VerticalAlignment = VerticalAlignment.Center
		};

		var textBlock = new TextBlock
		{
			Text = label,
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 12
		};

		var panel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		panel.Children.Add(iconBlock);
		panel.Children.Add(textBlock);

		var button = new Button
		{
			Content = panel,
			Padding = new Thickness(12, 6, 12, 6),
			Margin = new Thickness(0, 0, 4, 0),
			MinHeight = 32,
			Cursor = System.Windows.Input.Cursors.Hand
		};

		// Set accessibility name (Requirement 5.4)
		System.Windows.Automation.AutomationProperties.SetName(button, label);

		button.Click += clickHandler;
		return button;
	}

	#endregion
}
