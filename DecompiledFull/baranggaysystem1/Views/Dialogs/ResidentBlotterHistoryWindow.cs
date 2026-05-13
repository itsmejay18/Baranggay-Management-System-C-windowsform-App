using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Dialogs;

public class ResidentBlotterHistoryWindow : Window, IComponentConnector
{
	private readonly int _residentId;

	private readonly string _residentName;

	private readonly BlotterRepository _repository = new BlotterRepository();

	private DataTable? _allCasesTable;

	internal TextBlock residentNameLabel;

	internal TextBlock totalCasesValue;

	internal TextBlock activeCasesValue;

	internal TextBlock settledCasesValue;

	internal TextBlock complainantCountValue;

	internal TextBlock respondentCountValue;

	internal ComboBox timeFilterCombo;

	internal DataGrid casesGrid;

	internal StackPanel emptyState;

	internal StackPanel loadingState;

	internal TextBlock footerLabel;

	private bool _contentLoaded;

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
			string text = r["status"]?.ToString()?.Trim().ToUpperInvariant() ?? "";
			return text == "OPEN" || text == "ONGOING";
		});
		int num2 = view.Cast<DataRowView>().Count((DataRowView r) => string.Equals(r["status"]?.ToString()?.Trim(), "SETTLED", StringComparison.OrdinalIgnoreCase));
		int num3 = view.Cast<DataRowView>().Count(delegate(DataRowView r)
		{
			string text = r["involvement"]?.ToString()?.Trim() ?? "";
			return text == "Complainant" || text == "Both";
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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/residentblotterhistorywindow.xaml", UriKind.Relative);
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
			residentNameLabel = (TextBlock)target;
			break;
		case 2:
			((Button)target).Click += BtnClose_Click;
			break;
		case 3:
			totalCasesValue = (TextBlock)target;
			break;
		case 4:
			activeCasesValue = (TextBlock)target;
			break;
		case 5:
			settledCasesValue = (TextBlock)target;
			break;
		case 6:
			complainantCountValue = (TextBlock)target;
			break;
		case 7:
			respondentCountValue = (TextBlock)target;
			break;
		case 8:
			timeFilterCombo = (ComboBox)target;
			timeFilterCombo.SelectionChanged += TimeFilterCombo_SelectionChanged;
			break;
		case 9:
			casesGrid = (DataGrid)target;
			break;
		case 10:
			emptyState = (StackPanel)target;
			break;
		case 11:
			loadingState = (StackPanel)target;
			break;
		case 12:
			footerLabel = (TextBlock)target;
			break;
		case 13:
			((Button)target).Click += BtnClose_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
