using System.Windows;
using System.Windows.Controls;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views;

/// <summary>
/// Shared helper for creating styled toolbar buttons used in fullscreen view toolbars.
/// Provides consistent button styling across all module pages.
/// </summary>
public static class FullscreenToolbarHelper
{
    /// <summary>
    /// Creates a styled toolbar button for use in fullscreen view toolbars.
    /// Each button has an icon, label text, proper accessibility name, and click handler.
    /// </summary>
    /// <param name="label">The visible text label for the button.</param>
    /// <param name="icon">The FontAwesome icon to display.</param>
    /// <param name="clickHandler">The click event handler.</param>
    /// <returns>A configured Button element ready for toolbar use.</returns>
    public static Button CreateToolbarButton(string label, IconChar icon, RoutedEventHandler clickHandler)
    {
        var iconBlock = new IconBlock
        {
            Icon = icon,
            FontSize = 14,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        panel.Children.Add(iconBlock);
        panel.Children.Add(textBlock);

        var button = new Button
        {
            Content = panel,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 4, 0),
            MinHeight = 32,
            Cursor = System.Windows.Input.Cursors.Hand
        };

        // Set accessibility name (Requirement 5.4)
        System.Windows.Automation.AutomationProperties.SetName(button, label);

        button.Click += clickHandler;
        return button;
    }
}
