using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class ResolutionEditorWindow : Window
{
    private readonly MeetingsService _service = new MeetingsService();
    private readonly int? _resolutionId;

    public ResolutionEditorWindow() : this(null) { }

    public ResolutionEditorWindow(int? resolutionId)
    {
        InitializeComponent();
        _resolutionId = resolutionId;
        InitializeCombos();

        if (resolutionId.HasValue)
        {
            eyebrowText.Text = "EDIT DOCUMENT";
            headerTitleText.Text = "Update document details";
            btnSave.Content = "Save Changes";
            _ = LoadAsync(resolutionId.Value);
        }
        else
        {
            txtYear.Text = DateTime.Today.Year.ToString();
        }
    }

    private void InitializeCombos()
    {
        typeCombo.Items.Clear();
        foreach (var t in MeetingsService.DocumentTypes)
            typeCombo.Items.Add(new ComboBoxItem { Content = ToTitle(t), Tag = t });
        typeCombo.SelectedIndex = 0;

        statusCombo.Items.Clear();
        foreach (var s in MeetingsService.DocumentStatuses)
            statusCombo.Items.Add(new ComboBoxItem { Content = ToTitle(s), Tag = s });
        statusCombo.SelectedIndex = 0;
    }

    private async Task LoadAsync(int id)
    {
        try
        {
            var table = await _service.GetResolutionAsync(id);
            if (table.Rows.Count == 0) return;
            var row = table.Rows[0];
            SelectCombo(typeCombo, row["document_type"]?.ToString());
            txtNumber.Text = row["document_number"]?.ToString() ?? "";
            txtYear.Text = row["series_year"]?.ToString() ?? DateTime.Today.Year.ToString();
            txtTitle.Text = row["title"]?.ToString() ?? "";
            txtDescription.Text = row["description"]?.ToString() ?? "";
            txtFullText.Text = row["full_text"]?.ToString() ?? "";
            if (row["effectivity_date"] != DBNull.Value)
                dpEffectivity.SelectedDate = Convert.ToDateTime(row["effectivity_date"]);
            txtAuthor.Text = row["authored_by"]?.ToString() ?? "";
            SelectCombo(statusCombo, row["status"]?.ToString());
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load resolution failed.", ex);
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNumber.Text))
        {
            MessageBox.Show("Document number is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtNumber.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show("Title is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtTitle.Focus();
            return;
        }
        if (!int.TryParse(txtYear.Text, out int year) || year < 1900 || year > 2100)
        {
            MessageBox.Show("Enter a valid series year (e.g. 2026).", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtYear.Focus();
            return;
        }

        string type = (typeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "RESOLUTION";
        string status = (statusCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "DRAFT";

        btnSave.IsEnabled = false;
        try
        {
            if (_resolutionId.HasValue)
            {
                await _service.UpdateResolutionAsync(_resolutionId.Value, type, txtNumber.Text,
                    year, txtTitle.Text, txtDescription.Text, txtFullText.Text,
                    dpEffectivity.SelectedDate, txtAuthor.Text, status);
            }
            else
            {
                await _service.CreateResolutionAsync(type, txtNumber.Text, year, txtTitle.Text,
                    txtDescription.Text, txtFullText.Text, dpEffectivity.SelectedDate,
                    txtAuthor.Text, null);
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save resolution failed.", ex);
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
