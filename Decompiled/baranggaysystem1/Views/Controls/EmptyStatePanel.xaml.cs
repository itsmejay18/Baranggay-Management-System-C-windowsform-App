using System;
using System.Windows;
using System.Windows.Controls;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Reusable empty state panel for lists/grids with no data.
/// Provides consistent empty state UX across all modules.
/// </summary>
public partial class EmptyStatePanel : UserControl
{
    public event RoutedEventHandler? ActionClick;

    public EmptyStatePanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Set the FontAwesome icon.
    /// </summary>
    public IconChar Icon
    {
        get => ((IconBlockBase<IconChar>)(object)iconBlock).Icon;
        set => ((IconBlockBase<IconChar>)(object)iconBlock).Icon = value;
    }

    /// <summary>
    /// Set the title text.
    /// </summary>
    public string Title
    {
        get => titleText.Text;
        set => titleText.Text = value ?? "No Data";
    }

    /// <summary>
    /// Set the description text.
    /// </summary>
    public string Description
    {
        get => descriptionText.Text;
        set => descriptionText.Text = value ?? "";
    }

    /// <summary>
    /// Set the action button text. Setting this makes the button visible.
    /// </summary>
    public string ActionText
    {
        get => actionButton.Content?.ToString() ?? "";
        set
        {
            actionButton.Content = value;
            actionButton.Visibility = string.IsNullOrWhiteSpace(value)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    /// <summary>
    /// Set a secondary hint below the action button.
    /// </summary>
    public string Hint
    {
        get => hintText.Text;
        set
        {
            hintText.Text = value ?? "";
            hintText.Visibility = string.IsNullOrWhiteSpace(value)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        ActionClick?.Invoke(this, e);
    }
}
