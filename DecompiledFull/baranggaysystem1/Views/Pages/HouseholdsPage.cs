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

public class HouseholdsPage : UserControl, IComponentConnector
{
	private const string AllPuroksOption = "All Puroks";

	private DataTable? _data;

	private readonly ResidentsModuleDataService _dataService;

	private readonly HouseholdRepository _householdRepository;

	private readonly int _barangayId;

	private bool _isLoadingFilters;

	private DateTime _lastLoadedAt;

	internal TextBlock recordCountLabel;

	internal TextBlock headerVisibleLabel;

	internal TextBlock headerCoverageLabel;

	internal TextBlock headerMembersLabel;

	internal Button btnExportCsv;

	internal TextBlock totalHouseholdsMetric;

	internal TextBlock visibleHouseholdsMetric;

	internal TextBlock totalMembersMetric;

	internal TextBlock purokCoverageMetric;

	internal TextBox searchBox;

	internal TextBlock searchPlaceholderText;

	internal ComboBox purokFilterCombo;

	internal TextBlock toolbarInsightLabel;

	internal TextBlock activeFilterLabel;

	internal TextBlock tableSummaryLabel;

	internal DataGrid mainGrid;

	internal StackPanel emptyState;

	internal TextBlock emptyLabel;

	internal Border contextActionBar;

	internal TextBlock selectedRecordLabel;

	internal TextBlock selectedRecordMetaLabel;

	internal Button btnEditDetails;

	internal Button btnViewMembers;

	internal Button btnTransferHistory;

	internal Button btnCertificate;

	internal Button btnTransfer;

	internal Button btnDeleteHousehold;

	internal TextBlock footerCountLabel;

	internal TextBlock lastRefreshLabel;

	private bool _contentLoaded;

	public HouseholdsPage()
	{
		InitializeComponent();
		_dataService = new ResidentsModuleDataService();
		_householdRepository = new HouseholdRepository();
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		_lastLoadedAt = DateTime.Now;
		btnDeleteHousehold.IsEnabled = Permissions.CanDeleteHouseholds;
		btnTransfer.IsEnabled = Permissions.CanTransferHouseholds;
		btnCertificate.IsEnabled = Permissions.CanIssueCertificates;
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public HouseholdsPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		try
		{
			_data = await FetchData();
			_lastLoadedAt = DateTime.Now;
			BindGrid(_data);
			PopulatePurokOptions(_data);
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("HouseholdsPage load failed.", ex);
			_data = null;
			BindGrid(null);
			PopulatePurokOptions(null);
			ApplyFilters();
			emptyLabel.Text = "Failed to load household records. Please refresh the registry.";
			emptyState.Visibility = Visibility.Visible;
			tableSummaryLabel.Text = "The registry could not be loaded.";
			toolbarInsightLabel.Text = "Household data is unavailable right now.";
		}
	}

	private async Task<DataTable> FetchData()
	{
		return await _dataService.LoadHouseholdsAsync(string.Empty, null);
	}

	private void BindGrid(DataTable? table)
	{
		mainGrid.SelectedItem = null;
		contextActionBar.Visibility = Visibility.Collapsed;
		mainGrid.ItemsSource = table?.DefaultView;
	}

