using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class ResidentBlotterHistoryWindow : Window
{
	private readonly int _residentId;

	private readonly string _residentName;

	private readonly BlotterRepository _repository = new BlotterRepository();

	private DataTable? _allCasesTable;











	public ResidentBlotterHistoryWindow(int residentId, string residentName)
	{
		InitializeComponent();
		_residentId = residentId;
		_residentName = residentName;
		residentNameLabel.Text = residentName;
		base.Title = "Blotter Cases — " + residentName;
		base.Loaded += OnLoadedAsync;
	}

	private async void OnLoadedAsync(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoadedAsync;
		try
		{
			_allCasesTable = await _repository.LoadCasesForResidentAsync(_residentId, CancellationToken.None);
			loadingState.Visibility = Visibility.Collapsed;
			if (_allCasesTable.Rows.Count == 0)
			{
				emptyState.Visibility = Visibility.Visible;
				casesGrid.Visibility = Visibility.Collapsed;
				UpdateSummary(_allCasesTable.DefaultView);
			}
			else
			{
				casesGrid.ItemsSource = _allCasesTable.DefaultView;
				ApplyFilter();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to load blotter case history.", ex);
			loadingState.Visibility = Visibility.Collapsed;
			emptyState.Visibility = Visibility.Visible;
			footerLabel.Text = "Failed to load blotter cases.";
			DialogService.Instance.ShowError(ex.Message, "Blotter History");
		}
	}

	private void UpdateSummary(DataView view)
	{
		int count = view.Count;
		int num = view.Cast<DataRowView>().Count(delegate(DataRowView r)
		{
			string text3 = r["status"]?.ToString()?.Trim().ToUpperInvariant() ?? "";
			return text3 == "OPEN" || text3 == "ONGOING";
		});
		int num2 = view.Cast<DataRowView>().Count((DataRowView r) => string.Equals(r["status"]?.ToString()?.Trim(), "SETTLED", StringComparison.OrdinalIgnoreCase));
		int num3 = view.Cast<DataRowView>().Count(delegate(DataRowView r)
		{
			string text2 = r["involvement"]?.ToString()?.Trim() ?? "";
			return text2 == "Complainant" || text2 == "Both";
		});
		int num4 = view.Cast<DataRowView>().Count(delegate(DataRowView r)
		{
			string text = r["involvement"]?.ToString()?.Trim() ?? "";
			return text == "Respondent" || text == "Both";
		});
		totalCasesValue.Text = count.ToString("N0");
		activeCasesValue.Text = num.ToString("N0");
		settledCasesValue.Text = num2.ToString("N0");
		complainantCountValue.Text = num3.ToString("N0");
		respondentCountValue.Text = num4.ToString("N0");
		footerLabel.Text = ((count == 0) ? "No blotter cases found matching the current filter." : $"Showing {count:N0} blotter case(s) for {_residentName}.");
	}

	private void TimeFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyFilter();
	}

	private void ApplyFilter()
	{
		if (_allCasesTable != null && casesGrid != null)
		{
			string rowFilter = string.Empty;
			int selectedIndex = timeFilterCombo.SelectedIndex;
			DateTime now = DateTime.Now;
			DateTime minValue = DateTime.MinValue;
			switch (selectedIndex)
			{
			case 1:
			{
				int num = (int)(7 + (now.DayOfWeek - 1)) % 7;
				minValue = now.AddDays(-1 * num).Date;
				rowFilter = $"incident_date >= '{minValue:yyyy-MM-dd}'";
				break;
			}
			case 2:
				minValue = new DateTime(now.Year, now.Month, 1);
				rowFilter = $"incident_date >= '{minValue:yyyy-MM-dd}'";
				break;
			case 3:
				minValue = new DateTime(now.Year, 1, 1);
				rowFilter = $"incident_date >= '{minValue:yyyy-MM-dd}'";
				break;
			}
			_allCasesTable.DefaultView.RowFilter = rowFilter;
			if (_allCasesTable.DefaultView.Count == 0)
			{
				emptyState.Visibility = Visibility.Visible;
				casesGrid.Visibility = Visibility.Collapsed;
			}
			else
			{
				emptyState.Visibility = Visibility.Collapsed;
				casesGrid.Visibility = Visibility.Visible;
			}
			UpdateSummary(_allCasesTable.DefaultView);
		}
	}

	private void BtnClose_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}}
