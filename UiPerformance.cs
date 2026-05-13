using System;
using System.Reflection;
using System.Windows.Forms;

namespace baranggaysystem1;

internal static class UiPerformance
{
    private static readonly PropertyInfo? DoubleBufferedProperty =
        typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void EnableDoubleBuffering(Control root)
    {
        if (root == null)
        {
            return;
        }

        ApplyRecursive(root);
    }

    private static void ApplyRecursive(Control control)
    {
        TryEnable(control);
        foreach (Control child in control.Controls)
        {
            ApplyRecursive(child);
        }
    }

    private static void TryEnable(Control control)
    {
        if (DoubleBufferedProperty == null)
        {
            return;
        }

        try
        {
            DoubleBufferedProperty.SetValue(control, true, null);
        }
        catch
        {
            // Ignore controls that reject this flag.
        }
    }
}
