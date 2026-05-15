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

public partial class MeetingsPage : UserControl, IRefreshable
{
    private readonly MeetingsService _service = new MeetingsService();
    private bool _isMeetingsTab = true;
    private int? _selectedId;
    private DataTable? _currentData;

    public MeetingsPage()
    {
        InitializeComponent();
        InitializeFilters();
        base.Loaded += async (_, __) => await LoadAsync();
    }

    private void InitializeFilters()
    {
        typeFilter.Items.Clear();
        typeFilter.Items.Add(new ComboBoxItem { Content = "All Types", Tag = "" });
        foreach (var t in MeetingsService.MeetingTypes)
            typeFilter.Items.Add(new ComboBoxItem { Content = ToTitle(t), Tag = t });
        typeFilter.SelectedIndex = 0;

        statusFilter.Items.Clear();
        statusFilter.Items.Add(new ComboBoxItem { Content = "All Status", Tag = "" });
        foreach (var s in MeetingsService.MeetingStatuses)
            statusFilter.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        statusFilter.SelectedIndex = 0;
    }

    private void RefreshFiltersForTab()
    {
        typeFilter.Items.Clear();
        typeFilter.Items.Add(new ComboBoxItem { Content = "All Types", Tag = "" });
        var types = _isMeetingsTab ? MeetingsService.MeetingTypes : MeetingsService.DocumentTypes;
        foreach (var t in types)
            typeFilter.Items.Add(new ComboBoxItem { Content = ToTitle(t), Tag = t });
        typeFilter.SelectedIndex = 0;

        statusFilter.Items.Clear();
        statusFilter.Items.Add(new ComboBoxItem { Content = "All Status", Tag = "" });
        var statuses = _isMeetingsTab ? MeetingsService.MeetingStatuses : MeetingsService.DocumentStatuses;
        foreach (var s in statuses)
            statusFilter.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        statusFilter.SelectedIndex = 0;
    }

    private async Task LoadAsync()
    {
        try
        {
            string search = searchBox.Text?.Trim() ?? string.Empty;
            string typeF = (typeFilter.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
            string statusF = (statusFilter.SelectedItem as ComboBoxItem)?.Tag as string ?? "";

            if (_isMeetingsTab)
                _currentData = await _service.LoadMeetingsAsync(search, statusF, typeF);
            else
                _currentData = await _service.LoadResolutionsAsync(search, typeF, statusF);

            ApplyToGrid(_currentData);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("MeetingsPage load failed.", ex);
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
            emptyLabel.Text = _isMeetingsTab ? "No meetings scheduled yet." : "No resolutions or ordinances yet.";
            emptyState.Visibility = Visibility.Visible;
            recordCountLabel.Text = _isMeetingsTab
                ? "Schedule sessions, record minutes, and publish resolutions."
                : "Draft, approve, and archive legislative documents.";
            return;
        }

        emptyState.Visibility = Visibility.Collapsed;

        if (_isMeetingsTab)
        {
            AddTextColumn("Title", "title", 2.0);
            AddTextColumn("Type", "meeting_type", 0.8);
            AddDateTimeColumn("Scheduled", "scheduled_at", 1.2);
            AddTextColumn("Venue", "venue", 1.3);
            AddTextColumn("Status", "status", 0.8);
            AddTextColumn("Attendance", "attendance_count", 0.6);
        }
        else
        {
            AddTextColumn("Number", "document_number", 0.9);
            AddTextColumn("Year", "series_year", 0.5);
            AddTextColumn("Type", "document_type", 0.8);
            AddTextColumn("Title", "title", 2.2);
            AddTextColumn("Status", "status", 0.8);
            AddDateColumn("Effectivity", "effectivity_date", 1.0);
        }

        mainGrid.ItemsSource = table.DefaultView;
        recordCountLabel.Text = $"{table.Rows.Count} record(s).";
    }

    private void AddTextColumn(string header, string binding, double star)
    {
        mainGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]"),
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    private void AddDateTimeColumn(string header, string binding, double star)
    {
        mainGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]") { StringFormat = "yyyy-MM-dd HH:mm" },
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    private void AddDateColumn(string header, string binding, double star)
    {
        mainGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]") { StringFormat = "yyyy-MM-dd" },
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    // ============================================================
    // EVENT HANDLERS
    // ============================================================

    private async void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _isMeetingsTab = (sender == tabMeetings);
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

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (mainGrid.SelectedItem is DataRowView row)
        {
            _selectedId = _isMeetingsTab
                ? Convert.ToInt32(row["meeting_id"])
                : Convert.ToInt32(row["resolution_id"]);
            selectedLabel.Text = row["title"]?.ToString() ?? "";
            contextActionBar.Visibility = Visibility.Visible;
        }
        else
        {
            _selectedId = null;
            contextActionBar.Visibility = Visibility.Collapsed;
        }
    }

