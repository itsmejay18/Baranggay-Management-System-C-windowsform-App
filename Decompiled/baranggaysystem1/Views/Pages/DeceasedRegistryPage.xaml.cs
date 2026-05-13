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

public partial class DeceasedRegistryPage : UserControl
{
	private DataTable? _data;







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
		return Database.DatabaseManagerAsync.LoadTableAsync(
			@"SELECT r.resident_id,
			         COALESCE(r.first_name,'') || ' ' || COALESCE(r.middle_name,'') || ' ' || COALESCE(r.last_name,'') AS full_name,
			         r.sex AS gender,
			         r.birth_date,
			         r.civil_status,
			         COALESCE(p.name, '') AS purok,
			         r.contact_no,
			         r.updated_at AS recorded_at
			  FROM resident r
			  LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id
			  WHERE UPPER(COALESCE(r.status,'')) = 'DECEASED'
			    AND r.barangay_id = @barangayId
			  ORDER BY r.updated_at DESC",
			cmd => { cmd.Parameters.AddWithValue("@barangayId", UserSession.BarangayId); }).GetAwaiter().GetResult();
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
		// Simple input via a prompt - use the search box value as the resident ID
		string input = searchBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out int residentId) || residentId <= 0)
		{
			DialogService.Instance.ShowInfo("Type a Resident ID in the search box, then click this button to mark them as deceased.");
			return;
		}
		if (MessageBox.Show($"Mark Resident #{residentId} as DECEASED?\n\nThis action cannot be easily undone.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
			return;
		try
		{
			int rows = Database.DbHelper.ExecuteNonQuery(
				"UPDATE resident SET status = 'DECEASED', updated_at = CURRENT_TIMESTAMP WHERE resident_id = @id AND barangay_id = @bid",
				cmd => {
					cmd.Parameters.AddWithValue("@id", residentId);
					cmd.Parameters.AddWithValue("@bid", UserSession.BarangayId);
				});
			if (rows > 0)
			{
				DialogService.Instance.ShowInfo($"Resident #{residentId} has been marked as deceased.");
				searchBox.Text = string.Empty;
				_ = LoadAsync();
			}
			else
			{
				DialogService.Instance.ShowWarning("No matching resident found with that ID.");
			}
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowWarning("Failed to update: " + ex.Message);
		}
	}}
