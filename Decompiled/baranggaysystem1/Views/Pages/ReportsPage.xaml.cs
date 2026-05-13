using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels;
using Microsoft.Win32;

namespace baranggaysystem1.Views.Pages;

public partial class ReportsPage : UserControl
{
	private sealed record FilterOption(string Label, string Value);

	private sealed class StaffPerformanceDisplayRow
	{
		public string User { get; init; } = string.Empty;


		public int Completed { get; init; }

		public int Overdue { get; init; }

		public int Approvals { get; init; }

		public int Releases { get; init; }

		public int Resolutions { get; init; }

		public string ApprovalDuration { get; init; } = "-";


		public string ReleaseDuration { get; init; } = "-";


		public string ResolutionDuration { get; init; } = "-";

	}

	private sealed class HotspotDisplayRow
	{
		public string PurokName { get; init; } = string.Empty;


		public int IncidentCount { get; init; }

		public string Coordinates { get; init; } = string.Empty;

	}

	private readonly ResidentsModuleDataService _dataService = new ResidentsModuleDataService();

	private ReportsDashboardData? _currentData;

	private bool _hasLoaded;

	private bool _isInitializingFilters;

	private int _loadVersion;

	private DateTime _lastLoadedAt;








































	public ReportsPage()
	{
		InitializeComponent();
		ConfigureFilters();
		_isInitializingFilters = true;
		try
		{
			ResetDateFilters();
		}
		finally
		{
			_isInitializingFilters = false;
		}
		UpdateActionButtons(isBusy: false);
		base.Loaded += async delegate
		{
			await EnsureLoadedAsync();
		};
	}

	public ReportsPage(string route)
		: this()
	{
	}

	private async Task EnsureLoadedAsync()
	{
		if (!_hasLoaded)
		{
			_hasLoaded = true;
			await LoadPurokOptionsAsync();
			await LoadAsync();
		}
	}

