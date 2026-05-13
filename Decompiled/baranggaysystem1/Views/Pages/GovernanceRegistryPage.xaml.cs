using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;

namespace baranggaysystem1.Views.Pages;

public partial class GovernanceRegistryPage : UserControl
{
	private enum GovernanceSection
	{
		Announcements,
		Projects
	}

	private readonly AnnouncementService _announcementService = new AnnouncementService();

	private readonly ProjectService _projectService = new ProjectService();

	private readonly bool _canManageAnnouncements = Permissions.IsAdmin || Permissions.CanManageAnnouncements;

	private readonly bool _canManageProjects = Permissions.IsAdmin || Permissions.CanManageProjects;

	private DataTable? _announcementData;

	private DataTable? _projectData;

	private GovernanceSection _activeSection;

	private bool _isUpdatingFilters;




























	public GovernanceRegistryPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	private async Task LoadAsync()
	{
		ApplyAccessState();
		if (!_canManageAnnouncements && !_canManageProjects)
		{
			ApplyUnavailableState("Announcements and projects are restricted for your role.", "Ask an administrator to grant the matching announcement or project permission.");
			return;
		}
		try
		{
			Task<DataTable> announcementTask = (_canManageAnnouncements ? _announcementService.GetAnnouncementRegistryAsync() : null);
			Task<DataTable> projectTask = (_canManageProjects ? _projectService.GetProjectRegistryAsync() : null);
			List<Task> list = new List<Task>();
			if (announcementTask != null)
			{
				list.Add(announcementTask);
			}
			if (projectTask != null)
			{
				list.Add(projectTask);
			}
			await Task.WhenAll(list);
			_announcementData = announcementTask?.Result;
			_projectData = projectTask?.Result;
			if (_announcementData != null)
			{
				EnrichAnnouncementTable(_announcementData);
				announcementGrid.ItemsSource = _announcementData.DefaultView;
			}
			else
			{
				announcementGrid.ItemsSource = null;
			}
			if (_projectData != null)
			{
				EnrichProjectTable(_projectData);
				projectGrid.ItemsSource = _projectData.DefaultView;
			}
			else
			{
				projectGrid.ItemsSource = null;
			}
			UpdateMetrics();
			UpdateSectionChrome();
			PopulateFilterOptions();
			ApplyFilters();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("GovernanceRegistryPage load failed.", ex);
			ApplyUnavailableState("The registry could not load right now.", "Refresh after checking database connectivity.");
		}
	}

	private void ApplyAccessState()
	{
		btnAddRecord.IsEnabled = true;
		governanceTabs.IsEnabled = true;
		announcementTab.Visibility = ((!_canManageAnnouncements) ? Visibility.Collapsed : Visibility.Visible);
		projectTab.Visibility = ((!_canManageProjects) ? Visibility.Collapsed : Visibility.Visible);
		if (_activeSection == GovernanceSection.Projects && _canManageProjects)
		{
			governanceTabs.SelectedItem = projectTab;
		}
		else if (_canManageAnnouncements)
		{
			_activeSection = GovernanceSection.Announcements;
			governanceTabs.SelectedItem = announcementTab;
		}
		else if (_canManageProjects)
		{
			_activeSection = GovernanceSection.Projects;
			governanceTabs.SelectedItem = projectTab;
		}
	}

	private void ApplyUnavailableState(string headline, string guidance)
	{
		governanceTabs.IsEnabled = false;
		announcementGrid.ItemsSource = null;
		projectGrid.ItemsSource = null;
		totalAnnouncementsMetric.Text = "0";
		publishedAnnouncementsMetric.Text = "0";
		pinnedAnnouncementsMetric.Text = "0";
		totalInitiativesMetric.Text = "0";
		completedInitiativesMetric.Text = "0";
		recordCountLabel.Text = headline;
		footerCountLabel.Text = headline;
		footerHintLabel.Text = guidance;
		announcementEmptyLabel.Text = headline;
		projectEmptyLabel.Text = headline;
		announcementEmptyState.Visibility = ((!_canManageAnnouncements) ? Visibility.Collapsed : Visibility.Visible);
		projectEmptyState.Visibility = ((!_canManageProjects) ? Visibility.Collapsed : Visibility.Visible);
		btnAddRecord.IsEnabled = false;
		contextActionBar.Visibility = Visibility.Collapsed;
	}

