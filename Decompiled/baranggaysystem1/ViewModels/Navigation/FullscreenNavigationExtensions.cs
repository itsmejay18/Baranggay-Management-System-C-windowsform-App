using System;
using System.Windows;
using baranggaysystem1.helper;
using baranggaysystem1.Views;
using baranggaysystem1.Views.Controls;

namespace baranggaysystem1.ViewModels.Navigation;

/// <summary>
/// Extension methods on <see cref="NavigationService"/> to simplify fullscreen view
/// navigation from any module page.
///
/// Requirements: 1.1, 1.2, 1.3, 1.6, 4.3
/// </summary>
public static class FullscreenNavigationExtensions
{
    /// <summary>
    /// Stores the OnSaved callback for the currently active fullscreen view.
    /// Cleared when navigating back from the fullscreen view.
    /// </summary>
    private static Action? _currentOnSavedCallback;

    /// <summary>
    /// Navigates from the current module page to a fullscreen data table view.
    /// Validates the config, creates and configures a FullscreenViewHost,
    /// pushes a navigation history entry for breadcrumb support, wires the
    /// BackCompleted event, and stores the OnSaved callback.
    /// </summary>
    /// <param name="nav">The NavigationService instance.</param>
    /// <param name="config">Configuration describing the fullscreen view to display.</param>
    /// <exception cref="ArgumentException">Thrown when Title or OriginRoute is null/empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when Content is null.</exception>
    public static void NavigateToFullscreen(this NavigationService nav, FullscreenViewConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Step 1: Validate configuration (throws on invalid state)
        config.Validate();

        // Step 2: Create the fullscreen host container
        var host = new FullscreenViewHost
        {
            ViewTitle = config.Title,
            ViewSubtitle = config.Subtitle ?? string.Empty,
            OriginRoute = config.OriginRoute,
            ContentArea = config.Content,
            ShowSideToolbar = config.ShowSideToolbar
        };

        // Step 3: Set optional icon
        if (config.Icon.HasValue)
        {
            host.ViewIcon = config.Icon.Value;
        }

        // Step 4: Add toolbar items
        if (config.ToolbarItems != null)
        {
            foreach (var item in config.ToolbarItems)
            {
                host.ToolbarItems.Add(item);
            }
        }

        // Step 5: Store OnSaved callback for post-save invocation
        _currentOnSavedCallback = config.OnSaved;

        // Step 5b: Protect the origin route from LRU cache eviction during active fullscreen session
        // Requirement 7.7: Fullscreen view origin pages are not evicted during active sessions
        nav.ProtectRoute(config.OriginRoute);

        // Step 6: Wire BackCompleted event to NavigateBackFromFullscreen
        host.BackCompleted += (sender, e) =>
        {
            nav.NavigateBackFromFullscreen(config.OriginRoute, refreshOnReturn: false);
        };

        // Step 7: Push navigation history entry for breadcrumb support
        string fullscreenRoute = $"Fullscreen:{config.OriginRoute}:{config.Title}";
        UxEnhancementsIntegration.RecordNavigation(fullscreenRoute, config.Title);

        // Step 8: Update breadcrumb display for fullscreen view
        // Requirement 4.1: Set breadcrumb to "OriginTitle › ViewTitle"
        // Requirement 4.4: Truncate ViewTitle with ellipsis if exceeding 50 characters
        // Requirement 4.5: Limit breadcrumb depth to two segments for nested fullscreen views
        var mainWindow = System.Windows.Application.Current?.MainWindow as MainWindow;
        mainWindow?.UpdateBreadcrumbForFullscreen(config.OriginRoute, config.Title);

        // Step 9: Navigate to the fullscreen host
        nav.NavigateTo(host);
    }

    /// <summary>
    /// Returns from a fullscreen view to the originating module page.
    /// Navigates back to the origin route using MainWindow.NavigatePage which
    /// retrieves the cached page via GetOrCreate (preserving DataGrid scroll position,
    /// row selection state, and active filter state). If the page is no longer cached,
    /// GetOrCreate recreates it via the factory. Optionally triggers data refresh
    /// via IRefreshable and invokes the stored OnSaved callback.
    /// </summary>
    /// <param name="nav">The NavigationService instance.</param>
    /// <param name="originRoute">The route key of the originating module page.</param>
    /// <param name="refreshOnReturn">If true, invokes RefreshData() on the restored page and the OnSaved callback.</param>
    /// <remarks>
    /// Requirements: 2.2, 2.4, 2.5, 2.6
    /// - 2.2: Navigate back to Origin_Route within 500ms
    /// - 2.4: Restore cached Module_Page preserving scroll, selection, and filter state
    /// - 2.5: Invoke IRefreshable.RefreshData() when refreshOnReturn is true
    /// - 2.6: Recreate page via GetOrCreate factory on cache miss
    /// </remarks>
    public static void NavigateBackFromFullscreen(
        this NavigationService nav,
        string originRoute,
        bool refreshOnReturn = false)
    {
        if (string.IsNullOrWhiteSpace(originRoute))
        {
            return;
        }

        // Remove eviction protection since the fullscreen session is ending
        // Requirement 7.7: Protection only applies during active fullscreen sessions
        nav.UnprotectRoute(originRoute);

        // Step 1: Navigate back to the origin route via MainWindow.NavigatePage.
        // Bypass the unsaved changes guard since the back button already confirmed
        // the discard with the user (Requirements 3.1-3.4 handled by FullscreenViewHost.NavigateBack).
        nav.BypassUnsavedChangesGuard();

        try
        {
            // NavigatePage internally calls NavigationService.GetOrCreate(route, factory) which:
            //   - Returns the cached page instance if available (Req 2.4: preserves state)
            //   - Recreates the page via factory if cache miss (Req 2.6: handles cache miss)
            // It also updates breadcrumb, shell chrome, and navigation selection.
            var mainWindow = Application.Current?.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                return;
            }

            mainWindow.NavigatePage(originRoute);
        }
        finally
        {
            nav.ResetBypassFlag();
        }

        // Step 2: If refresh requested, trigger data reload on the restored module page.
        // After NavigatePage completes, NavigationService.CurrentView holds the restored page.
        if (refreshOnReturn)
        {
            // Req 2.5: Invoke IRefreshable.RefreshData() on the restored page
            if (nav.CurrentView is IRefreshable refreshable)
            {
                refreshable.RefreshData();
            }

            // Invoke the stored OnSaved callback to notify the origin page of saved data
            _currentOnSavedCallback?.Invoke();
        }

        // Step 3: Clear the stored callback to prevent stale references
        _currentOnSavedCallback = null;
    }

    /// <summary>
    /// Invokes the stored OnSaved callback without navigating back.
    /// Useful when a save completes but the user wants to remain on the fullscreen view.
    /// </summary>
    public static void InvokeOnSavedCallback()
    {
        _currentOnSavedCallback?.Invoke();
    }
}
