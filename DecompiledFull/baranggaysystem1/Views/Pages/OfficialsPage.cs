using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using baranggaysystem1.Database;
using baranggaysystem1.ViewModels;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Pages;

public class OfficialsPage : UserControl, IComponentConnector
{
	private DataTable? _data;

	internal TextBlock recordCountLabel;

	internal TextBox searchBox;

	internal DataGrid mainGrid;

	internal StackPanel emptyState;

	internal TextBlock emptyLabel;

	internal Border contextActionBar;

	internal TextBlock selectedRecordLabel;

	internal Button btnEditOfficial;

	internal Button btnTerminate;

	internal Button btnCertOfService;

	internal TextBlock footerCountLabel;

	private bool _contentLoaded;

	public OfficialsPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public OfficialsPage(string route)
		: this()
	{
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
			AppLogger.LogError("OfficialsPage load failed.", ex);
			emptyLabel.Text = "Failed to load data. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
		}
	}

	private async Task<DataTable> FetchData()
	{
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT o.official_id,\n                       r.first_name || ' ' || r.last_name AS official_name,\n                       o.position,\n                       o.committee,\n                       o.status,\n                       t.term_start || ' to ' || t.term_end AS term_period\n                FROM barangay_official o\n                INNER JOIN resident r ON r.resident_id = o.resident_id\n                INNER JOIN official_term t ON t.term_id = o.term_id\n                ORDER BY o.status ASC, o.position DESC");
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		if (table == null || table.Rows.Count == 0)
		{
			mainGrid.ItemsSource = null;
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "No officials registered.";
			recordCountLabel.Text = "No active officials.";
			return;
		}
		emptyState.Visibility = Visibility.Collapsed;
		mainGrid.Columns.Clear();
		(string, string, double)[] array = new(string, string, double)[5]
		{
			("Full Name", "official_name", 180.0),
			("Position", "position", 140.0),
			("Committee", "committee", 140.0),
			("Status", "status", 100.0),
			("Term", "term_period", 160.0)
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
		footerCountLabel.Text = $"Showing {table.Rows.Count:N0} official(s)";
		recordCountLabel.Text = $"{table.Rows.Count:N0} records found";
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (_data != null)
		{
			string q = searchBox.Text.Trim();
			_data.DefaultView.RowFilter = (string.IsNullOrWhiteSpace(q) ? string.Empty : string.Join(" OR ", from DataColumn c in _data.Columns
				select "[" + c.ColumnName + "] LIKE '%" + q.Replace("'", "''") + "%'"));
			emptyState.Visibility = ((_data.DefaultView.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			contextActionBar.Visibility = Visibility.Visible;
			selectedRecordLabel.Text = dataRowView["official_name"]?.ToString() ?? "Unknown Official";
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

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		DialogService.Instance.ShowInfo("Use the Resident module to appoint new officials from existing residents.");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/officialspage.xaml", UriKind.Relative);
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
			((Button)target).Click += BtnAdd_Click;
			break;
		case 3:
			searchBox = (TextBox)target;
			searchBox.TextChanged += SearchBox_TextChanged;
			break;
		case 4:
			((Button)target).Click += BtnRefresh_Click;
			break;
		case 5:
			mainGrid = (DataGrid)target;
			mainGrid.SelectionChanged += MainGrid_SelectionChanged;
			break;
		case 6:
			emptyState = (StackPanel)target;
			break;
		case 7:
			emptyLabel = (TextBlock)target;
			break;
		case 8:
			contextActionBar = (Border)target;
			break;
		case 9:
			selectedRecordLabel = (TextBlock)target;
			break;
		case 10:
			btnEditOfficial = (Button)target;
			break;
		case 11:
			btnTerminate = (Button)target;
			break;
		case 12:
			btnCertOfService = (Button)target;
			break;
		case 13:
			((Button)target).Click += BtnClearSelection_Click;
			break;
		case 14:
			footerCountLabel = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
