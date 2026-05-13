using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Pages;

public class TagsCategoriesPage : UserControl, IComponentConnector
{
	private readonly ResidentClassificationService _service = new ResidentClassificationService();

	private IReadOnlyList<ResidentClassificationRecord> _records = Array.Empty<ResidentClassificationRecord>();

	private bool _isLoaded;

	internal TextBlock recordCountLabel;

	internal Button btnAdd;

	internal TextBox searchBox;

	internal ComboBox typeFilter;

	internal ComboBox statusFilter;

	internal Button btnRefresh;

	internal DataGrid mainGrid;

	internal StackPanel emptyState;

	internal TextBlock emptyLabel;

	internal Border contextActionBar;

	internal TextBlock selectedRecordLabel;

	internal TextBlock selectedRecordMetaLabel;

	internal Button btnEdit;

	internal Button btnToggleStatus;

	internal TextBlock toggleStatusText;

	internal Button btnDelete;

	internal TextBlock footerCountLabel;

	private bool _contentLoaded;

	private bool CanManage => ResidentClassificationService.CanManageClassifications();

	private ResidentClassificationRecord? SelectedRecord => mainGrid.SelectedItem as ResidentClassificationRecord;

	public TagsCategoriesPage()
	{
		InitializeComponent();
		ConfigureFilters();
		base.Loaded += async delegate
		{
			if (!_isLoaded)
			{
				_isLoaded = true;
				await LoadAsync();
			}
		};
	}

	public TagsCategoriesPage(string route)
		: this()
	{
	}

	private void ConfigureFilters()
	{
		typeFilter.ItemsSource = new string[3] { "All Types", "Categories", "Tags" };
		statusFilter.ItemsSource = new string[3] { "All Statuses", "Active", "Archived" };
		typeFilter.SelectedIndex = 0;
		statusFilter.SelectedIndex = 0;
	}

