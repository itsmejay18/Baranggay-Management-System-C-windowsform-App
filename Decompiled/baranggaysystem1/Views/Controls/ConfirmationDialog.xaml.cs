using System.Windows;
using System.Windows.Media;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Modern confirmation dialog replacing MessageBox for destructive actions.
/// Provides consistent styling, clear action labels, and better UX.
/// </summary>
public partial class ConfirmationDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Show a confirmation dialog and return whether the user confirmed.
    /// </summary>
    public static bool Show(Window? owner, string title, string message,
        string confirmText = "Confirm", string cancelText = "Cancel",
        ConfirmationType type = ConfirmationType.Warning)
    {
        var dialog = new ConfirmationDialog();
        if (owner != null) dialog.Owner = owner;

        dialog.titleText.Text = title;
        dialog.messageText.Text = message;
        dialog.confirmButton.Content = confirmText;
        dialog.cancelButton.Content = cancelText;

        dialog.ApplyType(type);
        dialog.ShowDialog();

        return dialog.Confirmed;
    }

    /// <summary>
    /// Show a delete confirmation with red styling.
    /// </summary>
    public static bool ShowDelete(Window? owner, string itemName)
    {
        return Show(owner,
            $"Delete {itemName}?",
            $"This will permanently remove this {itemName.ToLower()}. This action cannot be undone.",
            "Delete",
            "Keep",
            ConfirmationType.Danger);
    }

    /// <summary>
    /// Show an archive confirmation.
    /// </summary>
    public static bool ShowArchive(Window? owner, string itemName, int count = 1)
    {
        string msg = count > 1
            ? $"Are you sure you want to archive {count} {itemName.ToLower()}(s)? They can be restored later."
            : $"Are you sure you want to archive this {itemName.ToLower()}? It can be restored later.";

        return Show(owner, $"Archive {itemName}?", msg, "Archive", "Cancel", ConfirmationType.Warning);
    }

    /// <summary>
    /// Show a generic "are you sure" confirmation.
    /// </summary>
    public static bool ShowConfirm(Window? owner, string title, string message)
    {
        return Show(owner, title, message, "Yes, proceed", "Cancel", ConfirmationType.Info);
    }

    private void ApplyType(ConfirmationType type)
    {
        switch (type)
        {
            case ConfirmationType.Danger:
                iconCircle.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
                iconText.Text = "🗑️";
                confirmButton.Background = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                break;

            case ConfirmationType.Warning:
                iconCircle.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB));
                iconText.Text = "⚠️";
                confirmButton.Background = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
                break;

            case ConfirmationType.Info:
                iconCircle.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF));
                iconText.Text = "ℹ️";
                confirmButton.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
                break;

            case ConfirmationType.Success:
                iconCircle.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
                iconText.Text = "✓";
                confirmButton.Background = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
                break;
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// Confirmation dialog type (affects icon and button color).
/// </summary>
public enum ConfirmationType
{
    Info,
    Warning,
    Danger,
    Success
}
