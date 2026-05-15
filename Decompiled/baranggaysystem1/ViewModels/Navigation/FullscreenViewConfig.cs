using System;
using System.Collections.Generic;
using System.Windows;
using FontAwesome.Sharp;

namespace baranggaysystem1.ViewModels.Navigation;

/// <summary>
/// Configuration object for navigating to a fullscreen data table view.
/// Contains all the information needed to construct and display a FullscreenViewHost.
/// </summary>
public class FullscreenViewConfig
{
    /// <summary>
    /// Display title shown in the fullscreen view header.
    /// Must be non-empty for validation to pass.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional subtitle/description below the title.
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// The route key of the originating page (for back navigation).
    /// Must be non-empty for validation to pass.
    /// </summary>
    public string OriginRoute { get; set; } = string.Empty;

    /// <summary>
    /// The content UserControl to display in the fullscreen area.
    /// Must be non-null for validation to pass.
    /// </summary>
    public UIElement? Content { get; set; }

    /// <summary>
    /// Action buttons to display in the toolbar area.
    /// </summary>
    public IList<UIElement> ToolbarItems { get; set; } = new List<UIElement>();

    /// <summary>
    /// If true, toolbar is rendered as a vertical side panel. Otherwise, horizontal top bar.
    /// </summary>
    public bool ShowSideToolbar { get; set; } = false;

    /// <summary>
    /// Callback invoked when data is saved, allowing the origin page to refresh.
    /// </summary>
    public Action? OnSaved { get; set; }

    /// <summary>
    /// Optional icon (FontAwesome) for the view header.
    /// </summary>
    public IconChar? Icon { get; set; }

    /// <summary>
    /// Validates the configuration, throwing exceptions for invalid state.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when Title is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when Content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when OriginRoute is null or empty.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new ArgumentException("Title is required and must not be empty.", nameof(Title));
        }

        if (Content is null)
        {
            throw new ArgumentNullException(nameof(Content), "Content must not be null.");
        }

        if (string.IsNullOrWhiteSpace(OriginRoute))
        {
            throw new ArgumentException("OriginRoute is required and must not be empty.", nameof(OriginRoute));
        }
    }
}
