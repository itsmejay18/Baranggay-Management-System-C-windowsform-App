using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.ViewModels.Navigation;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Fullscreen form panel for creating and editing household records.
/// Replaces the HouseholdEditorWindow and HouseholdDetailsWindow dialogs.
/// </summary>
public class HouseholdFormPanel : FullscreenFormBase
{
    private readonly FormMode _mode;
    private readonly int? _householdId;
    private readonly int? _residentId;

    private TextBox _streetField = new TextBox();
    private TextBox _purokField = new TextBox();
    private TextBox _coordinatesField = new TextBox();

    public HouseholdFormPanel(FormMode mode, int? householdId = null, int? residentId = null)
    {
        _mode = mode;
        _householdId = householdId;
        _residentId = residentId;

        BuildLayout();
        Loaded += async (_, __) => await LoadDataAsync();
    }

    private void BuildLayout()
    {
        var panel = new StackPanel { Margin = new Thickness(24) };

        var header = new TextBlock
        {
            Text = _mode == FormMode.Create ? "New Household" : "Edit Household",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        panel.Children.Add(header);

        panel.Children.Add(CreateFieldGroup("Street / Address", _streetField));
        panel.Children.Add(CreateFieldGroup("Purok", _purokField));
        panel.Children.Add(CreateFieldGroup("Coordinates", _coordinatesField));

        _streetField.TextChanged += (_, __) => MarkFieldDirty();
        _purokField.TextChanged += (_, __) => MarkFieldDirty();
        _coordinatesField.TextChanged += (_, __) => MarkFieldDirty();

        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private static UIElement CreateFieldGroup(string label, TextBox field)
    {
        var group = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        group.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 4) });
        field.MinHeight = 32;
        field.Padding = new Thickness(8, 6, 8, 6);
        group.Children.Add(field);
        return group;
    }

    private async Task LoadDataAsync()
    {
        if (_mode == FormMode.Edit && _householdId.HasValue)
        {
            // Load existing household data for editing
            await Task.CompletedTask;
        }
    }

    protected override bool ValidateForm()
    {
        return !string.IsNullOrWhiteSpace(_streetField.Text);
    }

    protected override async Task<bool> SaveAsync()
    {
        await Task.Delay(100);
        return true;
    }

    protected override void ResetForm()
    {
        _streetField.Clear();
        _purokField.Clear();
        _coordinatesField.Clear();
    }

    protected override IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(_streetField.Text))
            errors.Add("Street / Address is required.");
        return errors;
    }
}
