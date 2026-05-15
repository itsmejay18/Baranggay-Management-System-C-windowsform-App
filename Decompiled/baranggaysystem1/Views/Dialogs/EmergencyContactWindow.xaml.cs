using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class EmergencyContactWindow : Window
{
    private readonly EmergencyContactService _service = new EmergencyContactService();
    private readonly int? _contactId;

    public EmergencyContactWindow() : this(null) { }

    public EmergencyContactWindow(int? contactId)
    {
        InitializeComponent();
        _contactId = contactId;
        InitializeCombos();
        if (contactId.HasValue)
        {
            eyebrowText.Text = "EDIT CONTACT";
            headerTitleText.Text = "Update emergency contact";
            btnSave.Content = "Save Changes";
            _ = LoadAsync(contactId.Value);
        }
    }

    private void InitializeCombos()
    {
        categoryCombo.Items.Clear();
        foreach (var c in EmergencyContactService.Categories)
            categoryCombo.Items.Add(new ComboBoxItem { Content = ToTitle(c), Tag = c });
        categoryCombo.SelectedIndex = 0;
    }

    private async Task LoadAsync(int id)
    {
        try
        {
            var table = await _service.GetAsync(id);
            if (table.Rows.Count == 0) return;
            var row = table.Rows[0];
            SelectCombo(categoryCombo, row["category"]?.ToString());
            txtAgency.Text = row["agency_name"]?.ToString() ?? "";
            txtPerson.Text = row["contact_person"]?.ToString() ?? "";
            txtPhonePrimary.Text = row["phone_primary"]?.ToString() ?? "";
            txtPhoneSecondary.Text = row["phone_secondary"]?.ToString() ?? "";
            txtEmail.Text = row["email"]?.ToString() ?? "";
            txtAddress.Text = row["address"]?.ToString() ?? "";
            txtNotes.Text = row["notes"]?.ToString() ?? "";
            chkPriority.IsChecked = row["is_priority"] != DBNull.Value &&
                                    Convert.ToInt32(row["is_priority"]) == 1;
            chkActive.IsChecked = row["is_active"] != DBNull.Value &&
                                  Convert.ToInt32(row["is_active"]) == 1;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load emergency contact failed.", ex);
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtAgency.Text))
        {
            MessageBox.Show("Agency name is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtAgency.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtPhonePrimary.Text))
        {
            MessageBox.Show("Primary phone is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtPhonePrimary.Focus();
            return;
        }
        string category = (categoryCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "OTHER";

        btnSave.IsEnabled = false;
        try
        {
            if (_contactId.HasValue)
            {
                await _service.UpdateAsync(_contactId.Value, category, txtAgency.Text, txtPerson.Text,
                    txtPhonePrimary.Text, txtPhoneSecondary.Text, txtEmail.Text, txtAddress.Text,
                    txtNotes.Text, chkPriority.IsChecked == true, chkActive.IsChecked == true);
            }
            else
            {
                await _service.CreateAsync(category, txtAgency.Text, txtPerson.Text,
                    txtPhonePrimary.Text, txtPhoneSecondary.Text, txtEmail.Text, txtAddress.Text,
                    txtNotes.Text, chkPriority.IsChecked == true);
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save emergency contact failed.", ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
