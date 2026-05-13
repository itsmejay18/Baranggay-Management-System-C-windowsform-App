using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using Microsoft.Win32;

namespace baranggaysystem1.Views.Pages;

public partial class SystemLogsPage : UserControl
{
	private sealed record FilterOption(string Label, string Value);

	private List<SystemLogEntry> _allEntries = new List<SystemLogEntry>();

	private ICollectionView? _entriesView;

	private SystemLogSnapshot? _snapshot;

	private bool _isInitializingFilters;


































	public SystemLogsPage()
	{
		InitializeComponent();
		ConfigureFilterCombos();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	public SystemLogsPage(string route)
		: this()
	{
	}

	private void ConfigureFilterCombos()
	{
		_isInitializingFilters = true;
		sourceFilterCombo.DisplayMemberPath = "Label";
		sourceFilterCombo.SelectedValuePath = "Value";
		sourceFilterCombo.ItemsSource = new FilterOption[3]
		{
			new FilterOption("All Sources", string.Empty),
			new FilterOption("Audit Trail", "audit"),
			new FilterOption("Application Log", "application")
		};
		sourceFilterCombo.SelectedIndex = 0;
		windowFilterCombo.DisplayMemberPath = "Label";
		windowFilterCombo.SelectedValuePath = "Value";
		windowFilterCombo.ItemsSource = new FilterOption[5]
		{
			new FilterOption("All Dates", "all"),
			new FilterOption("Today", "1"),
			new FilterOption("Last 7 Days", "7"),
			new FilterOption("Last 30 Days", "30"),
			new FilterOption("Last 90 Days", "90")
		};
		windowFilterCombo.SelectedIndex = 0;
		moduleFilterCombo.DisplayMemberPath = "Label";
		moduleFilterCombo.SelectedValuePath = "Value";
		moduleFilterCombo.ItemsSource = new FilterOption[1]
		{
			new FilterOption("All Modules", string.Empty)
		};
		moduleFilterCombo.SelectedIndex = 0;
		_isInitializingFilters = false;
	}

	private async Task LoadAsync()
	{
		string selectedSource = GetSelectedValue(sourceFilterCombo);
		string selectedModule = GetSelectedValue(moduleFilterCombo);
		string selectedWindow = GetSelectedValue(windowFilterCombo);
		try
		{
			recordCountLabel.Text = "Scanning audit trail and application log feeds...";
			_snapshot = await Task.Run(() => SystemLogsService.LoadSnapshot());
			_allEntries = _snapshot.Entries.ToList();
			RefreshModuleOptions(selectedModule);
			mainGrid.ItemsSource = _allEntries;
			_entriesView = CollectionViewSource.GetDefaultView(mainGrid.ItemsSource);
			_entriesView.Filter = FilterEntry;
			((Collection<SortDescription>)(object)_entriesView.SortDescriptions).Clear();
			((Collection<SortDescription>)(object)_entriesView.SortDescriptions).Add(new SortDescription("Timestamp", ListSortDirection.Descending));
			SetSelectedValue(sourceFilterCombo, selectedSource);
			SetSelectedValue(windowFilterCombo, selectedWindow);
			UpdateSnapshotSummary();
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("SystemLogsPage load failed.", ex);
			emptyLabel.Text = "Unable to load system logs right now.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Logs failed to load.";
			recordCountLabel.Text = "Unable to read audit or application logs.";
			DialogService.Instance.ShowError("Failed to load system logs: " + ex.Message);
		}
	}

	private void RefreshModuleOptions(string preferredModule)
	{
		_isInitializingFilters = true;
		try
		{
			string[] source = (from value in (from entry in _allEntries
					select entry.ModuleDisplay into value
					where !string.IsNullOrWhiteSpace(value)
					select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby value
				select value).ToArray();
			List<FilterOption> list = new List<FilterOption>
			{
				new FilterOption("All Modules", string.Empty)
			};
			list.AddRange(source.Select((string module) => new FilterOption(module, module)));
			moduleFilterCombo.ItemsSource = list;
			SetSelectedValue(moduleFilterCombo, preferredModule);
		}
		finally
		{
			_isInitializingFilters = false;
		}
	}

	private bool FilterEntry(object item)
	{
		if (!(item is SystemLogEntry systemLogEntry))
		{
			return false;
		}
		string value = searchBox.Text.Trim();
		if (!string.IsNullOrWhiteSpace(value) && systemLogEntry.SearchIndex.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}
		string selectedValue = GetSelectedValue(sourceFilterCombo);
		if (string.Equals(selectedValue, "audit", StringComparison.OrdinalIgnoreCase) && systemLogEntry.Source != 0)
		{
			return false;
		}
		if (string.Equals(selectedValue, "application", StringComparison.OrdinalIgnoreCase) && systemLogEntry.Source != SystemLogSource.ApplicationLog)
		{
			return false;
		}
		string selectedValue2 = GetSelectedValue(moduleFilterCombo);
		if (!string.IsNullOrWhiteSpace(selectedValue2) && !string.Equals(systemLogEntry.ModuleDisplay, selectedValue2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (int.TryParse(GetSelectedValue(windowFilterCombo), out var result))
		{
			if (systemLogEntry.Timestamp == DateTime.MinValue)
			{
				return false;
			}
			DateTime dateTime = DateTime.Today.AddDays(-(result - 1));
			if (systemLogEntry.Timestamp < dateTime)
			{
				return false;
			}
		}
		return true;
	}

	private void ApplyFilters()
	{
		ICollectionView? entriesView = _entriesView;
		if (entriesView != null)
		{
			entriesView.Refresh();
		}
		UpdateFilteredSummary();
		if (mainGrid.Items.Count > 0)
		{
			if (!(mainGrid.SelectedItem is SystemLogEntry))
			{
				mainGrid.SelectedIndex = 0;
			}
		}
		else
		{
			mainGrid.SelectedItem = null;
			UpdateSelectedEntry(null);
		}
	}

	private void UpdateSnapshotSummary()
	{
		if (_snapshot != null)
		{
			auditCountValue.Text = _snapshot.AuditCount.ToString("N0");
			applicationCountValue.Text = _snapshot.ApplicationCount.ToString("N0");
			errorCountValue.Text = _snapshot.ErrorCount.ToString("N0");
			actorCountValue.Text = _snapshot.ActiveUsers.ToString("N0");
			moduleCoverageChip.Text = $"{_snapshot.ModuleCount:N0} modules covered";
			lastRefreshChip.Text = $"Scanned {_snapshot.LoadedAt:hh:mm tt}";
			recordCountLabel.Text = $"{_snapshot.Entries.Count:N0} combined entries loaded from audit history and application runtime logs.";
			footerPathLabel.Text = "Log folder: " + SystemLogsService.GetApplicationLogDirectory();
		}
	}

	private void UpdateFilteredSummary()
	{
		int count = mainGrid.Items.Count;
		int value2 = (from SystemLogEntry entry in mainGrid.Items
			select entry.ModuleDisplay into value
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct<string>(StringComparer.OrdinalIgnoreCase).Count();
		visibleNowChip.Text = $"{count:N0} entries visible";
		tableVisibleLabel.Text = $"{count:N0} visible";
		tableMetaLabel.Text = ((count == 0) ? "No entries matched the current view." : $"{value2:N0} module(s) represented in the current feed.");
		footerCountLabel.Text = ((count == 0) ? "No log entries matched the current filters." : $"Showing {count:N0} system log entr{((count == 1) ? "y" : "ies")}.");
		emptyState.Visibility = ((count != 0) ? Visibility.Collapsed : Visibility.Visible);
		string value3 = (string.IsNullOrWhiteSpace(searchBox.Text.Trim()) ? "all entries" : ("search '" + searchBox.Text.Trim() + "'"));
		string value4 = GetSelectedLabel(sourceFilterCombo).ToLowerInvariant();
		string value5 = (string.Equals(GetSelectedValue(moduleFilterCombo), string.Empty, StringComparison.Ordinal) ? "all modules" : GetSelectedLabel(moduleFilterCombo));
		string value6 = GetSelectedLabel(windowFilterCombo).ToLowerInvariant();
		filterSummaryTitle.Text = ((count == 0) ? "No logs matched the current filters." : $"Showing {count:N0} log entr{((count == 1) ? "y" : "ies")} in the feed.");
		filterSummaryBody.Text = $"Filters: {value3} | {value4} | {value5} | {value6}.";
	}

	private void UpdateSelectedEntry(SystemLogEntry? entry)
	{
		if (entry == null)
		{
			selectedSummaryLabel.Text = "Select a log entry to inspect its details.";
			selectedSourceChip.Text = "Source: -";
			selectedTypeChip.Text = "Type: -";
			selectedModuleChip.Text = "Module: -";
			selectedTimestampLabel.Text = "-";
			selectedActorLabel.Text = "-";
			selectedEntityLabel.Text = "-";
			selectedOriginLabel.Text = "-";
			selectedDetailsBox.Text = "No log entry selected.";
			selectedBeforeBox.Text = string.Empty;
			selectedAfterBox.Text = string.Empty;
			beforeStateLabel.Visibility = Visibility.Collapsed;
			selectedBeforeBox.Visibility = Visibility.Collapsed;
			afterStateLabel.Visibility = Visibility.Collapsed;
			selectedAfterBox.Visibility = Visibility.Collapsed;
		}
		else
		{
			selectedSummaryLabel.Text = entry.Summary;
			selectedSourceChip.Text = "Source: " + entry.SourceDisplay;
			selectedTypeChip.Text = "Type: " + entry.CategoryDisplay;
			selectedModuleChip.Text = "Module: " + entry.ModuleDisplay;
			selectedTimestampLabel.Text = entry.TimestampDisplay;
			selectedActorLabel.Text = entry.ActorDisplay;
			selectedEntityLabel.Text = entry.EntityDisplay;
			selectedOriginLabel.Text = ((entry.Source == SystemLogSource.AuditTrail) ? "Database audit_trail" : entry.FileName);
			selectedDetailsBox.Text = entry.FullDetailText;
			bool flag = !string.IsNullOrWhiteSpace(entry.BeforeJson);
			bool flag2 = !string.IsNullOrWhiteSpace(entry.AfterJson);
			beforeStateLabel.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			selectedBeforeBox.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			selectedBeforeBox.Text = (flag ? entry.BeforeJson : string.Empty);
			afterStateLabel.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
			selectedAfterBox.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
			selectedAfterBox.Text = (flag2 ? entry.AfterJson : string.Empty);
		}
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isInitializingFilters)
		{
			ApplyFilters();
		}
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateSelectedEntry(mainGrid.SelectedItem as SystemLogEntry);
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
	{
		_isInitializingFilters = true;
		searchBox.Text = string.Empty;
		sourceFilterCombo.SelectedIndex = 0;
		moduleFilterCombo.SelectedIndex = 0;
		windowFilterCombo.SelectedIndex = 0;
		_isInitializingFilters = false;
		ApplyFilters();
	}

	private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
	{
		string applicationLogDirectory = SystemLogsService.GetApplicationLogDirectory();
		try
		{
			Directory.CreateDirectory(applicationLogDirectory);
			Process.Start(new ProcessStartInfo
			{
				FileName = applicationLogDirectory,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Unable to open log folder.", ex);
			DialogService.Instance.ShowError("Unable to open the log folder: " + ex.Message);
		}
	}

	private void BtnExport_Click(object sender, RoutedEventArgs e)
	{
		List<SystemLogEntry> list = mainGrid.Items.Cast<SystemLogEntry>().ToList();
		if (list.Count == 0)
		{
			DialogService.Instance.ShowWarning("There are no visible log entries to export.");
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			FileName = $"system-logs-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
			Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
			DefaultExt = ".csv"
		};
		if (!saveFileDialog.ShowDialog().GetValueOrDefault())
		{
			return;
		}
		try
		{
			using StreamWriter streamWriter = new StreamWriter(saveFileDialog.FileName, append: false, Encoding.UTF8);
			streamWriter.WriteLine("timestamp,source,type,module,actor,summary,record,details");
			foreach (SystemLogEntry item in list)
			{
				streamWriter.WriteLine(string.Join(",", ToCsv(item.TimestampDisplay), ToCsv(item.SourceDisplay), ToCsv(item.CategoryDisplay), ToCsv(item.ModuleDisplay), ToCsv(item.ActorDisplay), ToCsv(item.Summary), ToCsv(item.EntityDisplay), ToCsv(item.FullDetailText)));
			}
			DialogService.Instance.ShowInfo($"Exported {list.Count:N0} log entr{((list.Count == 1) ? "y" : "ies")}.");
		}
		catch (Exception ex)
		{
			AppLogger.LogError("System logs export failed.", ex);
			DialogService.Instance.ShowError("Failed to export logs: " + ex.Message);
		}
	}

	private static string GetSelectedValue(ComboBox comboBox)
	{
		return comboBox.SelectedValue?.ToString() ?? string.Empty;
	}

	private static string GetSelectedLabel(ComboBox comboBox)
	{
		if (!(comboBox.SelectedItem is FilterOption filterOption))
		{
			return string.Empty;
		}
		return filterOption.Label;
	}

	private static void SetSelectedValue(ComboBox comboBox, string value)
	{
		string value2 = value;
		if (comboBox.ItemsSource is IEnumerable<FilterOption> source)
		{
			FilterOption filterOption = source.FirstOrDefault((FilterOption item) => string.Equals(item.Value, value2 ?? string.Empty, StringComparison.OrdinalIgnoreCase));
			comboBox.SelectedItem = filterOption ?? source.FirstOrDefault();
		}
	}

	private static string ToCsv(string? value)
	{
		string text = value ?? string.Empty;
		return "\"" + text.Replace("\"", "\"\"") + "\"";
	}}