	private void UpdateMetrics()
	{
		totalAnnouncementsMetric.Text = (_announcementData?.Rows.Count ?? 0).ToString("N0", CultureInfo.InvariantCulture);
		publishedAnnouncementsMetric.Text = CountRows(_announcementData, (DataRow row) => string.Equals(ReadString(row, "status"), "Published", StringComparison.OrdinalIgnoreCase)).ToString("N0", CultureInfo.InvariantCulture);
		pinnedAnnouncementsMetric.Text = CountRows(_announcementData, (DataRow row) => ReadInt(row, "is_pinned") != 0).ToString("N0", CultureInfo.InvariantCulture);
		totalInitiativesMetric.Text = (_projectData?.Rows.Count ?? 0).ToString("N0", CultureInfo.InvariantCulture);
		completedInitiativesMetric.Text = CountRows(_projectData, (DataRow row) => string.Equals(ReadString(row, "status"), "Completed", StringComparison.OrdinalIgnoreCase)).ToString("N0", CultureInfo.InvariantCulture);
	}

	private void UpdateSectionChrome()
	{
		if (_activeSection == GovernanceSection.Projects)
		{
			sectionBadgeText.Text = "Projects & Programs";
			btnAddRecord.Content = "Add Initiative";
			btnEditSelected.Content = "Edit Record";
			footerHintLabel.Text = "Track schedules, ownership, budgets, attendance, and outcomes for community initiatives.";
		}
		else
		{
			sectionBadgeText.Text = "Announcement Registry";
			btnAddRecord.Content = "Create Announcement";
			btnEditSelected.Content = "Edit Announcement";
			footerHintLabel.Text = "Use this workspace for full announcement maintenance beyond the dashboard snapshot.";
		}
	}

	private void PopulateFilterOptions()
	{
		_isUpdatingFilters = true;
		try
		{
			categoryFilter.Items.Clear();
			categoryFilter.Items.Add(GetAllCategoryLabel());
			statusFilter.Items.Clear();
			statusFilter.Items.Add(GetAllStatusLabel());
			DataTable activeData = GetActiveData();
			if (activeData != null)
			{
				foreach (string item in from value in (from row in activeData.AsEnumerable()
						select ReadString(row, GetCategoryColumn()) into value
						where !string.IsNullOrWhiteSpace(value)
						select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
					orderby value
					select value)
				{
					categoryFilter.Items.Add(item);
				}
				foreach (string item2 in from value in (from row in activeData.AsEnumerable()
						select ReadString(row, GetStatusColumn()) into value
						where !string.IsNullOrWhiteSpace(value)
						select value).Distinct<string>(StringComparer.OrdinalIgnoreCase)
					orderby value
					select value)
				{
					statusFilter.Items.Add(item2);
				}
			}
			categoryFilter.SelectedIndex = 0;
			statusFilter.SelectedIndex = 0;
		}
		finally
		{
			_isUpdatingFilters = false;
		}
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && !_isUpdatingFilters)
		{
			ApplyFilters();
		}
	}