    private void MainGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_selectedId.HasValue) OpenEditor(_selectedId.Value);
    }

    private async void BtnNewMeeting_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MeetingEditorWindow(null);
        var adapter = new DialogContentAdapter(dialog);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Meeting", IconChar.Save,
            (s, args) =>
            {
                _isMeetingsTab = true;
                NavigationService.Instance.NavigateBackFromFullscreen("Meetings", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "Schedule New Meeting",
            Subtitle = "Create a new meeting session",
            OriginRoute = "Meetings",
            Content = adapter,
            Icon = IconChar.CalendarPlus,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private async void BtnNewResolution_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ResolutionEditorWindow(null);
        var adapter = new DialogContentAdapter(dialog);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Resolution", IconChar.Save,
            (s, args) =>
            {
                _isMeetingsTab = false;
                NavigationService.Instance.NavigateBackFromFullscreen("Meetings", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "Draft New Resolution",
            Subtitle = "Create a new resolution or ordinance",
            OriginRoute = "Meetings",
            Content = adapter,
            Icon = IconChar.FileSignature,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId.HasValue) OpenEditor(_selectedId.Value);
    }

    private async void OpenEditor(int id)
    {
        if (_isMeetingsTab)
        {
            var dialog = new MeetingEditorWindow(id);
            var adapter = new DialogContentAdapter(dialog);

            var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Changes", IconChar.Save,
                (s, args) =>
                {
                    NavigationService.Instance.NavigateBackFromFullscreen("Meetings", refreshOnReturn: true);
                });

            NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
            {
                Title = "Edit Meeting",
                Subtitle = "Update meeting details",
                OriginRoute = "Meetings",
                Content = adapter,
                Icon = IconChar.CalendarCheck,
                ToolbarItems = new List<UIElement> { saveButton },
                ShowSideToolbar = false,
                OnSaved = () => RefreshData()
            });
        }
        else
        {
            var dialog = new ResolutionEditorWindow(id);
            var adapter = new DialogContentAdapter(dialog);

            var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Changes", IconChar.Save,
                (s, args) =>
                {
                    NavigationService.Instance.NavigateBackFromFullscreen("Meetings", refreshOnReturn: true);
                });

            NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
            {
                Title = "Edit Resolution",
                Subtitle = "Update resolution or ordinance details",
                OriginRoute = "Meetings",
                Content = adapter,
                Icon = IconChar.FileSignature,
                ToolbarItems = new List<UIElement> { saveButton },
                ShowSideToolbar = false,
                OnSaved = () => RefreshData()
            });
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue) return;
        var result = MessageBox.Show(
            "Are you sure you want to delete this record? This cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            if (_isMeetingsTab)
                await _service.DeleteMeetingAsync(_selectedId.Value);
            else
                await _service.DeleteResolutionAsync(_selectedId.Value);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Delete failed.", ex);
            MessageBox.Show("Failed to delete: " + ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
