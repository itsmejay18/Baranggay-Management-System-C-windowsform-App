using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class FacilityBookingWindow : Window
{
    private readonly FacilityBookingService _service = new FacilityBookingService();
    private readonly int? _bookingId;

    public FacilityBookingWindow() : this(null) { }

    public FacilityBookingWindow(int? bookingId)
    {
        InitializeComponent();
        _bookingId = bookingId;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadFacilitiesAsync();
        if (_bookingId.HasValue)
        {
            Title = "Edit Booking";
            btnSave.Content = "Save Changes";
            await LoadBookingAsync(_bookingId.Value);
        }
        else
        {
            dpStart.SelectedDate = DateTime.Today.AddDays(1);
            dpEnd.SelectedDate = DateTime.Today.AddDays(1);
        }
    }

    private async Task LoadFacilitiesAsync()
    {
        try
        {
            var table = await _service.LoadFacilitiesAsync(false);
            facilityCombo.ItemsSource = table.DefaultView;
            if (table.Rows.Count > 0 && !_bookingId.HasValue) facilityCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load facilities failed.", ex);
        }
    }

    private async Task LoadBookingAsync(int id)
    {
        try
        {
            var table = await _service.GetBookingAsync(id);
            if (table.Rows.Count == 0) return;
            var row = table.Rows[0];
            facilityCombo.SelectedValue = Convert.ToInt32(row["facility_id"]);
            txtRequester.Text = row["requester_name"]?.ToString() ?? "";
            txtContact.Text = row["requester_contact"]?.ToString() ?? "";
            txtPurpose.Text = row["purpose"]?.ToString() ?? "";
            if (row["start_at"] != DBNull.Value)
            {
                var s = Convert.ToDateTime(row["start_at"]);
                dpStart.SelectedDate = s.Date;
                txtStartTime.Text = s.ToString("HH:mm");
            }
            if (row["end_at"] != DBNull.Value)
            {
                var e2 = Convert.ToDateTime(row["end_at"]);
                dpEnd.SelectedDate = e2.Date;
                txtEndTime.Text = e2.ToString("HH:mm");
            }
            if (row["expected_guests"] != DBNull.Value)
                txtGuests.Text = row["expected_guests"].ToString();
            if (row["total_amount"] != DBNull.Value)
                txtAmount.Text = Convert.ToDecimal(row["total_amount"]).ToString("0.00", CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load booking failed.", ex);
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (facilityCombo.SelectedValue == null)
        {
            MessageBox.Show("Please select a facility.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            facilityCombo.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtRequester.Text))
        {
            MessageBox.Show("Requester name is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtRequester.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtPurpose.Text))
        {
            MessageBox.Show("Purpose is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtPurpose.Focus();
            return;
        }
        if (!dpStart.SelectedDate.HasValue || !dpEnd.SelectedDate.HasValue)
        {
            MessageBox.Show("Start and end dates are required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryParseTime(txtStartTime.Text, out var startTime))
        {
            MessageBox.Show("Start time must be in HH:MM format.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtStartTime.Focus();
            return;
        }
        if (!TryParseTime(txtEndTime.Text, out var endTime))
        {
            MessageBox.Show("End time must be in HH:MM format.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtEndTime.Focus();
            return;
        }

        var startAt = dpStart.SelectedDate.Value.Date.Add(startTime);
        var endAt = dpEnd.SelectedDate.Value.Date.Add(endTime);

        if (endAt <= startAt)
        {
            MessageBox.Show("End date/time must be after the start.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(txtGuests.Text, out int guests);
        decimal.TryParse(txtAmount.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);
        int facilityId = Convert.ToInt32(facilityCombo.SelectedValue);

        btnSave.IsEnabled = false;
        try
        {
            if (_bookingId.HasValue)
            {
                await _service.UpdateBookingAsync(_bookingId.Value, facilityId, txtRequester.Text,
                    txtContact.Text, txtPurpose.Text, startAt, endAt,
                    guests > 0 ? guests : (int?)null, amount);
            }
            else
            {
                await _service.CreateBookingAsync(facilityId, txtRequester.Text, txtContact.Text,
                    txtPurpose.Text, startAt, endAt, guests > 0 ? guests : (int?)null, amount, null);
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save booking failed.", ex);
            MessageBox.Show(ex.Message, "Cannot save", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnSave.IsEnabled = true;
        }
    }

    private static bool TryParseTime(string text, out TimeSpan time)
    {
        return TimeSpan.TryParseExact((text ?? "").Trim(), new[] { "h\\:mm", "hh\\:mm" },
            CultureInfo.InvariantCulture, out time);
    }
}
