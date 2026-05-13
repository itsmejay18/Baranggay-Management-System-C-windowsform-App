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
using baranggaysystem1.helper;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;

namespace baranggaysystem1.Views.Pages;

public partial class StaffManagementPage : UserControl
{
	private DataTable? _data;

	private readonly StaffService _staffService;











	public StaffManagementPage()
	{
		InitializeComponent();
		_staffService = new StaffService();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public StaffManagementPage(string route)
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
			AppLogger.LogError("StaffManagementPage load failed.", ex);
			emptyLabel.Text = "Failed to load data. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
		}
	}

	private async Task<DataTable> FetchData()
	{
		return await _staffService.GetStaffsAsync();
	}

	private void ApplyDataToGrid(DataTable? table)
	{
		if (table == null || table.Rows.Count == 0)
		{
			mainGrid.ItemsSource = null;
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "No staff accounts found.";
			recordCountLabel.Text = "No records.";
			return;
		}
		emptyState.Visibility = Visibility.Collapsed;
		mainGrid.Columns.Clear();
		(string, string, double)[] array = new(string, string, double)[7]
		{
			("ID", "user_id", 60.0),
			("Full Name", "full_name", 160.0),
			("Username", "username", 110.0),
			("Role", "role_name", 120.0),
			("Department", "department", 140.0),
			("Date Created", "date_created", 120.0),
			("Active", "is_active", 80.0)
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
		footerCountLabel.Text = $"Showing {table.Rows.Count:N0} staff account(s)";
		recordCountLabel.Text = $"{table.Rows.Count:N0} users registered";
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (_data != null)
		{
			string q = searchBox.Text.Trim();
			_data.DefaultView.RowFilter = (string.IsNullOrWhiteSpace(q) ? string.Empty : string.Join(" OR ", from DataColumn c in _data.Columns
				select "[" + c.ColumnName + "] LIKE '%" + q.Replace("'", "''") + "%'"));
			int count = _data.DefaultView.Count;
			emptyState.Visibility = ((count != 0) ? Visibility.Collapsed : Visibility.Visible);
			footerCountLabel.Text = $"Showing {count:N0} record(s)";
		}
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			contextActionBar.Visibility = Visibility.Visible;
			selectedRecordLabel.Text = dataRowView["full_name"]?.ToString() ?? "Unknown User";
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

	private void BtnDelete_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select staff to deactivate.");
			return;
		}
		string text = dataRowView["full_name"]?.ToString() ?? "target user";
		if (DialogService.Instance.Confirm("Deactivate " + text + "?"))
		{
			DialogService.Instance.ShowInfo("Staff deactivated (WPF backend hook placeholder).");
		}
	}

	private void BtnResetPassword_Click(object sender, RoutedEventArgs e)
	{
		if (!(mainGrid.SelectedItem is DataRowView dataRowView))
		{
			DialogService.Instance.ShowWarning("Please select a user to reset their password.");
			return;
		}
		int targetUserId = ((dataRowView["id"] != DBNull.Value) ? Convert.ToInt32(dataRowView["id"]) : 0);
		string targetUsername = dataRowView["username"]?.ToString() ?? "Unknown";
		UpdateUserWindow window = new UpdateUserWindow(targetUserId, targetUsername);
		if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
		{
			LoadAsync();
		}
	}

	private void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		if (mainGrid.SelectedItem is DataRowView dataRowView)
		{
			StaffDetailsWindow window = new StaffDetailsWindow(new StaffProfileDetails
			{
				Username = (dataRowView["username"]?.ToString() ?? ""),
				FullName = (dataRowView["full_name"]?.ToString() ?? ""),
				RoleName = (dataRowView["role"]?.ToString() ?? "Standard"),
				IsActive = (!(dataRowView["is_active"] is bool flag) || flag)
			});
			if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
			{
				LoadAsync();
			}
		}
	}

	private void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		StaffDetailsWindow window = new StaffDetailsWindow();
		if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
		{
			LoadAsync();
		}
	}}
