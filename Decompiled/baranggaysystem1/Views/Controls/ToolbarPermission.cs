using System.Windows;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Provides an attached property to associate a permission key with a toolbar action button.
/// When set, the FullscreenViewHost will check Permissions.Has(permissionKey) to determine
/// whether the button should be visible.
/// 
/// Requirements: 10.3
/// 
/// Usage:
///   var saveButton = new Button { Content = "Save" };
///   ToolbarPermission.SetPermissionKey(saveButton, "residents.create");
/// </summary>
public static class ToolbarPermission
{
    /// <summary>
    /// Identifies the PermissionKey attached dependency property.
    /// When set on a toolbar UIElement, the FullscreenViewHost will only display
    /// the element if Permissions.Has(permissionKey) returns true.
    /// If not set (null or empty), the element is always visible (no permission check).
    /// </summary>
    public static readonly DependencyProperty PermissionKeyProperty =
        DependencyProperty.RegisterAttached(
            "PermissionKey",
            typeof(string),
            typeof(ToolbarPermission),
            new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Gets the permission key associated with the specified element.
    /// </summary>
    public static string? GetPermissionKey(DependencyObject element)
    {
        return (string?)element.GetValue(PermissionKeyProperty);
    }

    /// <summary>
    /// Sets the permission key on the specified element.
    /// </summary>
    public static void SetPermissionKey(DependencyObject element, string? value)
    {
        element.SetValue(PermissionKeyProperty, value);
    }
}
