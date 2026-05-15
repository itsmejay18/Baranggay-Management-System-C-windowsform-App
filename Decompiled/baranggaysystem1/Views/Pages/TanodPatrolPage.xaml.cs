using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class TanodPatrolPage : UserControl, IRefreshable
{
    private enum TabMode { Shifts, Logs, Members }

    private readonly TanodService _service = new TanodService();
    private TabMode _currentTab = TabMode.Shifts;
    private int? _selectedId;

    public TanodPatrolPage()
    {
        InitializeComponent();
        RefreshFiltersForTab();
        base.Loaded += async (_, __) => await LoadAsync();
    }

    private void RefreshFiltersForTab()
    {
        filterCombo.Items.Clear();
        if (_currentTab == TabMode.Shifts)
        {
            filterCombo.Items.Add(new ComboBoxItem { Content = "All Shifts", Tag = "" });
            foreach (var s in TanodService.ShiftTypes)
                filterCombo.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        }
        else if (_currentTab == TabMode.Logs)
        {
            filterCombo.Items.Add(new ComboBoxItem { Content = "All Severity", Tag = "" });
            foreach (var s in TanodService.Severities)
                filterCombo.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        }
        else
        {
            filterCombo.Items.Add(new ComboBoxItem { Content = "Active Only", Tag = "active" });
            filterCombo.Items.Add(new ComboBoxItem { Content = "Include Inactive", Tag = "all" });
        }
        filterCombo.SelectedIndex = 0;
    }

    private async Task LoadAsync()
    {
        try
        {
            string search = searchBox.Text?.Trim() ?? string.Empty;
            string tag = (filterCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
            DataTable data;

            if (_currentTab == TabMode.Shifts)
                data = await _service.LoadShiftsAsync(null, null, tag);
            else if (_currentTab == TabMode.Logs)
                data = await _service.LoadPatrolLogsAsync(null, null, tag, search);
            else
                data = await _service.LoadMembersAsync(tag == "all");

            ApplyToGrid(data);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("TanodPatrolPage load failed.", ex);
            emptyLabel.Text = "Failed to load data. Please refresh.";
            emptyState.Visibility = Visibility.Visible;
        }
    }

    private void ApplyToGrid(DataTable? table)
    {
        mainGrid.Columns.Clear();
        contextActionBar.Visibility = Visibility.Collapsed;
        _selectedId = null;

        if (table == null || table.Rows.Count == 0)
        {
            mainGrid.ItemsSource = null;
            emptyLabel.Text = _currentTab switch
            {
                TabMode.Shifts => "No patrol shifts scheduled.",
                TabMode.Logs => "No patrol logs yet.",
                _ => "No tanod members registered."
            };
            emptyState.Visibility = Visibility.Visible;
            return;
        }

        emptyState.Visibility = Visibility.Collapsed;

        if (_currentTab == TabMode.Shifts)
        {
            AddDateCol("Date", "shift_date", 1.0);
            AddCol("Type", "shift_type", 0.8);
            AddCol("Start", "start_time", 0.7);
            AddCol("End", "end_time", 0.7);
            AddCol("Area", "area_assignment", 1.5);
            AddCol("Assigned", "assigned_count", 0.6);
            AddCol("Notes", "notes", 1.5);
        }
        else if (_currentTab == TabMode.Logs)
        {
            AddDateTimeCol("Logged", "logged_at", 1.1);
            AddCol("Location", "location", 1.2);
            AddCol("Incident", "incident_type", 1.0);
            AddCol("Severity", "severity", 0.7);
            AddCol("Description", "description", 2.0);
            AddCol("Action", "action_taken", 1.3);
        }
        else
        {
            AddCol("Full Name", "full_name", 1.6);
            AddCol("Rank", "rank_title", 0.9);
            AddCol("Contact", "contact_number", 1.0);
            AddDateCol("Assigned", "date_assigned", 1.0);
            AddCol("Active", "is_active", 0.5);
            AddCol("Remarks", "remarks", 1.5);
        }

        mainGrid.ItemsSource = table.DefaultView;
        recordCountLabel.Text = $"{table.Rows.Count} record(s).";
    }

    private void AddCol(string header, string binding, double star)
    {
        mainGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]"),
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    private void AddDateCol(string header, string binding, double star)
    {
        mainGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]") { StringFormat = "yyyy-MM-dd" },
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    private void AddDateTimeCol(string header, string binding, double star)
    {
        mainGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]") { StringFormat = "yyyy-MM-dd HH:mm" },
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    // ============================================================

    private async void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _currentTab = sender == tabShifts ? TabMode.Shifts
                    : sender == tabLogs ? TabMode.Logs
                    : TabMode.Members;
        RefreshFiltersForTab();
        searchBox.Clear();
        await LoadAsync();
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await LoadAsync();
    }

    private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await LoadAsync();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void MainGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_currentTab == TabMode.Shifts && _selectedId.HasValue)
            OpenAttendance(_selectedId.Value);
    }

    private void BtnAttendance_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTab == TabMode.Shifts && _selectedId.HasValue)
            OpenAttendance(_selectedId.Value);
    }

    private void OpenAttendance(int shiftId)
    {
        string label = selectedLabel.Text ?? $"Shift #{shiftId}";
        var dlg = new TanodShiftAttendanceWindow(shiftId, label);
        var adapter = new DialogContentAdapter(dlg);

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = $"Shift Attendance - {label}",
            Subtitle = "View and manage attendance for this shift",
            OriginRoute = "TanodPatrol",
            Content = adapter,
            Icon = IconChar.ClipboardList,
            ToolbarItems = new List<UIElement>(),
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (mainGrid.SelectedItem is DataRowView row)
        {
            string idCol = _currentTab switch
            {
                TabMode.Shifts => "shift_id",
                TabMode.Logs => "log_id",
                _ => "tanod_id"
            };
            _selectedId = Convert.ToInt32(row[idCol]);
            selectedLabel.Text = _currentTab switch
            {
                TabMode.Shifts => $"{row["shift_date"]:yyyy-MM-dd} - {row["shift_type"]}",
                TabMode.Logs => row["description"]?.ToString() ?? "",
                _ => row["full_name"]?.ToString() ?? ""
            };
            contextActionBar.Visibility = Visibility.Visible;
            // Attendance button only applies to the Shifts tab
            btnAttendance.Visibility = _currentTab == TabMode.Shifts ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            _selectedId = null;
            contextActionBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnNewMember_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new TanodMemberWindow();
        var adapter = new DialogContentAdapter(dlg);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Member", IconChar.Save,
            (s, args) =>
            {
                _currentTab = TabMode.Members;
                NavigationService.Instance.NavigateBackFromFullscreen("TanodPatrol", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "Register New Tanod Member",
            Subtitle = "Add a new member to the tanod roster",
            OriginRoute = "TanodPatrol",
            Content = adapter,
            Icon = IconChar.UserPlus,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private async void BtnNewShift_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new TanodShiftWindow();
        var adapter = new DialogContentAdapter(dlg);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Shift", IconChar.Save,
            (s, args) =>
            {
                _currentTab = TabMode.Shifts;
                NavigationService.Instance.NavigateBackFromFullscreen("TanodPatrol", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "Schedule New Shift",
            Subtitle = "Create a new patrol shift assignment",
            OriginRoute = "TanodPatrol",
            Content = adapter,
            Icon = IconChar.Clock,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private async void BtnNewLog_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new TanodPatrolLogWindow();
        var adapter = new DialogContentAdapter(dlg);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Log", IconChar.Save,
            (s, args) =>
            {
                _currentTab = TabMode.Logs;
                NavigationService.Instance.NavigateBackFromFullscreen("TanodPatrol", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "New Patrol Log Entry",
            Subtitle = "Record a new patrol incident or observation",
            OriginRoute = "TanodPatrol",
            Content = adapter,
            Icon = IconChar.PenSquare,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue) return;
        if (MessageBox.Show("Delete this record?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            if (_currentTab == TabMode.Shifts)
                await _service.DeleteShiftAsync(_selectedId.Value);
            // Logs and members are kept for audit — skip delete.
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private static string ToTitle(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }

    #region IRefreshable Implementation

    /// <summary>
    /// Refreshes the page data after returning from a fullscreen view.
    /// </summary>
    public void RefreshData()
    {
        _ = LoadAsync();
    }

    #endregion
}
