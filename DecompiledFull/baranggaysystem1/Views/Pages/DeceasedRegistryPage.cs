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
using baranggaysystem1.ViewModels;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Pages;

public class DeceasedRegistryPage : UserControl, IComponentConnector
{
	private DataTable? _data;

	internal TextBlock recordCountLabel;

	internal StackPanel toolbarPanel;

	internal TextBox searchBox;

	internal DataGrid mainGrid;

	internal StackPanel emptyState;

	internal TextBlock emptyLabel;

	internal TextBlock footerCountLabel;

	private bool _contentLoaded;

	public DeceasedRegistryPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public DeceasedRegistryPage(string route)
		: this()
	{
	}

	private async Task LoadAsync()
	{
		try
		{
			_data = await Task.Run((Func<DataTable>)FetchData);
			ApplyDataToGrid(_data);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DeceasedRegistryPage load failed.", ex);
			emptyState.Visibility = Visibility.Visible;
		}
	}

	private static DataTable FetchData()
	{
		return new DataTable();
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		if (table == null || table.Rows.Count == 0)
		{
			mainGrid.ItemsSource = null;
			emptyState.Visibility = Visibility.Visible;
			return;
		}
		emptyState.Visibility = Visibility.Collapsed;
		mainGrid.Columns.Clear();
		foreach (DataColumn column in table.Columns)
		{
			mainGrid.Columns.Add(new DataGridTextColumn
			{
				Header = column.ColumnName,
				Binding = new Binding(column.ColumnName),
				Width = new DataGridLength(1.0, DataGridLengthUnitType.Star)
			});
		}
		mainGrid.ItemsSource = table.DefaultView;
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
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		DialogService.Instance.ShowInfo("Deceased registry is not yet implemented.");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/deceasedregistrypage.xaml", UriKind.Relative);
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
			toolbarPanel = (StackPanel)target;
			break;
		case 3:
			((Button)target).Click += BtnAdd_Click;
			break;
		case 4:
			searchBox = (TextBox)target;
			searchBox.TextChanged += SearchBox_TextChanged;
			break;
		case 5:
			((Button)target).Click += BtnRefresh_Click;
			break;
		case 6:
			mainGrid = (DataGrid)target;
			mainGrid.SelectionChanged += MainGrid_SelectionChanged;
			break;
		case 7:
			emptyState = (StackPanel)target;
			break;
		case 8:
			emptyLabel = (TextBlock)target;
			break;
		case 9:
			footerCountLabel = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