	private async Task LoadAsync(int? selectId = null)
	{
		if (!CanManage)
		{
			btnAdd.IsEnabled = false;
			btnRefresh.IsEnabled = false;
			mainGrid.ItemsSource = null;
			contextActionBar.Visibility = Visibility.Collapsed;
			emptyLabel.Text = "Only administrator accounts can manage tags and categories.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Access restricted.";
			recordCountLabel.Text = "Access restricted.";
			return;
		}
		try
		{
			SetLoadingState(isLoading: true);
			_records = await _service.GetClassificationsAsync();
			ApplyFilters(selectId);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("TagsCategoriesPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load tags and categories. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Tags and categories could not be loaded.";
		}
		finally
		{
			SetLoadingState(isLoading: false);
		}
	}

	private void SetLoadingState(bool isLoading)
	{
		btnAdd.IsEnabled = !isLoading && CanManage;
		btnRefresh.IsEnabled = !isLoading && CanManage;
		footerCountLabel.Text = (isLoading ? "Loading tags and categories..." : footerCountLabel.Text);
	}

	private void ApplyFilters(int? selectId = null)
	{
		string query = searchBox.Text.Trim();
		string type = Convert.ToString(typeFilter.SelectedItem) ?? "All Types";
		string status = Convert.ToString(statusFilter.SelectedItem) ?? "All Statuses";
		List<ResidentClassificationRecord> list = (from record in _records
			where MatchesSearch(record, query)
			where MatchesType(record, type)
			where MatchesStatus(record, status)
			select record).ToList();
		mainGrid.ItemsSource = list;
		emptyLabel.Text = (string.IsNullOrWhiteSpace(query) ? "No tags or categories found." : "No matching tags or categories found.");
		emptyState.Visibility = ((list.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		int num = _records.Count((ResidentClassificationRecord record) => string.Equals(record.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase));
		int value = _records.Count - num;
		recordCountLabel.Text = $"{num:N0} active, {value:N0} archived classification(s).";
		footerCountLabel.Text = $"Showing {list.Count:N0} of {_records.Count:N0} tag/category record(s).";
		if (selectId.HasValue)
		{
			ResidentClassificationRecord residentClassificationRecord = list.FirstOrDefault((ResidentClassificationRecord record) => record.ClassificationId == selectId.Value);
			mainGrid.SelectedItem = residentClassificationRecord;
			if (residentClassificationRecord != null)
			{
				mainGrid.ScrollIntoView(residentClassificationRecord);
			}
		}
		UpdateSelectionState();
	}

	private static bool MatchesSearch(ResidentClassificationRecord record, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return true;
		}
		if (!Contains(record.Name, query) && !Contains(record.Description, query) && !Contains(record.TypeDisplay, query) && !Contains(record.StatusDisplay, query))
		{
			return Contains(record.SourceDisplay, query);
		}
		return true;
	}

	private static bool MatchesType(ResidentClassificationRecord record, string type)
	{
		if (string.Equals(type, "Categories", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.ClassificationType, "CATEGORY", StringComparison.OrdinalIgnoreCase);
		}
		if (string.Equals(type, "Tags", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.ClassificationType, "TAG", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool MatchesStatus(ResidentClassificationRecord record, string status)
	{
		if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
		}
		if (string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(record.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool Contains(string? value, string query)
	{
		return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (mainGrid != null)
		{
			ApplyFilters();
		}
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateSelectionState();
	}

	private void UpdateSelectionState()
	{
		ResidentClassificationRecord selectedRecord = SelectedRecord;
		if (selectedRecord == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			return;
		}
		contextActionBar.Visibility = Visibility.Visible;
		selectedRecordLabel.Text = selectedRecord.Name;
		selectedRecordMetaLabel.Text = $"{selectedRecord.TypeDisplay} - {selectedRecord.StatusDisplay} - {selectedRecord.SourceDisplay} - {selectedRecord.UsageDisplay}";
		toggleStatusText.Text = (string.Equals(selectedRecord.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "Reactivate" : "Archive");
		btnDelete.IsEnabled = !selectedRecord.IsSystem && selectedRecord.UsageCount == 0;
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.SelectedItem = null;
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync(SelectedRecord?.ClassificationId);
	}

	private async void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		ResidentClassificationWindow residentClassificationWindow = new ResidentClassificationWindow();
		if (DialogService.Instance.ShowDialog(residentClassificationWindow) == true)
		{
			await SaveAndReloadAsync(residentClassificationWindow.Draft);
		}
	}

	private async void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		await OpenSelectedRecordAsync();
	}

	private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		await OpenSelectedRecordAsync();
	}

	private async Task OpenSelectedRecordAsync()
	{
		ResidentClassificationRecord selectedRecord = SelectedRecord;
		if (selectedRecord == null)
		{
			DialogService.Instance.ShowWarning("Please select a tag or category to edit.", "Tags & Categories");
			return;
		}
		ResidentClassificationRecord residentClassificationRecord = await _service.GetClassificationAsync(selectedRecord.ClassificationId);
		if (residentClassificationRecord == null)
		{
			DialogService.Instance.ShowWarning("The selected tag or category could not be found anymore.", "Tags & Categories");
			await LoadAsync();
			return;
		}
		ResidentClassificationWindow residentClassificationWindow = new ResidentClassificationWindow(residentClassificationRecord);
		if (DialogService.Instance.ShowDialog(residentClassificationWindow) == true)
		{
			await SaveAndReloadAsync(residentClassificationWindow.Draft);
		}
	}

	private async Task SaveAndReloadAsync(ResidentClassificationRecord draft)
	{
		_ = 1;
		try
		{
			await LoadAsync(await _service.SaveClassificationAsync(draft));
		}
		catch (Exception ex)
		{
			AppLogger.LogError("TagsCategoriesPage save failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Tags & Categories");
		}
	}

	private async void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
	{
		ResidentClassificationRecord record = SelectedRecord;
		if (record == null)
		{
			DialogService.Instance.ShowWarning("Please select a tag or category first.", "Tags & Categories");
			return;
		}
		string text = (string.Equals(record.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "ARCHIVED");
		string value = (string.Equals(text, "ACTIVE", StringComparison.OrdinalIgnoreCase) ? "reactivate" : "archive");
		if (!DialogService.Instance.Confirm($"Do you want to {value} '{record.Name}'?", "Tags & Categories"))
		{
			return;
		}
		try
		{
			await _service.SetStatusAsync(record.ClassificationId, text);
			await LoadAsync(record.ClassificationId);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("TagsCategoriesPage status update failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Tags & Categories");
		}
	}

	private async void BtnDelete_Click(object sender, RoutedEventArgs e)
	{
		ResidentClassificationRecord selectedRecord = SelectedRecord;
		if (selectedRecord == null)
		{
			DialogService.Instance.ShowWarning("Please select a tag or category to delete.", "Tags & Categories");
		}
		else if (selectedRecord.IsSystem)
		{
			DialogService.Instance.ShowWarning("System classifications cannot be deleted.", "Tags & Categories");
		}
		else if (selectedRecord.UsageCount > 0)
		{
			DialogService.Instance.ShowWarning("This classification is still used by resident records.", "Tags & Categories");
		}
		else if (DialogService.Instance.Confirm("Delete '" + selectedRecord.Name + "' permanently?", "Tags & Categories"))
		{
			try
			{
				await _service.DeleteClassificationAsync(selectedRecord.ClassificationId);
				await LoadAsync();
			}
			catch (Exception ex)
			{
				AppLogger.LogError("TagsCategoriesPage delete failed.", ex);
				DialogService.Instance.ShowError(ex.Message, "Tags & Categories");
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/tagscategoriespage.xaml", UriKind.Relative);
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
			btnAdd = (Button)target;
			btnAdd.Click += BtnAdd_Click;
			break;
		case 3:
			searchBox = (TextBox)target;
			searchBox.TextChanged += SearchBox_TextChanged;
			break;
		case 4:
			typeFilter = (ComboBox)target;
			typeFilter.SelectionChanged += Filter_SelectionChanged;
			break;
		case 5:
			statusFilter = (ComboBox)target;
			statusFilter.SelectionChanged += Filter_SelectionChanged;
			break;
		case 6:
			btnRefresh = (Button)target;
			btnRefresh.Click += BtnRefresh_Click;
			break;
		case 7:
			mainGrid = (DataGrid)target;
			mainGrid.SelectionChanged += MainGrid_SelectionChanged;
			mainGrid.MouseDoubleClick += MainGrid_MouseDoubleClick;
			break;
		case 8:
			emptyState = (StackPanel)target;
			break;
		case 9:
			emptyLabel = (TextBlock)target;
			break;
		case 10:
			contextActionBar = (Border)target;
			break;
		case 11:
			selectedRecordLabel = (TextBlock)target;
			break;
		case 12:
			selectedRecordMetaLabel = (TextBlock)target;
			break;
		case 13:
			btnEdit = (Button)target;
			btnEdit.Click += BtnEdit_Click;
			break;
		case 14:
			btnToggleStatus = (Button)target;
			btnToggleStatus.Click += BtnToggleStatus_Click;
			break;
		case 15:
			toggleStatusText = (TextBlock)target;
			break;
		case 16:
			btnDelete = (Button)target;
			btnDelete.Click += BtnDelete_Click;
			break;
		case 17:
			((Button)target).Click += BtnClearSelection_Click;
			break;
		case 18:
			footerCountLabel = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