	private void ConfigureFilters()
	{
		_isInitializingFilters = true;
		try
		{
			purokFilterCombo.DisplayMemberPath = "Name";
			purokFilterCombo.SelectedValuePath = "Id";
			purokFilterCombo.ItemsSource = new LookupItem[1]
			{
				new LookupItem(0, "All puroks")
			};
			purokFilterCombo.SelectedIndex = 0;
			certificateStatusCombo.DisplayMemberPath = "Label";
			certificateStatusCombo.SelectedValuePath = "Value";
			certificateStatusCombo.ItemsSource = new FilterOption[7]
			{
				new FilterOption("All certificate statuses", 0.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Pending", 1.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Submitted", 2.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Approved", 3.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Released", 4.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Cancelled", 5.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Rejected", 6.ToString(CultureInfo.InvariantCulture))
			};
			certificateStatusCombo.SelectedIndex = 0;
			blotterStatusCombo.DisplayMemberPath = "Label";
			blotterStatusCombo.SelectedValuePath = "Value";
			blotterStatusCombo.ItemsSource = new FilterOption[5]
			{
				new FilterOption("All blotter statuses", 0.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Active", 1.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Settled", 2.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Referred", 3.ToString(CultureInfo.InvariantCulture)),
				new FilterOption("Closed", 4.ToString(CultureInfo.InvariantCulture))
			};
			blotterStatusCombo.SelectedIndex = 0;
		}
		finally
		{
			_isInitializingFilters = false;
		}
	}

	private void ResetDateFilters()
	{
		DateTime today = DateTime.Today;
		DateTime value = new DateTime(today.AddMonths(-5).Year, today.AddMonths(-5).Month, 1);
		fromDatePicker.SelectedDate = value;
		toDatePicker.SelectedDate = today;
	}

	private async Task LoadPurokOptionsAsync()
	{
		int selectedPurokId = GetSelectedPurokId();
		try
		{
			IReadOnlyList<LookupItem> source = await _dataService.GetPurokOptionsAsync();
			List<LookupItem> list = new List<LookupItem>
			{
				new LookupItem(0, "All puroks")
			};
			list.AddRange((from option in source
				where option != null
				group option by option.Id into @group
				select @group.First()).OrderBy<LookupItem, string>((LookupItem option) => option.Name, StringComparer.OrdinalIgnoreCase));
			_isInitializingFilters = true;
			try
			{
				purokFilterCombo.ItemsSource = list;
				purokFilterCombo.SelectedValue = (list.Any((LookupItem option) => option.Id == selectedPurokId) ? selectedPurokId : 0);
			}
			finally
			{
				_isInitializingFilters = false;
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("ReportsPage failed to load purok filter options.", ex);
			_isInitializingFilters = true;
			try
			{
				purokFilterCombo.ItemsSource = new LookupItem[1]
				{
					new LookupItem(0, "All puroks")
				};
				purokFilterCombo.SelectedIndex = 0;
			}
			finally
			{
				_isInitializingFilters = false;
			}
		}
	}

	private async Task LoadAsync()
	{
		DateTime from = fromDatePicker.SelectedDate?.Date ?? DateTime.Today;
		DateTime to = toDatePicker.SelectedDate?.Date ?? DateTime.Today;
		if (from > to)
		{
			DateTime dateTime = to;
			DateTime dateTime2 = from;
			to = dateTime2;
			from = dateTime;
			_isInitializingFilters = true;
			try
			{
				fromDatePicker.SelectedDate = from;
				toDatePicker.SelectedDate = to;
			}
			finally
			{
				_isInitializingFilters = false;
			}
		}
		ReportsFilters filters = BuildFilters();
		int loadVersion = ++_loadVersion;
		SetBusyState($"Generating live report snapshot for {from:MMM dd, yyyy} to {to:MMM dd, yyyy}...");
		try
		{
			ReportsDashboardData reportsDashboardData = await Task.Run(() => ReportsService.LoadDashboard(from, to, filters));
			if (loadVersion == _loadVersion)
			{
				_currentData = reportsDashboardData;
				_lastLoadedAt = DateTime.Now;
				ApplyDashboard(reportsDashboardData, from, to, filters);
			}
		}
		catch (Exception ex)
		{
			if (loadVersion == _loadVersion)
			{
				_currentData = null;
				ResetDashboardValues();
				AppLogger.LogError("ReportsPage load failed.", ex);
				ApplyLoadFailure(ex, from, to, filters);
			}
		}
		finally
		{
			UpdateActionButtons(isBusy: false);
		}
	}

	private ReportsFilters BuildFilters()
	{
		int selectedPurokId = GetSelectedPurokId();
		return new ReportsFilters
		{
			PurokId = ((selectedPurokId > 0) ? new int?(selectedPurokId) : null),
			CertificateStatus = GetSelectedCertificateStatus(),
			BlotterStatus = GetSelectedBlotterStatus()
		};
	}

	private void SetBusyState(string message)
	{
		recordCountLabel.Text = message;
		lastRefreshChip.Text = "Refreshing snapshot...";
		filterSummaryLabel.Text = BuildFilterSummary();
		footerStatusLabel.Text = "Refreshing...";
		UpdateActionButtons(isBusy: true);
	}

	private void ApplyDashboard(ReportsDashboardData data, DateTime from, DateTime to, ReportsFilters filters)
	{
		IReadOnlyList<StaffPerformanceDisplayRow> readOnlyList = BuildStaffRows(data.StaffPerformance);
		IReadOnlyList<HotspotDisplayRow> readOnlyList2 = BuildHotspotRows(data.Hotspots);
		HotspotPoint hotspotPoint = data.Hotspots.OrderByDescending((HotspotPoint point) => point.IncidentCount).ThenBy<HotspotPoint, string>((HotspotPoint point) => point.PurokName, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
		newResidentsValue.Text = data.Summary.NewResidents.ToString("N0", CultureInfo.InvariantCulture);
		certificateRequestsValue.Text = data.Summary.CertificateRequests.ToString("N0", CultureInfo.InvariantCulture);
		certificatesReleasedValue.Text = data.Summary.CertificatesReleased.ToString("N0", CultureInfo.InvariantCulture);
		blottersFiledValue.Text = data.Summary.BlottersFiled.ToString("N0", CultureInfo.InvariantCulture);
		totalResidentsValue.Text = data.Summary.TotalResidents.ToString("N0", CultureInfo.InvariantCulture);
		pendingCertificatesValue.Text = data.Summary.PendingCertificates.ToString("N0", CultureInfo.InvariantCulture);
		activeBlottersValue.Text = data.Summary.ActiveBlotters.ToString("N0", CultureInfo.InvariantCulture);
		approvalCycleValue.Text = FormatDuration(data.ServiceTimes.AvgRequestToApprovalSeconds);
		approvalCycleMetaLabel.Text = $"{data.ServiceTimes.ApprovalSamples:N0} approval sample(s)";
		releaseCycleValue.Text = FormatDuration(data.ServiceTimes.AvgApprovalToReleaseSeconds);
		releaseCycleMetaLabel.Text = $"{data.ServiceTimes.ReleaseSamples:N0} release sample(s)";
		topHotspotValue.Text = ((hotspotPoint == null || hotspotPoint.IncidentCount <= 0) ? "No incidents" : $"{hotspotPoint.PurokName} ({hotspotPoint.IncidentCount:N0})");
		topHotspotMetaLabel.Text = ((hotspotPoint == null || hotspotPoint.IncidentCount <= 0) ? "No purok incident concentration detected in the current filter." : BuildHotspotMeta(hotspotPoint));
		rangeChip.Text = $"{from:MMM dd, yyyy} - {to:MMM dd, yyyy}";
		scopeChip.Text = BuildScopeChip(filters);
		lastRefreshChip.Text = $"Refreshed {_lastLoadedAt:hh:mm tt}";
		recordCountLabel.Text = $"Live resident, clearance, and blotter reporting from {from:MMM dd, yyyy} to {to:MMM dd, yyyy}.";
		filterSummaryLabel.Text = BuildFilterSummary();
		trendsMetaLabel.Text = $"{data.Trends.Count:N0} monthly checkpoint(s) in the selected reporting window.";
		snapshotMetaLabel.Text = "Current scope: " + BuildScopeDescription(filters);
		serviceTimesMetaLabel.Text = $"Cycle-time averages based on completed actions between {from:MMM dd} and {to:MMM dd}.";
		hotspotMetaLabel.Text = ((readOnlyList2.Count == 0) ? "No puroks recorded incident activity in this reporting window." : $"Top {readOnlyList2.Count:N0} purok hotspot row(s) with incident activity.");
		hotspotEmptyLabel.Visibility = ((readOnlyList2.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		staffMetaLabel.Text = ((readOnlyList.Count == 0) ? "No approval, release, or blotter resolution activity matched the current range." : $"Showing {readOnlyList.Count:N0} staff performance row(s) for the selected range.");
		staffCountLabel.Text = $"{readOnlyList.Count:N0} staff row(s)";
		staffEmptyLabel.Visibility = ((readOnlyList.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		trendsGrid.ItemsSource = data.Trends;
		hotspotGrid.ItemsSource = readOnlyList2;
		staffGrid.ItemsSource = readOnlyList;
		footerCountLabel.Text = $"{data.Trends.Count:N0} trend row(s) | {readOnlyList.Count:N0} staff row(s) | {readOnlyList2.Count:N0} hotspot row(s)";
		footerStatusLabel.Text = $"Last loaded {_lastLoadedAt:MMM dd, yyyy hh:mm tt}";
	}

	private void ResetDashboardValues()
	{
		newResidentsValue.Text = "0";
		certificateRequestsValue.Text = "0";
		certificatesReleasedValue.Text = "0";
		blottersFiledValue.Text = "0";
		totalResidentsValue.Text = "0";
		pendingCertificatesValue.Text = "0";
		activeBlottersValue.Text = "0";
		topHotspotValue.Text = "No incidents";
		topHotspotMetaLabel.Text = "No purok incident concentration detected in the current filter.";
		approvalCycleValue.Text = "-";
		approvalCycleMetaLabel.Text = "0 approval sample(s)";
		releaseCycleValue.Text = "-";
		releaseCycleMetaLabel.Text = "0 release sample(s)";
		trendsGrid.ItemsSource = Array.Empty<MonthlyTrendRow>();
		hotspotGrid.ItemsSource = Array.Empty<HotspotDisplayRow>();
		staffGrid.ItemsSource = Array.Empty<StaffPerformanceDisplayRow>();
		hotspotEmptyLabel.Visibility = Visibility.Visible;
		staffEmptyLabel.Visibility = Visibility.Visible;
		staffCountLabel.Text = "0 staff row(s)";
	}

	private void ApplyLoadFailure(Exception ex, DateTime from, DateTime to, ReportsFilters filters)
	{
		rangeChip.Text = $"{from:MMM dd, yyyy} - {to:MMM dd, yyyy}";
		scopeChip.Text = BuildScopeChip(filters);
		lastRefreshChip.Text = "Snapshot failed";
		recordCountLabel.Text = "Unable to generate the live report snapshot right now.";
		filterSummaryLabel.Text = "Report load failed: " + ex.Message;
		trendsMetaLabel.Text = "The reporting dataset could not be loaded.";
		snapshotMetaLabel.Text = "Retry the current filters or check database connectivity.";
		serviceTimesMetaLabel.Text = "Service-time metrics are unavailable until the report reload succeeds.";
		hotspotMetaLabel.Text = "Hotspot data is unavailable until the report reload succeeds.";
		staffMetaLabel.Text = "Staff performance data is unavailable until the report reload succeeds.";
		footerCountLabel.Text = "Report snapshot failed to load.";
		footerStatusLabel.Text = $"Last error at {DateTime.Now:hh:mm tt}";
	}

	private async void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isInitializingFilters && _hasLoaded)
		{
			await LoadAsync();
		}
	}

	private async void FilterDateChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isInitializingFilters && _hasLoaded && fromDatePicker.SelectedDate.HasValue && toDatePicker.SelectedDate.HasValue)
		{
			await LoadAsync();
		}
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadPurokOptionsAsync();
		await LoadAsync();
	}

	private async void BtnClearFilters_Click(object sender, RoutedEventArgs e)
	{
		_isInitializingFilters = true;
		try
		{
			ResetDateFilters();
			purokFilterCombo.SelectedIndex = 0;
			certificateStatusCombo.SelectedIndex = 0;
			blotterStatusCombo.SelectedIndex = 0;
		}
		finally
		{
			_isInitializingFilters = false;
		}
		if (_hasLoaded)
		{
			await LoadAsync();
		}
	}

	private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
	{
		await ExportAsync("Excel", ".xlsx", "Excel Workbook (*.xlsx)|*.xlsx|All files (*.*)|*.*", delegate(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
		{
			ReportsExportService.ExportDashboardExcel(data, from, to, filePath);
		});
	}

	private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
	{
		await ExportAsync("PDF", ".pdf", "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*", delegate(ReportsDashboardData data, DateTime from, DateTime to, string filePath)
		{
			ReportsExportService.ExportDashboardPdf(data, from, to, filePath);
		});
	}

	private async Task ExportAsync(string formatName, string defaultExtension, string filter, Action<ReportsDashboardData, DateTime, DateTime, string> exportAction)
	{
		Action<ReportsDashboardData, DateTime, DateTime, string> exportAction2 = exportAction;
		if (_currentData == null)
		{
			DialogService.Instance.ShowWarning("Generate a report snapshot first before exporting.");
			return;
		}
		DateTime from = fromDatePicker.SelectedDate?.Date ?? DateTime.Today;
		DateTime to = toDatePicker.SelectedDate?.Date ?? DateTime.Today;
		if (from > to)
		{
			DateTime dateTime = to;
			DateTime dateTime2 = from;
			to = dateTime2;
			from = dateTime;
		}
		SaveFileDialog dialog = new SaveFileDialog
		{
			FileName = $"barangay-reports-{DateTime.Now:yyyyMMdd-HHmmss}{defaultExtension}",
			Filter = filter,
			DefaultExt = defaultExtension
		};
		if (!dialog.ShowDialog().GetValueOrDefault())
		{
			return;
		}
		UpdateActionButtons(isBusy: true);
		footerStatusLabel.Text = "Exporting " + formatName + "...";
		try
		{
			ReportsDashboardData snapshot = _currentData;
			await Task.Run(delegate
			{
				exportAction2(snapshot, from, to, dialog.FileName);
			});
			footerStatusLabel.Text = $"{formatName} export completed at {DateTime.Now:hh:mm tt}";
			DialogService.Instance.ShowInfo("The filtered report snapshot was exported successfully as " + formatName + ".");
		}
		catch (Exception ex)
		{
			AppLogger.LogError("ReportsPage " + formatName + " export failed.", ex);
			footerStatusLabel.Text = formatName + " export failed";
			DialogService.Instance.ShowError("Failed to export the " + formatName + " report: " + ex.Message);
		}
		finally
		{
			UpdateActionButtons(isBusy: false);
		}
	}

	private void UpdateActionButtons(bool isBusy)
	{
		btnRefresh.IsEnabled = !isBusy;
		btnExportExcel.IsEnabled = !isBusy && _currentData != null;
		btnExportPdf.IsEnabled = !isBusy && _currentData != null;
		btnClearFilters.IsEnabled = !isBusy;
	}

	private int GetSelectedPurokId()
	{
		object selectedValue = purokFilterCombo.SelectedValue;
		if (selectedValue == null)
		{
			return 0;
		}
		if (!int.TryParse(selectedValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return 0;
		}
		return result;
	}

	private CertificateStatusFilter GetSelectedCertificateStatus()
	{
		return ParseEnumSelection(certificateStatusCombo.SelectedValue, CertificateStatusFilter.AllNonDraft);
	}

	private BlotterStatusFilter GetSelectedBlotterStatus()
	{
		return ParseEnumSelection(blotterStatusCombo.SelectedValue, BlotterStatusFilter.All);
	}

	private static TEnum ParseEnumSelection<TEnum>(object? selectedValue, TEnum fallback) where TEnum : struct, Enum
	{
		if (selectedValue != null && int.TryParse(selectedValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && Enum.IsDefined(typeof(TEnum), result))
		{
			return (TEnum)Enum.ToObject(typeof(TEnum), result);
		}
		return fallback;
	}

	private string BuildFilterSummary()
	{
		string selectedPurokName = GetSelectedPurokName();
		string selectedOptionLabel = GetSelectedOptionLabel(certificateStatusCombo, "All certificate statuses");
		string selectedOptionLabel2 = GetSelectedOptionLabel(blotterStatusCombo, "All blotter statuses");
		DateTime dateTime = fromDatePicker.SelectedDate?.Date ?? DateTime.Today;
		DateTime dateTime2 = toDatePicker.SelectedDate?.Date ?? DateTime.Today;
		if (dateTime > dateTime2)
		{
			DateTime dateTime3 = dateTime2;
			dateTime2 = dateTime;
			dateTime = dateTime3;
		}
		return $"Range: {dateTime:MMM dd, yyyy} to {dateTime2:MMM dd, yyyy} | Purok: {selectedPurokName} | Certificate: {selectedOptionLabel} | Blotter: {selectedOptionLabel2}.";
	}

	private string BuildScopeChip(ReportsFilters filters)
	{
		string obj = (filters.PurokId.HasValue ? GetSelectedPurokName() : "All puroks");
		string selectedOptionLabel = GetSelectedOptionLabel(blotterStatusCombo, "All blotter statuses");
		return obj + " | " + selectedOptionLabel;
	}

	private string BuildScopeDescription(ReportsFilters filters)
	{
		string value = (filters.PurokId.HasValue ? GetSelectedPurokName() : "all puroks");
		string value2 = GetSelectedOptionLabel(certificateStatusCombo, "All certificate statuses").ToLowerInvariant();
		string value3 = GetSelectedOptionLabel(blotterStatusCombo, "All blotter statuses").ToLowerInvariant();
		return $"{value}, {value2}, {value3}";
	}

	private string GetSelectedPurokName()
	{
		if (!(purokFilterCombo.SelectedItem is LookupItem lookupItem))
		{
			return "All puroks";
		}
		return lookupItem.Name;
	}

	private static string GetSelectedOptionLabel(ComboBox comboBox, string fallback)
	{
		if (!(comboBox.SelectedItem is FilterOption filterOption))
		{
			return fallback;
		}
		return filterOption.Label;
	}

	private static IReadOnlyList<StaffPerformanceDisplayRow> BuildStaffRows(IReadOnlyList<StaffPerformanceRow> rows)
	{
		return (from row in (rows ?? Array.Empty<StaffPerformanceRow>()).Where((StaffPerformanceRow row) => row.IsActive || HasAnyActivity(row)).Select(delegate(StaffPerformanceRow row)
			{
				int completed = row.ApprovalsCompleted + row.ReleasesCompleted + row.BlotterResolutions;
				int overdue = row.ApprovalsOverdue + row.ReleasesOverdue + row.BlotterResolutionsOverdue;
				return new StaffPerformanceDisplayRow
				{
					User = FormatUser(row),
					Completed = completed,
					Overdue = overdue,
					Approvals = row.ApprovalsCompleted,
					Releases = row.ReleasesCompleted,
					Resolutions = row.BlotterResolutions,
					ApprovalDuration = FormatDuration(row.AvgRequestToApprovalSeconds),
					ReleaseDuration = FormatDuration(row.AvgApprovalToReleaseSeconds),
					ResolutionDuration = FormatDuration(row.AvgBlotterResolutionSeconds)
				};
			})
			orderby row.Completed descending, row.Overdue
			select row).ThenBy<StaffPerformanceDisplayRow, string>((StaffPerformanceDisplayRow row) => row.User, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static IReadOnlyList<HotspotDisplayRow> BuildHotspotRows(IReadOnlyList<HotspotPoint> hotspots)
	{
		return (from point in (hotspots ?? Array.Empty<HotspotPoint>()).Where((HotspotPoint point) => point.IncidentCount > 0).Take(8)
			select new HotspotDisplayRow
			{
				PurokName = point.PurokName,
				IncidentCount = point.IncidentCount,
				Coordinates = ((point.Latitude.HasValue && point.Longitude.HasValue) ? $"{point.Latitude.Value:0.0000}, {point.Longitude.Value:0.0000}" : "Not mapped")
			}).ToList();
	}

	private static bool HasAnyActivity(StaffPerformanceRow row)
	{
		if (row.ApprovalsCompleted <= 0 && row.ReleasesCompleted <= 0 && row.BlotterStatusChanges <= 0)
		{
			return row.BlotterResolutions > 0;
		}
		return true;
	}

	private static string FormatUser(StaffPerformanceRow row)
	{
		string text = (string.IsNullOrWhiteSpace(row.DisplayName) ? row.Username : row.DisplayName);
		if (!string.IsNullOrWhiteSpace(row.Username) && !string.Equals(text, row.Username, StringComparison.OrdinalIgnoreCase))
		{
			text = row.Username + " (" + text + ")";
		}
		if (!row.IsActive)
		{
			text += " [inactive]";
		}
		return text;
	}

	private static string FormatDuration(double seconds)
	{
		if (seconds <= 0.0)
		{
			return "-";
		}
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
		if (timeSpan.TotalMinutes < 1.0)
		{
			return "<1m";
		}
		if (!(timeSpan.TotalHours < 1.0))
		{
			if (!(timeSpan.TotalDays < 1.0))
			{
				int num = (int)Math.Floor(timeSpan.TotalDays);
				if (num < 10 && timeSpan.Hours > 0)
				{
					return $"{num}d {timeSpan.Hours}h";
				}
				return $"{timeSpan.TotalDays:0.#}d";
			}
			return $"{timeSpan.TotalHours:0.#}h";
		}
		return $"{timeSpan.TotalMinutes:0}m";
	}

	private static string BuildHotspotMeta(HotspotPoint hotspot)
	{
		if (hotspot.Latitude.HasValue && hotspot.Longitude.HasValue)
		{
			return $"{hotspot.IncidentCount:N0} incident(s) mapped at {hotspot.Latitude.Value:0.0000}, {hotspot.Longitude.Value:0.0000}.";
		}
		return $"{hotspot.IncidentCount:N0} incident(s) recorded, but this purok is not yet mapped.";
	}}
