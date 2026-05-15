namespace baranggaysystem1.ViewModels;

/// <summary>
/// Interface for module pages that need to refresh their data
/// when returning from a fullscreen view with changes saved.
/// </summary>
/// <remarks>
/// Implementing pages should reload their DataGrid items source,
/// recalculate metric counters, clear selection state, and update
/// empty state visibility based on record count.
/// </remarks>
public interface IRefreshable
{
    /// <summary>
    /// Refreshes the page data after returning from a fullscreen view.
    /// Called by NavigationService when back navigation completes with
    /// refreshOnReturn set to true.
    /// </summary>
    void RefreshData();
}
