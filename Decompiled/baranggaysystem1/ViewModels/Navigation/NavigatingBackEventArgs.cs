using System;

namespace baranggaysystem1.ViewModels.Navigation;

/// <summary>
/// Event arguments raised when a fullscreen view is about to navigate back to the origin page.
/// Allows subscribers to cancel the navigation or request a data refresh on return.
/// </summary>
public class NavigatingBackEventArgs : EventArgs
{
    /// <summary>
    /// Set to true to cancel the back navigation.
    /// </summary>
    public bool Cancel { get; set; } = false;

    /// <summary>
    /// Indicates whether the current form has unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges { get; }

    /// <summary>
    /// The route key of the originating page to navigate back to.
    /// </summary>
    public string OriginRoute { get; }

    /// <summary>
    /// Set to true to trigger a data refresh on the origin page after returning.
    /// </summary>
    public bool RefreshOnReturn { get; set; } = false;

    /// <summary>
    /// Creates a new instance of NavigatingBackEventArgs.
    /// </summary>
    /// <param name="originRoute">The route key of the originating page.</param>
    /// <param name="hasUnsavedChanges">Whether the form has unsaved changes.</param>
    /// <exception cref="ArgumentNullException">Thrown when originRoute is null.</exception>
    public NavigatingBackEventArgs(string originRoute, bool hasUnsavedChanges)
    {
        OriginRoute = originRoute ?? throw new ArgumentNullException(nameof(originRoute));
        HasUnsavedChanges = hasUnsavedChanges;
    }
}