	private void GovernanceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && e.Source == governanceTabs)
		{
			_activeSection = ((governanceTabs.SelectedItem == projectTab) ? GovernanceSection.Projects : GovernanceSection.Announcements);
			UpdateSectionChrome();
			PopulateFilterOptions();
			ApplyFilters();
		}
	}

	private void ApplyFilters()
	{
		DataTable activeData = GetActiveData();
		if (activeData == null)
		{
			return;
		}
		List<string> list = new List<string>();
		string value = searchBox.Text.Trim();
		if (!string.IsNullOrWhiteSpace(value))
		{
			string escaped = EscapeForRowFilter(value);
			string text = string.Join(" OR ", from column in GetSearchColumns()
				select $"Convert([{column}], 'System.String') LIKE '%{escaped}%'");
			list.Add("(" + text + ")");
		}
		if (categoryFilter.SelectedItem is string text2 && !string.Equals(text2, GetAllCategoryLabel(), StringComparison.OrdinalIgnoreCase))
		{
			list.Add($"[{GetCategoryColumn()}] = '{EscapeForRowFilter(text2)}'");
		}
		if (statusFilter.SelectedItem is string text3 && !string.Equals(text3, GetAllStatusLabel(), StringComparison.OrdinalIgnoreCase))
		{
			list.Add($"[{GetStatusColumn()}] = '{EscapeForRowFilter(text3)}'");
		}
		activeData.DefaultView.RowFilter = string.Join(" AND ", list);
		UpdateEmptyStates();
		DataGrid activeGrid = GetActiveGrid();
		DataRowView selectedRow = activeGrid.SelectedItem as DataRowView;
		if (selectedRow == null || !activeData.DefaultView.Cast<DataRowView>().Any((DataRowView view) => view.Row == selectedRow.Row))
		{
			activeGrid.UnselectAll();
			UpdateSelectionState(null);
		}
		else
		{
			UpdateSelectionState(selectedRow);
		}
		int count = activeData.DefaultView.Count;
		int count2 = activeData.Rows.Count;
		recordCountLabel.Text = ((_activeSection != GovernanceSection.Projects) ? ((count == count2) ? $"{count:N0} announcement record(s) loaded." : $"{count:N0} of {count2:N0} announcement record(s) match the current filters.") : ((count == count2) ? $"{count:N0} project/program record(s) loaded." : $"{count:N0} of {count2:N0} project/program record(s) match the current filters."));
		footerCountLabel.Text = ((_activeSection == GovernanceSection.Projects) ? $"Showing {count:N0} project/program record(s)" : $"Showing {count:N0} announcement record(s)");
	}

	private void UpdateEmptyStates()
	{
		UpdateEmptyState(_announcementData, announcementEmptyState, announcementEmptyLabel, "No announcements found.", "No announcements match the current filters.");
		UpdateEmptyState(_projectData, projectEmptyState, projectEmptyLabel, "No project or program records found.", "No project or program records match the current filters.");
	}

	private static void UpdateEmptyState(DataTable? table, UIElement stateElement, TextBlock label, string noDataMessage, string noMatchMessage)
	{
		if (table == null)
		{
			stateElement.Visibility = Visibility.Collapsed;
			label.Text = noDataMessage;
		}
		else if (table.Rows.Count == 0)
		{
			stateElement.Visibility = Visibility.Visible;
			label.Text = noDataMessage;
		}
		else if (table.DefaultView.Count == 0)
		{
			stateElement.Visibility = Visibility.Visible;
			label.Text = noMatchMessage;
		}
		else
		{
			stateElement.Visibility = Visibility.Collapsed;
			label.Text = noDataMessage;
		}
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync();
	}

	private async void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		if (_activeSection == GovernanceSection.Projects)
		{
			ProjectWindow window = new ProjectWindow();
			if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
			{
				DialogService.Instance.ShowInfo("Project or program record saved successfully.");
				await LoadAsync();
			}
		}
		else
		{
			AnnouncementWindow window2 = new AnnouncementWindow();
			if (DialogService.Instance.ShowDialog(window2).GetValueOrDefault())
			{
				DialogService.Instance.ShowInfo("Announcement saved successfully.");
				await LoadAsync();
			}
		}
	}

	private async void BtnEditSelected_Click(object sender, RoutedEventArgs e)
	{
		if (!(GetActiveGrid().SelectedItem is DataRowView row))
		{
			DialogService.Instance.ShowWarning("Select a record to edit first.");
		}
		else if (_activeSection == GovernanceSection.Projects)
		{
			ProjectWindow window = new ProjectWindow(ToProjectRecord(row));
			if (DialogService.Instance.ShowDialog(window).GetValueOrDefault())
			{
				DialogService.Instance.ShowInfo("Project or program record updated successfully.");
				await LoadAsync();
			}
		}
		else
		{
			AnnouncementWindow window2 = new AnnouncementWindow(ToAnnouncementRecord(row));
			if (DialogService.Instance.ShowDialog(window2).GetValueOrDefault())
			{
				DialogService.Instance.ShowInfo("Announcement updated successfully.");
				await LoadAsync();
			}
		}
	}

	private void BtnViewSelected_Click(object sender, RoutedEventArgs e)
	{
		if (!(GetActiveGrid().SelectedItem is DataRowView row))
		{
			DialogService.Instance.ShowWarning("Select a record first.");
			return;
		}
		string title = ((_activeSection == GovernanceSection.Projects) ? "Project / Program Details" : "Announcement Details");
		DialogService.Instance.ShowInfo(BuildDetailMessage(row), title);
	}

	private async void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
	{
		if (!(GetActiveGrid().SelectedItem is DataRowView row))
		{
			DialogService.Instance.ShowWarning("Select a record to delete first.");
			return;
		}
		try
		{
			if (_activeSection == GovernanceSection.Projects)
			{
				ProjectRecord projectRecord = ToProjectRecord(row);
				string value = (string.Equals(projectRecord.RecordType, "Program", StringComparison.OrdinalIgnoreCase) ? "program" : "project");
				if (DialogService.Instance.Confirm($"Delete {value} \"{projectRecord.Name}\"?", "Delete Record"))
				{
					await _projectService.DeleteProjectAsync(projectRecord.ProjectId);
					DialogService.Instance.ShowInfo("Project or program record deleted successfully.");
					await LoadAsync();
				}
			}
			else
			{
				AnnouncementRecord announcementRecord = ToAnnouncementRecord(row);
				if (DialogService.Instance.Confirm("Delete announcement \"" + announcementRecord.Title + "\"?", "Delete Announcement"))
				{
					await _announcementService.DeleteAnnouncementAsync(announcementRecord.AnnouncementId);
					DialogService.Instance.ShowInfo("Announcement deleted successfully.");
					await LoadAsync();
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("GovernanceRegistryPage delete failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Delete Record");
		}
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		GetActiveGrid().UnselectAll();
		UpdateSelectionState(null);
	}

	private void AnnouncementGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_activeSection == GovernanceSection.Announcements)
		{
			UpdateSelectionState(announcementGrid.SelectedItem as DataRowView);
		}
	}

	private void ProjectGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_activeSection == GovernanceSection.Projects)
		{
			UpdateSelectionState(projectGrid.SelectedItem as DataRowView);
		}
	}

	private void UpdateSelectionState(DataRowView? row)
	{
		if (row == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			selectedRecordLabel.Text = "No record selected";
			selectedRecordMetaLabel.Text = "Select an announcement or initiative record to review it.";
			return;
		}
		if (_activeSection == GovernanceSection.Projects)
		{
			selectedRecordLabel.Text = ReadString(row.Row, "name");
			selectedRecordMetaLabel.Text = $"{ReadString(row.Row, "record_type")} | {ReadString(row.Row, "status")} | {ReadString(row.Row, "budget_display")}";
		}
		else
		{
			selectedRecordLabel.Text = ReadString(row.Row, "title");
			selectedRecordMetaLabel.Text = $"{ReadString(row.Row, "priority")} | {ReadString(row.Row, "status")} | {ReadString(row.Row, "created_at_display")}";
		}
		contextActionBar.Visibility = Visibility.Visible;
	}

	private string BuildDetailMessage(DataRowView row)
	{
		if (_activeSection != GovernanceSection.Projects)
		{
			return $"Title: {row["title"]}\nPriority: {row["priority"]}\nStatus: {row["status"]}\nPinned: {row["pinned_display"]}\nPublished: {row["created_at_display"]}\n\n{(string.IsNullOrWhiteSpace(ReadString(row.Row, "body")) ? "No message body provided." : ReadString(row.Row, "body"))}";
		}
		return $"Name: {row["name"]}\nType: {row["record_type"]}\nStatus: {row["status"]}\nBudget: {row["budget_display"]}\nLead: {row["lead_display"]}\nSchedule: {row["schedule_display"]}\nAttendance: {row["attendance_display"]}\nLast Activity: {row["last_activity_display"]}\nOutcome: {ReadString(row.Row, "outcome_status")}\n\n{BuildProjectNarrative(row.Row)}";
	}

	private static string BuildProjectNarrative(DataRow row)
	{
		string text = ReadString(row, "outcome_summary");
		string text2 = ReadString(row, "remarks");
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			return "Outcome Summary: " + text + "\n\nRemarks: " + text2;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return "Outcome Summary: " + text;
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return "Remarks: " + text2;
		}
		return "No additional notes recorded.";
	}

	private DataTable? GetActiveData()
	{
		if (_activeSection != GovernanceSection.Projects)
		{
			return _announcementData;
		}
		return _projectData;
	}

	private DataGrid GetActiveGrid()
	{
		if (_activeSection != GovernanceSection.Projects)
		{
			return announcementGrid;
		}
		return projectGrid;
	}

	private string[] GetSearchColumns()
	{
		if (_activeSection == GovernanceSection.Projects)
		{
			return new string[9] { "name", "record_type", "status", "lead", "remarks", "outcome_status", "outcome_summary", "schedule_display", "attendance_display" };
		}
		return new string[5] { "title", "body", "priority", "status", "body_preview_display" };
	}

	private string GetCategoryColumn()
	{
		if (_activeSection != GovernanceSection.Projects)
		{
			return "priority";
		}
		return "record_type";
	}

	private string GetStatusColumn()
	{
		return "status";
	}

	private string GetAllCategoryLabel()
	{
		if (_activeSection != GovernanceSection.Projects)
		{
			return "All Priorities";
		}
		return "All Record Types";
	}

	private string GetAllStatusLabel()
	{
		if (_activeSection != GovernanceSection.Projects)
		{
			return "All Announcement Statuses";
		}
		return "All Initiative Statuses";
	}

	private static void EnrichAnnouncementTable(DataTable table)
	{
		EnsureStringColumn(table, "created_at_display");
		EnsureStringColumn(table, "pinned_display");
		EnsureStringColumn(table, "body_preview_display");
		foreach (DataRow row in table.Rows)
		{
			row["created_at_display"] = ReadDateTime(row, "created_at")?.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture) ?? "Date unavailable";
			row["pinned_display"] = ((ReadInt(row, "is_pinned") != 0) ? "Yes" : "No");
			string value = ReadString(row, "body");
			row["body_preview_display"] = (string.IsNullOrWhiteSpace(value) ? "No message body provided." : TrimSummary(value, 120));
		}
	}

	private static void EnrichProjectTable(DataTable table)
	{
		EnsureStringColumn(table, "budget_display");
		EnsureStringColumn(table, "lead_display");
		EnsureStringColumn(table, "schedule_display");
		EnsureStringColumn(table, "attendance_display");
		EnsureStringColumn(table, "outcome_display");
		EnsureStringColumn(table, "last_activity_display");
		foreach (DataRow row in table.Rows)
		{
			row["budget_display"] = FormatCurrency(ReadDecimal(row, "budget"));
			row["lead_display"] = (string.IsNullOrWhiteSpace(ReadString(row, "lead")) ? "No lead assigned" : ReadString(row, "lead"));
			row["schedule_display"] = BuildProjectScheduleDisplay(ReadDateTime(row, "start_date"), ReadDateTime(row, "end_date"), ReadDateTime(row, "created_at"));
			row["attendance_display"] = BuildAttendanceDisplay(ReadInt(row, "attendance_count"), ReadInt(row, "attendance_target"));
			row["outcome_display"] = BuildOutcomeDisplay(ReadString(row, "outcome_status"), ReadString(row, "outcome_summary"));
			row["last_activity_display"] = ReadDateTime(row, "last_activity_date")?.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture) ?? "No activity";
		}
	}

	private static void EnsureStringColumn(DataTable table, string columnName)
	{
		if (!table.Columns.Contains(columnName))
		{
			table.Columns.Add(columnName, typeof(string));
		}
	}

	private static int CountRows(DataTable? table, Func<DataRow, bool> predicate)
	{
		return table?.AsEnumerable().Count(predicate) ?? 0;
	}

	private static string BuildProjectScheduleDisplay(DateTime? startDate, DateTime? endDate, DateTime? createdAt)
	{
		if (startDate.HasValue && endDate.HasValue)
		{
			return $"{startDate.Value:MMM dd, yyyy} - {endDate.Value:MMM dd, yyyy}";
		}
		if (startDate.HasValue)
		{
			return $"Starts {startDate.Value:MMM dd, yyyy}";
		}
		if (endDate.HasValue)
		{
			return $"Target end {endDate.Value:MMM dd, yyyy}";
		}
		if (!createdAt.HasValue)
		{
			return "Schedule pending";
		}
		return $"Created {createdAt.Value:MMM dd, yyyy}";
	}

	private static string BuildAttendanceDisplay(int attendanceCount, int attendanceTarget)
	{
		if (attendanceTarget > 0)
		{
			return $"{attendanceCount:N0}/{attendanceTarget:N0}";
		}
		if (attendanceCount <= 0)
		{
			return "No attendance";
		}
		return attendanceCount.ToString("N0", CultureInfo.InvariantCulture);
	}

	private static string BuildOutcomeDisplay(string outcomeStatus, string outcomeSummary)
	{
		if (string.IsNullOrWhiteSpace(outcomeSummary))
		{
			if (!string.IsNullOrWhiteSpace(outcomeStatus))
			{
				return outcomeStatus;
			}
			return "Pending";
		}
		return (string.IsNullOrWhiteSpace(outcomeStatus) ? "Pending" : outcomeStatus) + " | " + TrimSummary(outcomeSummary, 48);
	}

	private static string TrimSummary(string value, int maxLength)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length <= maxLength)
		{
			return text;
		}
		return text.Substring(0, Math.Max(0, maxLength - 3)).TrimEnd() + "...";
	}

	private static string FormatCurrency(decimal amount)
	{
		return $"PHP {amount:N2}";
	}

	private static string EscapeForRowFilter(string value)
	{
		return value.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]")
			.Replace("*", "[*]");
	}

	private static int ReadInt(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0;
		}
		return Convert.ToInt32(row[columnName], CultureInfo.InvariantCulture);
	}

	private static decimal ReadDecimal(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return 0m;
		}
		return Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
	}

	private static string ReadString(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(row[columnName], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
	}

	private static DateTime? ReadDateTime(DataRow row, string columnName)
	{
		if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
		{
			return null;
		}
		object obj = row[columnName];
		if (obj is DateTime)
		{
			return (DateTime)obj;
		}
		if (!DateTime.TryParse(Convert.ToString(row[columnName], CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
		{
			return null;
		}
		return result;
	}

	private static AnnouncementRecord ToAnnouncementRecord(DataRowView row)
	{
		return new AnnouncementRecord
		{
			AnnouncementId = ReadInt(row.Row, "announcement_id"),
			Title = ReadString(row.Row, "title"),
			Body = ReadString(row.Row, "body"),
			Priority = ReadString(row.Row, "priority"),
			Status = ReadString(row.Row, "status"),
			IsPinned = (ReadInt(row.Row, "is_pinned") != 0),
			CreatedAt = ReadDateTime(row.Row, "created_at")
		};
	}

	private static ProjectRecord ToProjectRecord(DataRowView row)
	{
		return new ProjectRecord
		{
			ProjectId = ReadInt(row.Row, "project_id"),
			RecordType = ReadString(row.Row, "record_type"),
			Name = ReadString(row.Row, "name"),
			Status = ReadString(row.Row, "status"),
			Budget = ReadDecimal(row.Row, "budget"),
			StartDate = ReadDateTime(row.Row, "start_date"),
			EndDate = ReadDateTime(row.Row, "end_date"),
			Lead = ReadString(row.Row, "lead"),
			Remarks = ReadString(row.Row, "remarks"),
			AttendanceTarget = ReadInt(row.Row, "attendance_target"),
			AttendanceCount = ReadInt(row.Row, "attendance_count"),
			LastActivityDate = ReadDateTime(row.Row, "last_activity_date"),
			OutcomeStatus = ReadString(row.Row, "outcome_status"),
			OutcomeSummary = ReadString(row.Row, "outcome_summary"),
			CreatedAt = ReadDateTime(row.Row, "created_at")
		};
	}}
