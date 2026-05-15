using System;
using System.Windows;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class TanodMemberWindow : Window
{
    private readonly TanodService _service = new TanodService();

    public TanodMemberWindow()
    {
        InitializeComponent();
        dpAssigned.SelectedDate = DateTime.Today;
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtFullName.Text))
        {
            MessageBox.Show("Full name is required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtFullName.Focus();
            return;
        }

        btnSave.IsEnabled = false;
        try
        {
            await _service.CreateMemberAsync(txtFullName.Text, txtContact.Text, txtRank.Text,
                dpAssigned.SelectedDate, null, txtRemarks.Text);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Save tanod member failed.", ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnSave.IsEnabled = true;
        }
    }
}
