using System;
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
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Pages;

public partial class PermitsPage : UserControl
{
	private DataTable? _data;







	public PermitsPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public PermitsPage(string route)
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
			AppLogger.LogError("PermitsPage load failed.", ex);
			emptyState.Visibility = Visibility.Visible;
		}
	}

	private static DataTable FetchData()
	{
		return Database.DatabaseManagerAsync.LoadTableAsync(
			@"SELECT c.certificate_id,
			         COALESCE(r.first_name,'') || ' ' || COALESCE(r.last_name,'') AS resident_name,
			         COALESCE(dt.name, 'Unknown') AS document_type,
			         c.or_number,
			         c.purpose,
			         c.status,
			         c.issued_at,
			         c.expires_at,
			         c.created_at
			  FROM certificate c
			  LEFT JOIN resident r ON r.resident_id = c.resident_id
			  LEFT JOIN document_type dt ON dt.document_type_id = c.document_type_id
			  WHERE c.barangay_id = @barangayId
			    AND UPPER(COALESCE(dt.code,'')) IN ('BUS','PERMIT')
			  ORDER BY c.created_at DESC",
			cmd => { cmd.Parameters.AddWithValue("@barangayId", UserSession.BarangayId); }).GetAwaiter().GetResult();
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		if (table == null || table.Rows.Count == 0)
		{
			mainGrid.ItemsSource = null;
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "No permit records found.";
			recordCountLabel.Text = "No records.";
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
		footerCountLabel.Text = $"Showing {table.Rows.Count:N0} record(s)";
		recordCountLabel.Text = $"{table.Rows.Count:N0} permit requests";
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
		DialogService.Instance.ShowInfo("To request a Business Permit, use the Clearances page and select 'Business Clearance' as the document type.");
	}}
