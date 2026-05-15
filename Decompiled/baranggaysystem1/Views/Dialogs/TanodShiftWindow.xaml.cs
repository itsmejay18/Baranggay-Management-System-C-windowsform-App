using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class TanodShiftWindow : Window
{
    private readonly TanodService _service = new TanodService();

    public TanodShiftWindow()
    {
        InitializeComponent();
        InitializeCombos();
        dpDate.SelectedDate = DateTime.Today;
        _ = LoadTanodsAsync();
    }

    private void InitializeCombos()
    {
        typeCombo.Items.Clear();
        foreach (var t in TanodService.ShiftTypes)
            typeCombo.Items.Add(new ComboBoxItem { Content = ToTitle(t), Tag = t });
        typeCombo.SelectedIndex = 0;
    }

    private async Task LoadTanodsAsync()
    {
        try
        {
            var table = await _service.LoadMembersAsync(false);
            tanodList.ItemsSource = table.DefaultView;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load tanod members failed.", ex);
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!dpDate.SelectedDate.HasValue)
        {
            MessageBox.Show("Shift date is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryParseTime(txtStart.Text, out var startTime))
        {
            MessageBox.Show("Start time must be HH:MM format.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtStart.Focus();
            return;
        }
        if (!TryParseTime(txtEnd.Text, out var endTime))
        {
            MessageBox.Show("End time must be HH:MM format.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtEnd.Focus();
            return;
        }

        var ids = new List<int>();
        foreach (var item in tanodList.SelectedItems)
        {
            if (item is DataRowView row)
                ids.Add(Convert.ToInt32(row["tanod_id"]));
        }

        string type = (typeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "MORNING";

        btnSave.IsEnabled = false;
        try
        {
            await _service.CreateShiftAsync(dpDate.SelectedDate.Value, type, startTime, endTime,
                txtArea.Text, txtNotes.Text, ids.ToArray());
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save shift failed.", ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private static string ToTitle(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }
}
