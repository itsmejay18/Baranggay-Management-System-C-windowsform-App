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

public partial class FacilityBookingPage : UserControl, IRefreshable
{
    private readonly FacilityBookingService _service = new FacilityBookingService();
    private bool _isBookingsTab = true;
    private int? _selectedId;

    public FacilityBookingPage()
    {
        InitializeComponent();
        InitializeFilters();
        base.Loaded += async (_, __) => await LoadAsync();
    }

    private void InitializeFilters()
    {
        RefreshFiltersForTab();
    }

    private void RefreshFiltersForTab()
    {
        statusFilter.Items.Clear();
        if (_isBookingsTab)
        {
            statusFilter.Items.Add(new ComboBoxItem { Content = "All Status", Tag = "" });
            foreach (var s in FacilityBookingService.BookingStatuses)
                statusFilter.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        }
        else
        {
            statusFilter.Items.Add(new ComboBoxItem { Content = "Active Only", Tag = "active" });
            statusFilter.Items.Add(new ComboBoxItem { Content = "Include Inactive", Tag = "all" });
        }
        statusFilter.SelectedIndex = 0;
    }

    private async Task LoadAsync()
    {
        try
        {
            string search = searchBox.Text?.Trim() ?? string.Empty;
            string tag = (statusFilter.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
            DataTable data;

            if (_isBookingsTab)
                data = await _service.LoadBookingsAsync(search, tag);
            else
                data = await _service.LoadFacilitiesAsync(tag == "all");

            ApplyToGrid(data);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("FacilityBookingPage load failed.", ex);
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
            emptyLabel.Text = _isBookingsTab ? "No bookings yet." : "No facilities registered.";
            emptyState.Visibility = Visibility.Visible;
            recordCountLabel.Text = _isBookingsTab
                ? "Manage reservations for halls, courts, and equipment."
                : "Register facilities available for booking.";
            return;
        }

        emptyState.Visibility = Visibility.Collapsed;

        if (_isBookingsTab)
        {
            AddCol("Facility", "facility_name", 1.4);
            AddCol("Requester", "requester_name", 1.4);
            AddCol("Purpose", "purpose", 1.8);
            AddDateTimeCol("Start", "start_at", 1.1);
            AddDateTimeCol("End", "end_at", 1.1);
            AddCol("Amount", "total_amount", 0.7);
            AddCol("Payment", "payment_status", 0.8);
            AddCol("Status", "status", 0.8);
        }
        else
        {
            AddCol("Facility", "facility_name", 2.0);
            AddCol("Type", "facility_type", 0.8);
            AddCol("Capacity", "capacity", 0.7);
            AddCol("Rate/hr", "hourly_rate", 0.8);
            AddCol("Location", "location", 1.5);
            AddCol("Active", "is_active", 0.5);
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
        _isBookingsTab = (sender == tabBookings);
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

    private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (mainGrid.SelectedItem is DataRowView row)
        {
            _selectedId = _isBookingsTab
                ? Convert.ToInt32(row["booking_id"])
                : Convert.ToInt32(row["facility_id"]);
            selectedLabel.Text = _isBookingsTab
                ? $"{row["requester_name"]} - {row["facility_name"]}"
                : row["facility_name"]?.ToString() ?? "";
            contextActionBar.Visibility = Visibility.Visible;

            // Hide booking-specific actions on facilities tab
            btnEdit.Visibility = _isBookingsTab ? Visibility.Visible : Visibility.Collapsed;
            btnApprove.Visibility = _isBookingsTab ? Visibility.Visible : Visibility.Collapsed;
            btnReject.Visibility = _isBookingsTab ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Visibility = _isBookingsTab ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            _selectedId = null;
            contextActionBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnNewBooking_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FacilityBookingWindow(null);
        var adapter = new DialogContentAdapter(dialog);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Booking", IconChar.Save,
            (s, args) =>
            {
                _isBookingsTab = true;
                NavigationService.Instance.NavigateBackFromFullscreen("FacilityBooking", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "New Facility Booking",
            Subtitle = "Reserve a facility for an event or activity",
            OriginRoute = "FacilityBooking",
            Content = adapter,
            Icon = IconChar.CalendarPlus,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private void MainGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isBookingsTab && _selectedId.HasValue) OpenEditDialog(_selectedId.Value);
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (_isBookingsTab && _selectedId.HasValue) OpenEditDialog(_selectedId.Value);
    }

    private async void OpenEditDialog(int bookingId)
    {
        var dialog = new FacilityBookingWindow(bookingId);
        var adapter = new DialogContentAdapter(dialog);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Changes", IconChar.Save,
            (s, args) =>
            {
                NavigationService.Instance.NavigateBackFromFullscreen("FacilityBooking", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "Edit Booking",
            Subtitle = "Update booking details",
            OriginRoute = "FacilityBooking",
            Content = adapter,
            Icon = IconChar.Edit,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private async void BtnApprove_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue) return;
        try
        {
            await _service.UpdateBookingStatusAsync(_selectedId.Value, "APPROVED");
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void BtnReject_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue) return;
        try
        {
            await _service.UpdateBookingStatusAsync(_selectedId.Value, "REJECTED", "Rejected by staff.");
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue) return;
        if (MessageBox.Show("Cancel this booking?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            await _service.UpdateBookingStatusAsync(_selectedId.Value, "CANCELLED", "Cancelled by staff.");
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue) return;
        if (MessageBox.Show("Delete this record permanently?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            if (_isBookingsTab)
                await _service.DeleteBookingAsync(_selectedId.Value);
            // Facilities: we soft-disable via update; keep delete out.
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
