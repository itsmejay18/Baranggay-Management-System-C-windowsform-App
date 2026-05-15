using System;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class TanodPatrolLogWindow : Window
{
    private readonly TanodService _service = new TanodService();

    public TanodPatrolLogWindow()
    {
        InitializeComponent();
        InitializeCombos();
    }

    private void InitializeCombos()
    {
        severityCombo.Items.Clear();
        foreach (var s in TanodService.Severities)
            severityCombo.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        severityCombo.SelectedIndex = 0;
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtDescription.Text))
        {
            MessageBox.Show("Description is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtDescription.Focus();
            return;
        }

        string severity = (severityCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "LOW";

        btnSave.IsEnabled = false;
        try
        {
            await _service.CreatePatrolLogAsync(null, txtLocation.Text, txtIncident.Text,
                txtDescription.Text, severity, txtAction.Text, txtReporter.Text);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save patrol log failed.", ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnSave.IsEnabled = true;
        }
    }

    private static string ToTitle(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }
}
