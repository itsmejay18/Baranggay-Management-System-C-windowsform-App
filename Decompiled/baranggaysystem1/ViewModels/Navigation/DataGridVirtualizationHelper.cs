using System.Windows;
using System.Windows.Controls;

namespace baranggaysystem1.ViewModels.Navigation;

/// <summary>
/// Helper class for enabling UI virtualization on DataGrid controls.
/// When a DataGrid is expected to display more than 100 rows, virtualization
/// should be enabled so that only visible rows are rendered in memory.
///
/// Requirement 7.4: Enable UI virtualization on DataGrids with more than 100 rows.
///
/// Usage:
///   DataGridVirtualizationHelper.EnableVirtualization(myDataGrid);
///   
/// Or in XAML via attached property:
///   &lt;DataGrid local:DataGridVirtualizationHelper.AutoVirtualize="True" /&gt;
/// </summary>
public static class DataGridVirtualizationHelper
{
    /// <summary>
    /// The row count threshold above which virtualization is automatically enabled.
    /// </summary>
    public const int VirtualizationThreshold = 100;

    #region AutoVirtualize Attached Property

    /// <summary>
    /// Attached property that enables automatic virtualization on a DataGrid.
    /// When set to true, the DataGrid's virtualization settings are configured
    /// for optimal performance with large datasets.
    /// </summary>
    public static readonly DependencyProperty AutoVirtualizeProperty =
        DependencyProperty.RegisterAttached(
            "AutoVirtualize",
            typeof(bool),
            typeof(DataGridVirtualizationHelper),
            new PropertyMetadata(false, OnAutoVirtualizeChanged));

    public static bool GetAutoVirtualize(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoVirtualizeProperty);
    }

    public static void SetAutoVirtualize(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoVirtualizeProperty, value);
    }

    private static void OnAutoVirtualizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid dataGrid && (bool)e.NewValue)
        {
            EnableVirtualization(dataGrid);
        }
    }

    #endregion

    /// <summary>
    /// Enables UI virtualization on the specified DataGrid for optimal performance
    /// with large datasets (100+ rows). Configures:
    /// - VirtualizingPanel.IsVirtualizing = true
    /// - VirtualizingPanel.VirtualizationMode = Recycling (reuses containers)
    /// - ScrollViewer.CanContentScroll = true (item-based scrolling for virtualization)
    /// - EnableRowVirtualization = true
    /// - EnableColumnVirtualization = true (for wide grids)
    /// </summary>
    /// <param name="dataGrid">The DataGrid to configure for virtualization.</param>
    public static void EnableVirtualization(DataGrid dataGrid)
    {
        if (dataGrid == null) return;

        // Enable row virtualization (only visible rows are rendered)
        VirtualizingPanel.SetIsVirtualizing(dataGrid, true);
        VirtualizingPanel.SetVirtualizationMode(dataGrid, VirtualizationMode.Recycling);

        // Enable item-based scrolling (required for virtualization to work)
        ScrollViewer.SetCanContentScroll(dataGrid, true);

        // Enable DataGrid-specific virtualization properties
        dataGrid.EnableRowVirtualization = true;
        dataGrid.EnableColumnVirtualization = true;
    }

    /// <summary>
    /// Conditionally enables virtualization based on the expected row count.
    /// Only applies virtualization settings when the row count exceeds the threshold (100).
    /// </summary>
    /// <param name="dataGrid">The DataGrid to configure.</param>
    /// <param name="expectedRowCount">The expected number of rows to be displayed.</param>
    public static void EnableVirtualizationIfNeeded(DataGrid dataGrid, int expectedRowCount)
    {
        if (dataGrid == null) return;

        if (expectedRowCount > VirtualizationThreshold)
        {
            EnableVirtualization(dataGrid);
        }
    }
}
