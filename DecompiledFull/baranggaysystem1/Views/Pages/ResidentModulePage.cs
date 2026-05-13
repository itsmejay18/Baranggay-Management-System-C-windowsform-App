using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Pages;

public class ResidentModulePage : UserControl, IComponentConnector
{
	private sealed class RegistryPageConfig
	{
		public string CategoryKey { get; init; } = "ALL";

		public string PageTitle { get; init; } = "Resident Records";

		public string TableTitle { get; init; } = "Resident Directory";

		public string SearchPlaceholder { get; init; } = "Search by resident name, contact, purok, or ID";

		public string EmptyStateLabel { get; init; } = "No residents found.";

		public string TotalMetricLabel { get; init; } = "residents";

		public string RegisteredSummaryLabel { get; init; } = "residents registered";

		public string FooterRecordLabel { get; init; } = "resident record(s)";

		public string BrowseTableMetaLabel { get; init; } = "Browse the full directory, then select a row for quick actions.";

		public string EmptyTableMetaLabel { get; init; } = "Residents will appear here once records are loaded.";

		public string FilterMatchLabel { get; init; } = "resident(s)";

		public string SelectionSubjectLabel { get; init; } = "resident";

		public string NoRecordsSummaryLabel { get; init; } = "No records.";

		public string NoRecordsFooterLabel { get; init; } = "No resident records available.";
	}

	private static readonly string[] DefaultStatuses = new string[4] { "All Status", "Active", "Inactive", "Deceased" };

	private static readonly string[] SearchableColumns = new string[8] { "resident_id", "full_name", "age_display", "sex_display", "purok_display", "contact_display", "matched_category_display", "status_display" };

	private DataTable? _data;

	private readonly ResidentsModuleDataService _dataService;

	private readonly RegistryPageConfig _pageConfig;

	internal TextBlock pageTitleText;

	internal TextBlock recordCountLabel;

	internal TextBlock totalResidentsMetric;

	internal TextBlock activeResidentsMetric;

	internal TextBlock purokResidentsMetric;

	internal TextBlock visibleResidentsMetric;

	internal TextBox searchBox;

	internal TextBlock searchPlaceholderText;

	internal ComboBox filterStatus;

	internal ComboBox filterPurok;

	internal TextBlock tableTitleText;

	internal TextBlock tableMetaLabel;

	internal TextBlock tableVisibleLabel;

	internal TextBlock tableSelectionLabel;

	internal DataGrid mainGrid;

	internal StackPanel emptyState;

	internal TextBlock emptyLabel;

	internal Border contextActionBar;

	internal TextBlock selectedResidentLabel;

	internal TextBlock selectedResidentMetaLabel;

	internal Button btnContextBlotter;

	internal Button btnContextBlotterHistory;

	internal Button btnContextCert;

	internal Button btnContextHousehold;

	internal Button btnContextPayment;

	internal TextBlock footerCountLabel;

	internal Button btnEdit;

	internal Button btnDelete;

	private bool _contentLoaded;

	public ResidentModulePage()
		: this("ResidentWorkspace")
	{
	}

