using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class MeetingEditorWindow : Window
{
    private readonly MeetingsService _service = new MeetingsService();
    private readonly int? _meetingId;

    public MeetingEditorWindow() : this(null) { }

    public MeetingEditorWindow(int? meetingId)
    {
        InitializeComponent();
        _meetingId = meetingId;
        InitializeCombos();

        if (meetingId.HasValue)
        {
            eyebrowText.Text = "EDIT MEETING";
            headerTitleText.Text = "Update meeting details";
            btnSave.Content = "Save Changes";
            _ = LoadAsync(meetingId.Value);
        }
        else
        {
            dpDate.SelectedDate = DateTime.Today;
        }
    }

    private void InitializeCombos()
    {
        typeCombo.Items.Clear();
        foreach (var t in MeetingsService.MeetingTypes)
            typeCombo.Items.Add(new ComboBoxItem { Content = ToTitle(t), Tag = t });
        typeCombo.SelectedIndex = 0;

        statusCombo.Items.Clear();
        foreach (var s in MeetingsService.MeetingStatuses)
            statusCombo.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        statusCombo.SelectedIndex = 0;
    }

    private async Task LoadAsync(int id)
    {
        try
        {
            var table = await _service.GetMeetingAsync(id);
            if (table.Rows.Count == 0) return;
            var row = table.Rows[0];
            txtTitle.Text = row["title"]?.ToString() ?? "";
            SelectCombo(typeCombo, row["meeting_type"]?.ToString());
            SelectCombo(statusCombo, row["status"]?.ToString());
            if (row["scheduled_at"] != DBNull.Value)
            {
                var dt = Convert.ToDateTime(row["scheduled_at"]);
                dpDate.SelectedDate = dt.Date;
                txtTime.Text = dt.ToString("HH:mm");
            }
            txtVenue.Text = row["venue"]?.ToString() ?? "";
            txtAgenda.Text = row["agenda"]?.ToString() ?? "";
            txtMinutes.Text = row["minutes"]?.ToString() ?? "";
            txtAttendance.Text = row["attendance_count"]?.ToString() ?? "0";
            chkQuorum.IsChecked = row["quorum_reached"] != DBNull.Value &&
                                  Convert.ToInt32(row["quorum_reached"]) == 1;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load meeting failed.", ex);
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show("Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtTitle.Focus();
            return;
        }
        if (!dpDate.SelectedDate.HasValue)
        {
            MessageBox.Show("Date is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            dpDate.Focus();
            return;
        }
        if (!TimeSpan.TryParseExact(txtTime.Text.Trim(), new[] { "h\\:mm", "hh\\:mm" },
            CultureInfo.InvariantCulture, out var time))
        {
            MessageBox.Show("Time must be in HH:MM format (e.g. 09:00 or 14:30).",
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtTime.Focus();
            return;
        }

        int.TryParse(txtAttendance.Text, out int attendance);
        var scheduled = dpDate.SelectedDate.Value.Date.Add(time);
        string type = (typeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "REGULAR";
        string status = (statusCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "SCHEDULED";

        btnSave.IsEnabled = false;
        try
        {
            if (_meetingId.HasValue)
            {
                await _service.UpdateMeetingAsync(_meetingId.Value, txtTitle.Text, type,
                    scheduled, txtVenue.Text, txtAgenda.Text, txtMinutes.Text, status,
                    attendance, chkQuorum.IsChecked == true);
            }
            else
            {
                await _service.CreateMeetingAsync(txtTitle.Text, type, scheduled,
                    txtVenue.Text, txtAgenda.Text);
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save meeting failed.", ex);
            MessageBox.Show("Failed to save: " + ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnSave.IsEnabled = true;
        }
    }

    private static void SelectCombo(ComboBox combo, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        foreach (ComboBoxItem item in combo.Items)
        {
            if (string.Equals(item.Tag as string, value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private static string ToTitle(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }
}
