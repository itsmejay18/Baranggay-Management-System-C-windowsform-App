using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Inline form validation feedback panel.
/// Provides consistent error/success/info display for all forms.
/// </summary>
public partial class FormValidationPanel : UserControl
{
    public FormValidationPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Show validation errors.
    /// </summary>
    public void ShowErrors(IEnumerable<string> errors)
    {
        var errorList2 = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList()
            ?? new List<string>();

        if (errorList2.Count == 0)
        {
            Clear();
            return;
        }

        ApplyStyle(ValidationStyle.Error);
        headerText.Text = errorList2.Count == 1 ? "Validation Error" : $"Please fix {errorList2.Count} issue(s):";
        headerText.Visibility = Visibility.Visible;
        errorList.ItemsSource = errorList2;
        errorList.Visibility = Visibility.Visible;
        singleMessage.Visibility = Visibility.Collapsed;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show a single error message.
    /// </summary>
    public void ShowError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Clear();
            return;
        }

        ApplyStyle(ValidationStyle.Error);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show a success message.
    /// </summary>
    public void ShowSuccess(string message)
    {
        ApplyStyle(ValidationStyle.Success);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show an info message.
    /// </summary>
    public void ShowInfo(string message)
    {
        ApplyStyle(ValidationStyle.Info);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Show a warning message.
    /// </summary>
    public void ShowWarning(string message)
    {
        ApplyStyle(ValidationStyle.Warning);
        headerText.Visibility = Visibility.Collapsed;
        errorList.Visibility = Visibility.Collapsed;
        singleMessage.Text = message;
        singleMessage.Visibility = Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Clear and hide the panel.
    /// </summary>
    public void Clear()
    {
        Visibility = Visibility.Collapsed;
        errorList.ItemsSource = null;
        singleMessage.Text = "";
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        Clear();
    }

    private void ApplyStyle(ValidationStyle style)
    {
        switch (style)
        {
            case ValidationStyle.Error:
                panelBorder.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA));
                statusIcon.Text = "⚠";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                headerText.Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C));
                break;

            case ValidationStyle.Success:
                panelBorder.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBB, 0xF7, 0xD0));
                statusIcon.Text = "✓";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x65, 0x34));
                break;

            case ValidationStyle.Warning:
                panelBorder.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFD, 0xE6, 0x8A));
                statusIcon.Text = "⚠";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E));
                break;

            case ValidationStyle.Info:
                panelBorder.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF));
                panelBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBF, 0xDB, 0xFE));
                statusIcon.Text = "ℹ";
                statusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
                singleMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0xAF));
                break;
        }
    }

    private enum ValidationStyle
    {
        Error,
        Success,
        Warning,
        Info
    }
}