	public ResidentModulePage(string route)
	{
		InitializeComponent();
		_dataService = new ResidentsModuleDataService();
		_pageConfig = ResolvePageConfig(route);
		btnDelete.IsEnabled = false;
		InitializeFilters();
		ApplyPageConfig();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	private async Task LoadAsync()
	{
		try
		{
			_data = await FetchData();
			ApplyDataToGrid(_data);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("ResidentModulePage load failed.", ex);
			_data = null;
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load data. Please refresh.";
			UpdateSummaryMetrics();
			UpdateChrome();
		}
	}

	private async Task<DataTable> FetchData()
	{
		return await _dataService.LoadResidentsByCategoryAsync(_pageConfig.CategoryKey, null, null, null, null);
	}

	private void InitializeFilters()
	{
		filterStatus.ItemsSource = DefaultStatuses;
		filterStatus.SelectedIndex = 0;
		filterPurok.ItemsSource = new string[1] { "All Puroks" };
		filterPurok.SelectedIndex = 0;
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		mainGrid.UnselectAll();
		mainGrid.ItemsSource = table?.DefaultView;
		emptyLabel.Text = _pageConfig.EmptyStateLabel;
		RefreshPurokOptions(table);
		UpdateSummaryMetrics();
		ApplyFilters();
	}

	private void RefreshPurokOptions(DataTable? table)
	{
		string currentSelection = GetSelectedFilterValue(filterPurok);
		List<string> list = new List<string> { "All Puroks" };
		if (table != null && table.Columns.Contains("purok_display"))
		{
			list.AddRange(from value in (from row in table.AsEnumerable()
					select GetCellText(row, "purok_display") into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value);
		}
		filterPurok.ItemsSource = list;
		filterPurok.SelectedItem = list.FirstOrDefault((string value) => string.Equals(value, currentSelection, StringComparison.OrdinalIgnoreCase)) ?? list[0];
	}

	private void ApplyFilters()
	{
		if (_data == null)
		{
			UpdateChrome();
			return;
		}
		List<string> list = new List<string>();
		string query = EscapeFilterValue(searchBox.Text.Trim());
		if (!string.IsNullOrWhiteSpace(query))
		{
			List<string> list2 = (from column in SearchableColumns
				where _data.Columns.Contains(column)
				select $"Convert([{column}], 'System.String') LIKE '%{query}%'").ToList();
			if (list2.Count > 0)
			{
				list.Add("(" + string.Join(" OR ", list2) + ")");
			}
		}
		string selectedFilterValue = GetSelectedFilterValue(filterStatus);
		if (!string.IsNullOrWhiteSpace(selectedFilterValue) && !string.Equals(selectedFilterValue, "All Status", StringComparison.OrdinalIgnoreCase) && _data.Columns.Contains("status_display"))
		{
			string value = EscapeFilterValue(selectedFilterValue);
			string value2 = EscapeFilterValue(selectedFilterValue.ToUpperInvariant());
			list.Add($"(Convert([status_display], 'System.String') = '{value}' OR Convert([status_display], 'System.String') = '{value2}')");
		}
		string selectedFilterValue2 = GetSelectedFilterValue(filterPurok);
		if (!string.IsNullOrWhiteSpace(selectedFilterValue2) && !string.Equals(selectedFilterValue2, "All Puroks", StringComparison.OrdinalIgnoreCase) && _data.Columns.Contains("purok_display"))
		{
			list.Add("Convert([purok_display], 'System.String') = '" + EscapeFilterValue(selectedFilterValue2) + "'");
		}
		_data.DefaultView.RowFilter = string.Join(" AND ", list);
		UpdateChrome();
	}

	private static string EscapeFilterValue(string value)
	{
		return value.Replace("'", "''");
	}

	private static string GetCellText(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return row[columnName]?.ToString()?.Trim() ?? string.Empty;
	}

	private string GetSelectedFilterValue(ComboBox comboBox)
	{
		return comboBox.SelectedItem?.ToString()?.Trim() ?? string.Empty;
	}

	private void UpdateSummaryMetrics()
	{
		int value = _data?.Rows.Count ?? 0;
		int value2 = _data?.AsEnumerable().Count((DataRow row) => string.Equals(GetCellText(row, "status_display"), "Active", StringComparison.OrdinalIgnoreCase)) ?? 0;
		int value3 = (from row in _data?.AsEnumerable()
			select GetCellText(row, "purok_display") into value4
			where !string.IsNullOrWhiteSpace(value4)
			select value4).Distinct<string>(StringComparer.OrdinalIgnoreCase).Count() ?? 0;
		totalResidentsMetric.Text = $"{value:N0} {_pageConfig.TotalMetricLabel}";
		activeResidentsMetric.Text = $"{value2:N0} active";
		purokResidentsMetric.Text = $"{value3:N0} puroks";
	}

	private void UpdateChrome()
	{
		int num = _data?.Rows.Count ?? 0;
		int num2 = _data?.DefaultView.Count ?? 0;
		DataRowView dataRowView = mainGrid.SelectedItem as DataRowView;
		bool flag = dataRowView != null;
		visibleResidentsMetric.Text = $"{num2:N0} visible";
		recordCountLabel.Text = ((num == 0) ? _pageConfig.NoRecordsSummaryLabel : $"{num:N0} {_pageConfig.RegisteredSummaryLabel}");
		footerCountLabel.Text = ((num == 0) ? _pageConfig.NoRecordsFooterLabel : ((num2 == num) ? $"Showing {num2:N0} {_pageConfig.FooterRecordLabel}" : $"Showing {num2:N0} of {num:N0} {_pageConfig.FooterRecordLabel}"));
		tableVisibleLabel.Text = $"{num2:N0} visible";
		tableSelectionLabel.Text = (flag ? "1 selected" : "No selection");
		emptyState.Visibility = ((num2 != 0) ? Visibility.Collapsed : Visibility.Visible);
		btnEdit.IsEnabled = flag && Permissions.CanUpdateResidents;
		btnDelete.IsEnabled = flag && Permissions.CanDeleteResidents;
		if (flag)
		{
			selectedResidentLabel.Text = GetCellText(dataRowView.Row, "full_name");
			string cellText = GetCellText(dataRowView.Row, "purok_display");
			string cellText2 = GetCellText(dataRowView.Row, "status_display");
			selectedResidentMetaLabel.Text = ((string.IsNullOrWhiteSpace(cellText) && string.IsNullOrWhiteSpace(cellText2)) ? "Resident details" : ((string.IsNullOrWhiteSpace(cellText) ? "No purok assigned" : cellText) + " | " + (string.IsNullOrWhiteSpace(cellText2) ? "No status" : cellText2)));
			tableMetaLabel.Text = "Quick actions are ready for the selected " + _pageConfig.SelectionSubjectLabel + ".";
			contextActionBar.Visibility = Visibility.Visible;
		}
		else
		{
			selectedResidentMetaLabel.Text = "Resident details";
			tableMetaLabel.Text = ((num == 0) ? _pageConfig.EmptyTableMetaLabel : ((num2 == num) ? _pageConfig.BrowseTableMetaLabel : $"Filtered view active. {num2:N0} {_pageConfig.FilterMatchLabel} match the current search."));
			contextActionBar.Visibility = Visibility.Collapsed;
		}
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateChrome();
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.UnselectAll();
		UpdateChrome();
	}

	private void BtnContextBlotter_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			BlotterDetailsWindow blotterDetailsWindow = new BlotterDetailsWindow(new BlotterDto
			{
				RespondentResidentId = Convert.ToInt32(dataRowView["resident_id"]),
				RespondentName = (dataRowView["full_name"]?.ToString() ?? "")
			});
			blotterDetailsWindow.Owner = Window.GetWindow((DependencyObject)(object)this);
			blotterDetailsWindow.ShowDialog();
		}
	}