	private void PopulatePurokOptions(DataTable? table)
	{
		_isLoadingFilters = true;
		try
		{
			string currentSelection = (purokFilterCombo.SelectedItem as string) ?? "All Puroks";
			List<string> list = new List<string> { "All Puroks" };
			if (table != null && table.Columns.Contains("purok_display"))
			{
				list.AddRange((from row in table.AsEnumerable()
					select NormalizeText(row["purok_display"], "No purok") into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string value) => value, StringComparer.OrdinalIgnoreCase));
			}
			purokFilterCombo.ItemsSource = list;
			purokFilterCombo.SelectedItem = list.FirstOrDefault((string option) => string.Equals(option, currentSelection, StringComparison.OrdinalIgnoreCase)) ?? "All Puroks";
		}
		finally
		{
			_isLoadingFilters = false;
		}
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void PurokFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isLoadingFilters)
		{
			ApplyFilters();
		}
	}

	private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
	{
		_isLoadingFilters = true;
		try
		{
			searchBox.Text = string.Empty;
			purokFilterCombo.SelectedItem = "All Puroks";
		}
		finally
		{
			_isLoadingFilters = false;
		}
		ApplyFilters();
	}

	private void ApplyFilters()
	{
		searchPlaceholderText.Visibility = ((!string.IsNullOrWhiteSpace(searchBox.Text)) ? Visibility.Collapsed : Visibility.Visible);
		if (_data == null)
		{
			UpdateInsights(Array.Empty<DataRowView>(), 0, 0);
			return;
		}
		string value = searchBox.Text.Trim();
		string text = (purokFilterCombo.SelectedItem as string) ?? "All Puroks";
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(value))
		{
			string escapedSearch = EscapeRowFilterValue(value);
			string[] source = new string[6] { "household_id", "purok_display", "street_display", "member_count", "coordinates_display", "updated_display" };
			string text2 = string.Join(" OR ", from column in source
				where _data.Columns.Contains(column)
				select $"CONVERT([{column}], 'System.String') LIKE '%{escapedSearch}%'");
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add("(" + text2 + ")");
			}
		}
		if (!string.Equals(text, "All Puroks", StringComparison.OrdinalIgnoreCase) && _data.Columns.Contains("purok_display"))
		{
			list.Add("[purok_display] = '" + text.Replace("'", "''") + "'");
		}
		_data.DefaultView.RowFilter = ((list.Count == 0) ? string.Empty : string.Join(" AND ", list));
		DataRowView[] array = _data.DefaultView.Cast<DataRowView>().ToArray();
		int count = _data.Rows.Count;
		int distinctPurokCount = GetDistinctPurokCount(from row in _data.AsEnumerable()
			select row["purok_display"]);
		object selectedItem = mainGrid.SelectedItem;
		DataRowView selectedRow = selectedItem as DataRowView;
		if (selectedRow != null && !array.Any((DataRowView row) => row.Row == selectedRow.Row))
		{
			mainGrid.SelectedItem = null;
		}
		UpdateInsights(array, count, distinctPurokCount);
	}

	private void UpdateInsights(IReadOnlyCollection<DataRowView> visibleRows, int totalCount, int totalPuroks)
	{
		int count = visibleRows.Count;
		int value = visibleRows.Sum((DataRowView row) => GetInt(row, "member_count"));
		int distinctPurokCount = GetDistinctPurokCount(visibleRows.Select((DataRowView row) => row["purok_display"]));
		string selectedPurok = (purokFilterCombo.SelectedItem as string) ?? "All Puroks";
		string search = searchBox.Text.Trim();
		recordCountLabel.Text = ((totalCount == 0) ? "No households are registered yet." : $"{totalCount:N0} households registered across {totalPuroks:N0} purok(s).");
		headerVisibleLabel.Text = $"{count:N0} visible now";
		headerCoverageLabel.Text = $"{distinctPurokCount:N0} purok(s) covered";
		headerMembersLabel.Text = $"{value:N0} listed member(s)";
		totalHouseholdsMetric.Text = totalCount.ToString("N0");
		visibleHouseholdsMetric.Text = count.ToString("N0");
		totalMembersMetric.Text = value.ToString("N0");
		purokCoverageMetric.Text = distinctPurokCount.ToString("N0");
		toolbarInsightLabel.Text = ((totalCount == 0) ? "Create a household to start building the registry." : ((count == totalCount) ? "Showing the full household registry." : $"Showing {count:N0} filtered household(s)."));
		activeFilterLabel.Text = BuildFilterSummary(search, selectedPurok);
		tableSummaryLabel.Text = ((totalCount == 0) ? "No records loaded yet." : $"{count:N0} result(s) ready for review and household actions.");
		footerCountLabel.Text = ((totalCount == 0) ? "No households loaded." : $"Showing {count:N0} household(s) and {value:N0} visible member(s).");
		lastRefreshLabel.Text = $"Last refreshed {_lastLoadedAt:MMM dd, yyyy hh:mm tt}";
		emptyLabel.Text = ((totalCount == 0) ? "No household records have been added yet." : "No households match the current search or purok filter.");
		emptyState.Visibility = ((count != 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private static string BuildFilterSummary(string search, string selectedPurok)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(search))
		{
			list.Add("Search: \"" + search + "\"");
		}
		if (!string.Equals(selectedPurok, "All Puroks", StringComparison.OrdinalIgnoreCase))
		{
			list.Add("Purok: " + selectedPurok);
		}
		if (list.Count != 0)
		{
			return "Filters: " + string.Join(" | ", list);
		}
		return "Filters: All households and all puroks.";
	}

	private static string EscapeRowFilterValue(string value)
	{
		return value.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]")
			.Replace("*", "[*]");
	}

	private static int GetDistinctPurokCount(IEnumerable<object?> values)
	{
		return (from value in values
			select NormalizeText(value, "No purok") into value
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct<string>(StringComparer.OrdinalIgnoreCase).Count();
	}

	private static string NormalizeText(object? value, string fallback)
	{
		string text = Convert.ToString(value)?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(text) && !(text == "-"))
		{
			return text;
		}
		return fallback;
	}

	private static int GetInt(DataRowView row, string columnName)
	{
		try
		{
			return Convert.ToInt32(row[columnName]);
		}
		catch
		{
			return 0;
		}
	}

	private static string GetText(DataRowView row, string columnName, string fallback)
	{
		return NormalizeText(row[columnName], fallback);
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			contextActionBar.Visibility = Visibility.Visible;
			selectedRecordLabel.Text = $"Household #{dataRowView["household_id"]} - {GetText(dataRowView, "street_display", "No address")}";
			selectedRecordMetaLabel.Text = $"{GetText(dataRowView, "purok_display", "No purok")} | {GetInt(dataRowView, "member_count")} member(s) | {GetText(dataRowView, "coordinates_display", "No coordinates saved")}";
		}
		else
		{
			contextActionBar.Visibility = Visibility.Collapsed;
		}
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.SelectedItem = null;
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnDelete_Click(object sender, RoutedEventArgs e)
	{
		if (!Permissions.CanDeleteHouseholds)
		{
			DialogService.Instance.ShowWarning("You do not have permission to delete household records.");
			return;
		}
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a household to delete.");
			return;
		}
		int num = Convert.ToInt32(dataRowView["household_id"]);
		string value = Convert.ToString(dataRowView["street_display"]) ?? $"Household #{num}";
		if (!DialogService.Instance.Confirm($"Delete household #{num}?\n\nAddress: {value}\n\nThis only works when no members are currently assigned."))
		{
			return;
		}
		try
		{
			if (_householdRepository.TryDelete(num, _barangayId, out string message))
			{
				mainGrid.SelectedItem = null;
				DialogService.Instance.ShowInfo("Household deleted successfully.");
				await LoadAsync();
			}
			else
			{
				DialogService.Instance.ShowWarning(message, "Delete Household");
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Household delete failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Delete Household");
		}
	}

	private void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a household first.");
			return;
		}
		HouseholdDetailsWindow window = new HouseholdDetailsWindow(Convert.ToInt32(dataRowView["household_id"]));
		if (DialogService.Instance.ShowDialog(window) == true)
		{
			LoadAsync();
		}
	}

	private void BtnTransferHistory_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a household first.");
			return;
		}
		int num = Convert.ToInt32(dataRowView["household_id"]);
		string text = GetText(dataRowView, "street_display", $"Household #{num}");
		HouseholdTransferHistoryWindow window = new HouseholdTransferHistoryWindow(num, text);
		DialogService.Instance.ShowDialog(window);
	}

	private void BtnEditDetails_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a household to edit.");
		}
		else
		{
			OpenHouseholdEditor(Convert.ToInt32(dataRowView["household_id"]), openFamilyManagerAfterSave: false);
		}
	}

	private void BtnTransfer_Click(object sender, RoutedEventArgs e)
	{
		if (!Permissions.CanTransferHouseholds)
		{
			DialogService.Instance.ShowWarning("You do not have permission to transfer household members.");
			return;
		}
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a household to transfer.");
			return;
		}
		try
		{
			HouseholdTransferWindow window = new HouseholdTransferWindow(Convert.ToInt32(dataRowView["household_id"]));
			if (DialogService.Instance.ShowDialog(window) == true)
			{
				LoadAsync();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Unable to open household transfer dialog.", ex);
			DialogService.Instance.ShowError(ex.Message, "Transfer Family");
		}
	}

	private void BtnCertificate_Click(object sender, RoutedEventArgs e)
	{
		if (!Permissions.CanIssueCertificates)
		{
			DialogService.Instance.ShowWarning("You do not have permission to generate household certificates.");
			return;
		}
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a household first.");
			return;
		}
		try
		{
			HouseholdCertificateWindow window = new HouseholdCertificateWindow(Convert.ToInt32(dataRowView["household_id"]));
			DialogService.Instance.ShowDialog(window);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Unable to open household certificate dialog.", ex);
			DialogService.Instance.ShowError(ex.Message, "Household Certificate");
		}
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		OpenHouseholdEditor(null, openFamilyManagerAfterSave: true);
	}

	private void OpenHouseholdEditor(int? householdId, bool openFamilyManagerAfterSave)
	{
		HouseholdEditorWindow householdEditorWindow = new HouseholdEditorWindow(householdId);
		if (DialogService.Instance.ShowDialog(householdEditorWindow) == true)
		{
			if (openFamilyManagerAfterSave && householdEditorWindow.SavedHouseholdId > 0)
			{
				HouseholdDetailsWindow window = new HouseholdDetailsWindow(householdEditorWindow.SavedHouseholdId)
				{
					Owner = Window.GetWindow((DependencyObject)(object)this)
				};
				DialogService.Instance.ShowDialog(window);
			}
			LoadAsync();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/householdspage.xaml", UriKind.Relative);
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
			headerVisibleLabel = (TextBlock)target;
			break;
		case 3:
			headerCoverageLabel = (TextBlock)target;
			break;
		case 4:
			headerMembersLabel = (TextBlock)target;
			break;
		case 5:
			btnExportCsv = (Button)target;
			break;
		case 6:
			((Button)target).Click += BtnAdd_Click;
			break;
		case 7:
			totalHouseholdsMetric = (TextBlock)target;
			break;
		case 8:
			visibleHouseholdsMetric = (TextBlock)target;
			break;
		case 9:
			totalMembersMetric = (TextBlock)target;
			break;
		case 10:
			purokCoverageMetric = (TextBlock)target;
			break;
		case 11:
			searchBox = (TextBox)target;
			searchBox.TextChanged += SearchBox_TextChanged;
			break;
		case 12:
			searchPlaceholderText = (TextBlock)target;
			break;
		case 13:
			purokFilterCombo = (ComboBox)target;
			purokFilterCombo.SelectionChanged += PurokFilterCombo_SelectionChanged;
			break;
		case 14:
			toolbarInsightLabel = (TextBlock)target;
			break;
		case 15:
			activeFilterLabel = (TextBlock)target;
			break;
		case 16:
			((Button)target).Click += BtnClearFilters_Click;
			break;
		case 17:
			((Button)target).Click += BtnRefresh_Click;
			break;
		case 18:
			tableSummaryLabel = (TextBlock)target;
			break;
		case 19:
			mainGrid = (DataGrid)target;
			mainGrid.SelectionChanged += MainGrid_SelectionChanged;
			break;
		case 20:
			emptyState = (StackPanel)target;
			break;
		case 21:
			emptyLabel = (TextBlock)target;
			break;
		case 22:
			contextActionBar = (Border)target;
			break;
		case 23:
			selectedRecordLabel = (TextBlock)target;
			break;
		case 24:
			selectedRecordMetaLabel = (TextBlock)target;
			break;
		case 25:
			btnEditDetails = (Button)target;
			btnEditDetails.Click += BtnEditDetails_Click;
			break;
		case 26:
			btnViewMembers = (Button)target;
			btnViewMembers.Click += BtnEdit_Click;
			break;
		case 27:
			btnTransferHistory = (Button)target;
			btnTransferHistory.Click += BtnTransferHistory_Click;
			break;
		case 28:
			btnCertificate = (Button)target;
			btnCertificate.Click += BtnCertificate_Click;
			break;
		case 29:
			btnTransfer = (Button)target;
			btnTransfer.Click += BtnTransfer_Click;
			break;
		case 30:
			btnDeleteHousehold = (Button)target;
			btnDeleteHousehold.Click += BtnDelete_Click;
			break;
		case 31:
			((Button)target).Click += BtnClearSelection_Click;
			break;
		case 32:
			footerCountLabel = (TextBlock)target;
			break;
		case 33:
			lastRefreshLabel = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
