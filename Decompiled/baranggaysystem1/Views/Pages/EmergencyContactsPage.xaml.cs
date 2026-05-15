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

public partial class EmergencyContactsPage : UserControl, IRefreshable
{
    private readonly EmergencyContactService _service = new EmergencyContactService();
    private int? _selectedId;
    private string? _selectedPhone;

    public EmergencyContactsPage()
    {
        InitializeComponent();
        InitializeFilters();
        base.Loaded += async (_, __) => await LoadAsync();
    }

    private void InitializeFilters()
    {
        categoryFilter.Items.Clear();
        categoryFilter.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = "" });
        foreach (var c in EmergencyContactService.Categories)
            categoryFilter.Items.Add(new ComboBoxItem { Content = ToTitle(c), Tag = c });
        categoryFilter.SelectedIndex = 0;
    }

    private async Task LoadAsync()
    {
        try
        {
            string search = searchBox.Text?.Trim() ?? string.Empty;
            string cat = (categoryFilter.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
            bool priority = chkPriorityOnly.IsChecked == true;
            var table = await _service.LoadAsync(search, cat, priority, false);
            ApplyToGrid(table);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("EmergencyContactsPage load failed.", ex);
            emptyLabel.Text = "Failed to load data. Please refresh.";
            emptyState.Visibility = Visibility.Visible;
        }
    }

    private void ApplyToGrid(DataTable? table)
    {
        mainGrid.Columns.Clear();
        contextActionBar.Visibility = Visibility.Collapsed;
        _selectedId = null;
        _selectedPhone = null;

        if (table == null || table.Rows.Count == 0)
        {
            mainGrid.ItemsSource = null;
            emptyLabel.Text = "No contacts match your filters.";
            emptyState.Visibility = Visibility.Visible;
            recordCountLabel.Text = "Police, fire, medical, disaster, and utility hotlines.";
            return;
        }
        emptyState.Visibility = Visibility.Collapsed;

        AddCol("Agency", "agency_name", 1.8);
        AddCol("Category", "category", 0.8);
        AddCol("Contact Person", "contact_person", 1.2);
        AddCol("Primary Phone", "phone_primary", 1.0);
        AddCol("Secondary", "phone_secondary", 0.9);
        AddCol("Email", "email", 1.3);
        AddCol("Priority", "is_priority", 0.5);
        AddCol("Active", "is_active", 0.5);

        mainGrid.ItemsSource = table.DefaultView;
        recordCountLabel.Text = $"{table.Rows.Count} contact(s).";
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

    // ============================================================

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await LoadAsync();
    }

    private async void Filter_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        await LoadAsync();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (mainGrid.SelectedItem is DataRowView row)
        {
            _selectedId = Convert.ToInt32(row["contact_id"]);
            _selectedPhone = row["phone_primary"]?.ToString();
            selectedLabel.Text = row["agency_name"]?.ToString() ?? "";
            contextActionBar.Visibility = Visibility.Visible;
        }
        else
        {
            _selectedId = null;
            _selectedPhone = null;
            contextActionBar.Visibility = Visibility.Collapsed;
        }
    }

    private void MainGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_selectedId.HasValue) OpenEditor(_selectedId.Value);
    }

    private async void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new EmergencyContactWindow();
        var adapter = new DialogContentAdapter(dlg);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Contact", IconChar.Save,
            (s, args) =>
            {
                NavigationService.Instance.NavigateBackFromFullscreen("EmergencyContacts", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "Add Emergency Contact",
            Subtitle = "Register a new emergency hotline or agency",
            OriginRoute = "EmergencyContacts",
            Content = adapter,
            Icon = IconChar.PhoneSquareAlt,
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
        var dlg = new EmergencyContactWindow(id);
        var adapter = new DialogContentAdapter(dlg);

        var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Changes", IconChar.Save,
            (s, args) =>
            {
                NavigationService.Instance.NavigateBackFromFullscreen("EmergencyContacts", refreshOnReturn: true);
            });

        NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
        {
            Title = "Edit Emergency Contact",
            Subtitle = "Update contact information",
            OriginRoute = "EmergencyContacts",
            Content = adapter,
            Icon = IconChar.Edit,
            ToolbarItems = new List<UIElement> { saveButton },
            ShowSideToolbar = false,
            OnSaved = () => RefreshData()
        });
    }

    private void BtnCopyPhone_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedPhone)) return;
        try
        {
            Clipboard.SetText(_selectedPhone);
            MessageBox.Show($"Copied: {_selectedPhone}", "Copied",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch { }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedId.HasValue) return;
        if (MessageBox.Show("Delete this emergency contact?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            await _service.DeleteAsync(_selectedId.Value);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Delete emergency contact failed.", ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