	private void BtnContextBlotterHistory_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			int residentId = Convert.ToInt32(dataRowView["resident_id"]);
			string residentName = dataRowView["full_name"]?.ToString() ?? "Resident";
			ResidentBlotterHistoryWindow residentBlotterHistoryWindow = new ResidentBlotterHistoryWindow(residentId, residentName);
			residentBlotterHistoryWindow.Owner = Window.GetWindow((DependencyObject)(object)this);
			residentBlotterHistoryWindow.ShowDialog();
		}
	}

	private void BtnContextCert_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			int residentId = Convert.ToInt32(dataRowView["resident_id"]);
			string residentName = dataRowView["full_name"]?.ToString() ?? "Resident";
			CertificationWindow certificationWindow = new CertificationWindow(residentId, residentName);
			certificationWindow.Owner = Window.GetWindow((DependencyObject)(object)this);
			certificationWindow.ShowDialog();
		}
	}

	private void BtnContextHousehold_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			int value = Convert.ToInt32(dataRowView["resident_id"]);
			HouseholdDetailsWindow householdDetailsWindow = new HouseholdDetailsWindow((dataRowView["household_id"] == DBNull.Value) ? ((int?)null) : new int?(Convert.ToInt32(dataRowView["household_id"])), value);
			householdDetailsWindow.Owner = Window.GetWindow((DependencyObject)(object)this);
			householdDetailsWindow.ShowDialog();
			LoadAsync();
		}
	}

	private void BtnContextPayment_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			int residentId = Convert.ToInt32(dataRowView["resident_id"]);
			string residentName = dataRowView["full_name"]?.ToString() ?? "Resident";
			PaymentWindow paymentWindow = new PaymentWindow(residentId, residentName);
			paymentWindow.Owner = Window.GetWindow((DependencyObject)(object)this);
			paymentWindow.ShowDialog();
		}
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnDelete_Click(object sender, RoutedEventArgs e)
	{
		if (!Permissions.CanDeleteResidents)
		{
			DialogService.Instance.ShowWarning("You do not have permission to delete resident records.");
			return;
		}
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a resident to delete.");
			return;
		}
		int residentId = Convert.ToInt32(dataRowView["resident_id"]);
		string name = dataRowView["full_name"]?.ToString() ?? "this resident";
		if (!DialogService.Instance.Confirm("Delete resident " + name + "?\n\nThis is a soft-delete and can be restored later."))
		{
			return;
		}
		try
		{
			await _dataService.DeleteResidentAsync(residentId, "Archived from resident registry.");
			mainGrid.UnselectAll();
			DialogService.Instance.ShowInfo("Resident " + name + " was archived successfully.");
			await LoadAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Resident delete failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Delete Resident");
		}
	}

	private void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		if (!Permissions.CanUpdateResidents)
		{
			DialogService.Instance.ShowWarning("You do not have permission to edit resident records.");
			return;
		}
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a resident to edit.");
			return;
		}
		ResidentDetailsWindow window = new ResidentDetailsWindow(CreateResidentDto(dataRowView.Row));
		if (DialogService.Instance.ShowDialog(window) == true)
		{
			LoadAsync();
		}
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		if (!Permissions.CanCreateResidents)
		{
			DialogService.Instance.ShowWarning("You do not have permission to add resident records.");
			return;
		}
		ResidentDto residentDto = CreateResidentTemplateForRegistry();
		ResidentDetailsWindow window = ((residentDto == null) ? new ResidentDetailsWindow() : new ResidentDetailsWindow(residentDto));
		if (DialogService.Instance.ShowDialog(window) == true)
		{
			LoadAsync();
		}
	}

	private ResidentDto CreateResidentDto(DataRow row)
	{
		return new ResidentDto
		{
			Id = Convert.ToInt32(row["resident_id"]),
			FirstName = GetCellText(row, "first_name"),
			MiddleName = GetCellText(row, "middle_name"),
			LastName = GetCellText(row, "last_name"),
			Suffix = GetCellText(row, "suffix"),
			Gender = GetCellText(row, "sex"),
			DateOfBirth = ((row.Table.Columns.Contains("birth_date") && row["birth_date"] != DBNull.Value) ? Convert.ToDateTime(row["birth_date"]) : DateTime.Today),
			CivilStatus = GetCellText(row, "civil_status"),
			ContactNo = GetCellText(row, "contact_no"),
			IsPwd = ReadBooleanCell(row, "is_pwd"),
			IsSenior = ReadBooleanCell(row, "is_senior"),
			Is4PsBeneficiary = ReadBooleanCell(row, "is_4ps_beneficiary"),
			IsRegisteredVoter = ReadBooleanCell(row, "is_registered_voter"),
			IsSoloParent = ReadBooleanCell(row, "is_solo_parent"),
			IsYouth = ReadBooleanCell(row, "is_youth"),
			IsIndigent = ReadBooleanCell(row, "is_indigent"),
			Status = GetCellText(row, "status"),
			BarangayId = _dataService.BarangayId,
			PurokId = ReadNullableInt(row, "purok_id"),
			HouseholdId = ReadNullableInt(row, "household_id")
		};
	}

	private void ApplyPageConfig()
	{
		pageTitleText.Text = _pageConfig.PageTitle;
		tableTitleText.Text = _pageConfig.TableTitle;
		searchPlaceholderText.Text = _pageConfig.SearchPlaceholder;
		emptyLabel.Text = _pageConfig.EmptyStateLabel;
		recordCountLabel.Text = _pageConfig.NoRecordsSummaryLabel;
		footerCountLabel.Text = _pageConfig.NoRecordsFooterLabel;
		tableMetaLabel.Text = _pageConfig.BrowseTableMetaLabel;
	}

	private ResidentDto? CreateResidentTemplateForRegistry()
	{
		return _pageConfig.CategoryKey switch
		{
			"SOLO_PARENT" => new ResidentDto
			{
				IsSoloParent = true,
				Status = "ACTIVE"
			}, 
			"YOUTH" => new ResidentDto
			{
				IsYouth = true,
				Status = "ACTIVE"
			}, 
			"INDIGENT" => new ResidentDto
			{
				IsIndigent = true,
				Status = "ACTIVE"
			}, 
			_ => null, 
		};
	}

	private static bool ReadBooleanCell(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return false;
		}
		object obj = row[columnName];
		if (obj is bool)
		{
			return (bool)obj;
		}
		return Convert.ToInt32(obj) == 1;
	}

	private static int? ReadNullableInt(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return null;
		}
		return Convert.ToInt32(row[columnName]);
	}

	private static RegistryPageConfig ResolvePageConfig(string? route)
	{
		return (route ?? string.Empty).Trim() switch
		{
			"ResidentSoloParents" => new RegistryPageConfig
			{
				CategoryKey = "SOLO_PARENT",
				PageTitle = "Solo Parent Registry",
				TableTitle = "Solo Parent Directory",
				SearchPlaceholder = "Search solo parent records by name, contact, purok, or ID",
				EmptyStateLabel = "No solo parent residents found.",
				TotalMetricLabel = "solo parent residents",
				RegisteredSummaryLabel = "solo parent residents registered",
				FooterRecordLabel = "solo parent resident record(s)",
				BrowseTableMetaLabel = "Browse the solo parent registry, then select a row for quick actions.",
				EmptyTableMetaLabel = "Solo parent residents will appear here once records are loaded.",
				FilterMatchLabel = "solo parent resident(s)",
				SelectionSubjectLabel = "solo parent resident",
				NoRecordsSummaryLabel = "No solo parent records.",
				NoRecordsFooterLabel = "No solo parent resident records available."
			}, 
			"ResidentYouth" => new RegistryPageConfig
			{
				CategoryKey = "YOUTH",
				PageTitle = "Youth Registry",
				TableTitle = "Youth Directory",
				SearchPlaceholder = "Search youth records by name, contact, purok, or ID",
				EmptyStateLabel = "No youth residents found.",
				TotalMetricLabel = "youth residents",
				RegisteredSummaryLabel = "youth residents registered",
				FooterRecordLabel = "youth resident record(s)",
				BrowseTableMetaLabel = "Browse the youth registry, then select a row for quick actions.",
				EmptyTableMetaLabel = "Youth residents will appear here once records are loaded.",
				FilterMatchLabel = "youth resident(s)",
				SelectionSubjectLabel = "youth resident",
				NoRecordsSummaryLabel = "No youth records.",
				NoRecordsFooterLabel = "No youth resident records available."
			}, 
			"ResidentIndigent" => new RegistryPageConfig
			{
				CategoryKey = "INDIGENT",
				PageTitle = "Indigent Registry",
				TableTitle = "Indigent Directory",
				SearchPlaceholder = "Search indigent records by name, contact, purok, or ID",
				EmptyStateLabel = "No indigent residents found.",
				TotalMetricLabel = "indigent residents",
				RegisteredSummaryLabel = "indigent residents registered",
				FooterRecordLabel = "indigent resident record(s)",
				BrowseTableMetaLabel = "Browse the indigent registry, then select a row for quick actions.",
				EmptyTableMetaLabel = "Indigent residents will appear here once records are loaded.",
				FilterMatchLabel = "indigent resident(s)",
				SelectionSubjectLabel = "indigent resident",
				NoRecordsSummaryLabel = "No indigent records.",
				NoRecordsFooterLabel = "No indigent resident records available."
			}, 
			_ => new RegistryPageConfig
			{
				CategoryKey = "ALL",
				PageTitle = "Resident Records",
				TableTitle = "Resident Directory",
				SearchPlaceholder = "Search by resident name, contact, purok, or ID",
				EmptyStateLabel = "No residents found.",
				TotalMetricLabel = "residents",
				RegisteredSummaryLabel = "residents registered",
				FooterRecordLabel = "resident record(s)",
				BrowseTableMetaLabel = "Browse the full directory, then select a row for quick actions.",
				EmptyTableMetaLabel = "Residents will appear here once records are loaded.",
				FilterMatchLabel = "resident(s)",
				SelectionSubjectLabel = "resident",
				NoRecordsSummaryLabel = "No records.",
				NoRecordsFooterLabel = "No resident records available."
			}, 
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/residentmodulepage.xaml", UriKind.Relative);
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
			pageTitleText = (TextBlock)target;
			break;
		case 2:
			recordCountLabel = (TextBlock)target;
			break;
		case 3:
			totalResidentsMetric = (TextBlock)target;
			break;
		case 4:
			activeResidentsMetric = (TextBlock)target;
			break;
		case 5:
			purokResidentsMetric = (TextBlock)target;
			break;
		case 6:
			visibleResidentsMetric = (TextBlock)target;
			break;
		case 7:
			((Button)target).Click += BtnAdd_Click;
			break;
		case 8:
			searchBox = (TextBox)target;
			searchBox.TextChanged += SearchBox_TextChanged;
			break;
		case 9:
			searchPlaceholderText = (TextBlock)target;
			break;
		case 10:
			filterStatus = (ComboBox)target;
			filterStatus.SelectionChanged += Filter_SelectionChanged;
			break;
		case 11:
			filterPurok = (ComboBox)target;
			filterPurok.SelectionChanged += Filter_SelectionChanged;
			break;
		case 12:
			((Button)target).Click += BtnRefresh_Click;
			break;
		case 13:
			tableTitleText = (TextBlock)target;
			break;
		case 14:
			tableMetaLabel = (TextBlock)target;
			break;
		case 15:
			tableVisibleLabel = (TextBlock)target;
			break;
		case 16:
			tableSelectionLabel = (TextBlock)target;
			break;
		case 17:
			mainGrid = (DataGrid)target;
			mainGrid.SelectionChanged += MainGrid_SelectionChanged;
			break;
		case 18:
			emptyState = (StackPanel)target;
			break;
		case 19:
			emptyLabel = (TextBlock)target;
			break;
		case 20:
			contextActionBar = (Border)target;
			break;
		case 21:
			selectedResidentLabel = (TextBlock)target;
			break;
		case 22:
			selectedResidentMetaLabel = (TextBlock)target;
			break;
		case 23:
			btnContextBlotter = (Button)target;
			btnContextBlotter.Click += BtnContextBlotter_Click;
			break;
		case 24:
			btnContextBlotterHistory = (Button)target;
			btnContextBlotterHistory.Click += BtnContextBlotterHistory_Click;
			break;
		case 25:
			btnContextCert = (Button)target;
			btnContextCert.Click += BtnContextCert_Click;
			break;
		case 26:
			btnContextHousehold = (Button)target;
			btnContextHousehold.Click += BtnContextHousehold_Click;
			break;
		case 27:
			btnContextPayment = (Button)target;
			btnContextPayment.Click += BtnContextPayment_Click;
			break;
		case 28:
			((Button)target).Click += BtnClearSelection_Click;
			break;
		case 29:
			footerCountLabel = (TextBlock)target;
			break;
		case 30:
			btnEdit = (Button)target;
			btnEdit.Click += BtnEdit_Click;
			break;
		case 31:
			btnDelete = (Button)target;
			btnDelete.Click += BtnDelete_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
