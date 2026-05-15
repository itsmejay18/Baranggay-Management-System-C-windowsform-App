using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FontAwesome.Sharp;
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels.Navigation;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Reusable container UserControl that provides consistent fullscreen view chrome:
/// back button, title area, action toolbar (top or side), and a content presenter
/// for module-specific forms.
/// 
/// Requirements: 1.5, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 7.4, 7.5, 7.6
/// </summary>
public partial class FullscreenViewHost : UserControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the ViewTitle dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewTitleProperty =
        DependencyProperty.Register(
            nameof(ViewTitle),
            typeof(string),
            typeof(FullscreenViewHost),
            new PropertyMetadata(string.Empty, OnViewTitleChanged));

    /// <summary>
    /// Identifies the ViewSubtitle dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewSubtitleProperty =
        DependencyProperty.Register(
            nameof(ViewSubtitle),
            typeof(string),
            typeof(FullscreenViewHost),
            new PropertyMetadata(string.Empty, OnViewSubtitleChanged));

    /// <summary>
    /// Identifies the OriginRoute dependency property.
    /// </summary>
    public static readonly DependencyProperty OriginRouteProperty =
        DependencyProperty.Register(
            nameof(OriginRoute),
            typeof(string),
            typeof(FullscreenViewHost),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the ContentArea dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentAreaProperty =
        DependencyProperty.Register(
            nameof(ContentArea),
            typeof(UIElement),
            typeof(FullscreenViewHost),
            new PropertyMetadata(null, OnContentAreaChanged));

    /// <summary>
    /// Identifies the ToolbarItems dependency property.
    /// </summary>
    public static readonly DependencyProperty ToolbarItemsProperty =
        DependencyProperty.Register(
            nameof(ToolbarItems),
            typeof(IList<UIElement>),
            typeof(FullscreenViewHost),
            new PropertyMetadata(null, OnToolbarItemsChanged));

    /// <summary>
    /// Identifies the ShowSideToolbar dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowSideToolbarProperty =
        DependencyProperty.Register(
            nameof(ShowSideToolbar),
            typeof(bool),
            typeof(FullscreenViewHost),
            new PropertyMetadata(false, OnShowSideToolbarChanged));

    #endregion

    #region Events

    /// <summary>
    /// Raised when back navigation is about to occur. Subscribers can cancel via the event args.
    /// </summary>
    public event EventHandler<NavigatingBackEventArgs>? NavigatingBack;

    /// <summary>
    /// Raised after back navigation has completed successfully.
    /// </summary>
    public event EventHandler? BackCompleted;

    #endregion

    #region Private State

    private bool _hasUnsavedChanges;

    /// <summary>
    /// Cancellation token source for the async content loading operation.
    /// Cancelled on timeout (30s) or when the user navigates away.
    /// </summary>
    private CancellationTokenSource? _loadingCts;

    /// <summary>
    /// Timeout duration for async data loading (Requirement 7.6: 30 seconds).
    /// </summary>
    private static readonly TimeSpan LoadingTimeout = TimeSpan.FromSeconds(30);

    #endregion

    public FullscreenViewHost()
    {
        InitializeComponent();
        ToolbarItems = new List<UIElement>();
        Loaded += FullscreenViewHost_Loaded;
        Unloaded += FullscreenViewHost_Unloaded;
    }

    #region CLR Property Wrappers

    /// <summary>
    /// The title displayed in the header area.
    /// </summary>
    public string ViewTitle
    {
        get => (string)GetValue(ViewTitleProperty);
        set => SetValue(ViewTitleProperty, value);
    }

    /// <summary>
    /// Optional subtitle displayed after the title. Collapses when null or empty.
    /// </summary>
    public string ViewSubtitle
    {
        get => (string)GetValue(ViewSubtitleProperty);
        set => SetValue(ViewSubtitleProperty, value);
    }

    /// <summary>
    /// The route key of the originating page (for back navigation).
    /// </summary>
    public string OriginRoute
    {
        get => (string)GetValue(OriginRouteProperty);
        set => SetValue(OriginRouteProperty, value);
    }

    /// <summary>
    /// The content UIElement displayed in the main content area.
    /// </summary>
    public UIElement? ContentArea
    {
        get => (UIElement?)GetValue(ContentAreaProperty);
        set => SetValue(ContentAreaProperty, value);
    }

    /// <summary>
    /// Collection of toolbar action buttons. Added to top or side toolbar based on ShowSideToolbar.
    /// </summary>
    public IList<UIElement> ToolbarItems
    {
        get => (IList<UIElement>)GetValue(ToolbarItemsProperty);
        set => SetValue(ToolbarItemsProperty, value);
    }

    /// <summary>
    /// When true, toolbar items are rendered in a vertical side panel.
    /// When false (default), toolbar items are rendered in the horizontal top bar.
    /// </summary>
    public bool ShowSideToolbar
    {
        get => (bool)GetValue(ShowSideToolbarProperty);
        set => SetValue(ShowSideToolbarProperty, value);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initiates back navigation with unsaved changes guard.
    /// Raises NavigatingBack event (with cancel support), checks for dirty form state,
    /// and if confirmed or no dirty state, raises BackCompleted event.
    /// Requirements: 2.2, 2.3, 3.1, 3.2, 3.3, 3.4
    /// </summary>
    public void NavigateBack()
    {
        // Determine if there are unsaved changes
        bool hasDirtyContent = _hasUnsavedChanges ||
            (ContentArea is FullscreenFormBase form && form.IsDirty);

        // Step 1: Raise NavigatingBack event with cancel support
        var args = new NavigatingBackEventArgs(
            OriginRoute ?? string.Empty,
            hasDirtyContent);

        NavigatingBack?.Invoke(this, args);

        // If a subscriber cancelled the navigation, stop here
        if (args.Cancel)
        {
            return;
        }

        // Step 2: If content is dirty, show confirmation dialog
        if (hasDirtyContent)
        {
            if (ContentArea is FullscreenFormBase dirtyForm)
            {
                // Use the form's built-in ConfirmDiscard which shows the dialog
                bool shouldProceed = dirtyForm.ConfirmDiscard();
                if (!shouldProceed)
                {
                    return;
                }

                // Requirement 3.3: Reset Dirty_State on discard
                dirtyForm.IsDirty = false;
            }
            else
            {
                // Generic unsaved changes confirmation for non-FullscreenFormBase content
                var owner = Window.GetWindow(this);
                bool confirmed = ConfirmationDialog.Show(
                    owner,
                    "Unsaved Changes",
                    "You have unsaved changes. Are you sure you want to go back? Your changes will be lost.",
                    "Discard Changes",
                    "Keep Editing",
                    ConfirmationType.Warning);

                if (!confirmed)
                {
                    return;
                }

                // Reset host-level unsaved changes flag
                _hasUnsavedChanges = false;
            }
        }

        // Step 3: Navigation confirmed — raise BackCompleted
        BackCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the unsaved changes flag for the view host.
    /// This is used when the content panel does not inherit from FullscreenFormBase
    /// but still needs to track dirty state.
    /// </summary>
    /// <param name="hasChanges">True if there are unsaved changes; false otherwise.</param>
    public void SetUnsavedChanges(bool hasChanges)
    {
        _hasUnsavedChanges = hasChanges;
    }

    /// <summary>
    /// Gets the current unsaved changes flag value.
    /// Used by NavigationService.GuardUnsavedChanges() to check host-level dirty state
    /// for non-FullscreenFormBase content.
    /// </summary>
    public bool HasUnsavedChangesFlag => _hasUnsavedChanges;

    /// <summary>
    /// Sets the optional FontAwesome icon in the header. Pass null to collapse the icon area.
    /// </summary>
    public IconChar? ViewIcon
    {
        set
        {
            if (value.HasValue)
            {
                viewIcon.Icon = value.Value;
                iconArea.Visibility = Visibility.Visible;
            }
            else
            {
                iconArea.Visibility = Visibility.Collapsed;
            }
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Back button click handler — delegates to NavigateBack().
    /// </summary>
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateBack();
    }

    /// <summary>
    /// Loaded event handler — renders toolbar items with permission filtering
    /// and triggers async content loading if the content supports it.
    /// Requirements: 7.4, 7.5
    /// </summary>
    private void FullscreenViewHost_Loaded(object sender, RoutedEventArgs e)
    {
        RenderToolbarItems();

        // Requirement 7.5: Load Content_Panel data asynchronously after view transition completes.
        // Check if the content implements IAsyncContentLoader for async data loading.
        if (ContentArea is IAsyncContentLoader asyncLoader)
        {
            _ = LoadContentAsync(asyncLoader);
        }
        else
        {
            // Requirement 7.4: Enable UI virtualization on DataGrids even without async loading.
            EnableDataGridVirtualization();
        }
    }

    /// <summary>
    /// Unloaded event handler — cancels any in-progress async loading operations.
    /// </summary>
    private void FullscreenViewHost_Unloaded(object sender, RoutedEventArgs e)
    {
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = null;
    }

    /// <summary>
    /// Retry button click handler — retries the async content loading operation
    /// after a previous failure or timeout.
    /// Requirement 7.6: Display an error message with an option to retry.
    /// </summary>
    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        // Hide the error panel
        errorPanel.Visibility = Visibility.Collapsed;

        // Trigger async content reload if the content supports IAsyncContentLoader
        if (ContentArea is IAsyncContentLoader asyncLoader)
        {
            _ = LoadContentAsync(asyncLoader);
        }
        else
        {
            // Fallback: show generic loading and attempt reload
            _ = LoadContentAsync(null);
        }
    }

    /// <summary>
    /// Loads content data asynchronously with timeout support.
    /// Requirement 7.4: Load Content_Panel data asynchronously after view transition.
    /// Requirement 7.5: Display LoadingOverlay during data fetch.
    /// Requirement 7.6: Hide LoadingOverlay and show error with retry on failure/timeout (30s).
    /// </summary>
    /// <param name="asyncLoader">
    /// The IAsyncContentLoader implementation to invoke, or null for a no-op load
    /// (used as fallback when content doesn't implement the interface).
    /// </param>
    private async Task LoadContentAsync(IAsyncContentLoader? asyncLoader)
    {
        // Cancel any previous loading operation
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = new CancellationTokenSource();

        var cts = _loadingCts;

        try
        {
            // Requirement 7.6: Set 30-second timeout
            cts.CancelAfter(LoadingTimeout);

            // Requirement 7.5: Show LoadingOverlay during data fetch
            string message = asyncLoader?.LoadingMessage ?? "Loading...";
            loadingOverlay.Show(message);

            if (asyncLoader != null)
            {
                // Invoke the content panel's async data loading
                await asyncLoader.LoadContentAsync(cts.Token);
            }
            else
            {
                // No-op: content doesn't support async loading, just yield briefly
                await Task.Delay(1, cts.Token);
            }

            // Success: hide loading overlay
            loadingOverlay.Hide();
            errorPanel.Visibility = Visibility.Collapsed;

            // Requirement 7.4: Enable UI virtualization after data is loaded
            EnableDataGridVirtualization();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Timeout or user navigated away — show error with retry option
            loadingOverlay.Hide();

            // Only show error if the control is still loaded (user didn't navigate away)
            if (IsLoaded)
            {
                errorMessageText.Text = "The data loading operation timed out after 30 seconds. Please check your connection and try again.";
                errorPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            // Load failure — show error with retry
            loadingOverlay.Hide();

            if (IsLoaded)
            {
                errorMessageText.Text = $"An error occurred while loading data: {ex.Message}";
                errorPanel.Visibility = Visibility.Visible;
            }

            helper.AppLogger.LogWarning($"Content loading failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Enables UI virtualization on DataGrids within the content area that have more than 100 rows.
    /// Requirement 7.4: Enable UI virtualization on DataGrids with more than 100 rows.
    /// 
    /// This method traverses the visual tree of the ContentArea to find DataGrid controls
    /// and ensures VirtualizingPanel properties are set for optimal performance.
    /// </summary>
    private void EnableDataGridVirtualization()
    {
        if (ContentArea == null) return;

        // Find all DataGrids in the content area's visual tree
        var dataGrids = FindVisualChildren<DataGrid>(ContentArea);
        foreach (var dataGrid in dataGrids)
        {
            // Enable virtualization properties for performance with large datasets
            VirtualizingPanel.SetIsVirtualizing(dataGrid, true);
            VirtualizingPanel.SetVirtualizationMode(dataGrid, VirtualizationMode.Recycling);
            VirtualizingPanel.SetScrollUnit(dataGrid, ScrollUnit.Pixel);

            // Enable container recycling for better memory usage
            dataGrid.EnableRowVirtualization = true;
            dataGrid.EnableColumnVirtualization = true;
        }
    }

    /// <summary>
    /// Recursively finds all visual children of a given type within a DependencyObject.
    /// Used by EnableDataGridVirtualization to locate DataGrid controls in the content tree.
    /// </summary>
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;

        int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    #endregion

    #region Permission-Based Toolbar Filtering

    /// <summary>
    /// Renders toolbar items into the appropriate panel (top or side), filtering out
    /// items for which the user lacks the corresponding permission.
    /// Shows a read-only indicator when all toolbar actions are hidden.
    /// 
    /// Requirements: 10.3, 10.4
    /// </summary>
    public void RenderToolbarItems()
    {
        // Clear existing toolbar children
        topToolbarPanel.Children.Clear();
        sideToolbarPanel.Children.Clear();

        var items = ToolbarItems;
        if (items == null || items.Count == 0)
        {
            // No toolbar items configured — show read-only indicator
            readOnlyIndicator.Visibility = Visibility.Visible;
            return;
        }

        int visibleCount = 0;

        foreach (var item in items)
        {
            // Check if this item has a permission key attached
            string? permissionKey = ToolbarPermission.GetPermissionKey(item);

            // If a permission key is set, check if the user has that permission
            if (!string.IsNullOrEmpty(permissionKey) && !Permissions.Has(permissionKey))
            {
                // User lacks permission — hide this button
                continue;
            }

            // User has permission (or no permission key was set) — add to toolbar
            var targetPanel = ShowSideToolbar ? sideToolbarPanel : topToolbarPanel;
            targetPanel.Children.Add(item);
            visibleCount++;
        }

        // Show read-only indicator when all toolbar actions are hidden due to permissions
        if (visibleCount == 0)
        {
            readOnlyIndicator.Visibility = Visibility.Visible;
            topToolbarPanel.Visibility = Visibility.Collapsed;
            sideToolbarBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            readOnlyIndicator.Visibility = Visibility.Collapsed;
            // Restore toolbar visibility based on ShowSideToolbar setting
            if (ShowSideToolbar)
            {
                sideToolbarBorder.Visibility = Visibility.Visible;
                topToolbarPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                topToolbarPanel.Visibility = Visibility.Visible;
                sideToolbarBorder.Visibility = Visibility.Collapsed;
            }
        }
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnViewTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FullscreenViewHost host)
        {
            host.titleText.Text = (e.NewValue as string) ?? string.Empty;
        }
    }

    private static void OnViewSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FullscreenViewHost host)
        {
            var subtitle = (e.NewValue as string) ?? string.Empty;
            host.subtitleText.Text = subtitle;
            host.subtitleText.Visibility = string.IsNullOrWhiteSpace(subtitle)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private static void OnContentAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FullscreenViewHost host)
        {
            host.contentPresenter.Content = e.NewValue as UIElement;
        }
    }

    private static void OnShowSideToolbarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FullscreenViewHost host)
        {
            bool showSide = (bool)e.NewValue;
            host.sideToolbarBorder.Visibility = showSide ? Visibility.Visible : Visibility.Collapsed;
            host.topToolbarPanel.Visibility = showSide ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static void OnToolbarItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FullscreenViewHost host && host.IsLoaded)
        {
            host.RenderToolbarItems();
        }
    }

    #endregion
}
