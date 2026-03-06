using System;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal static class ControllerDialogs
{
    public static void Error(string message, string title = "Error", Exception? ex = null)
    {
        AppLogger.LogError($"{title}: {message}", ex);
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void Error(Exception ex, string message, string title = "Error")
        => Error(message, title, ex);

    public static void Warning(string message, string title = "Warning", Exception? ex = null)
    {
        AppLogger.LogWarning($"{title}: {message}", ex);
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void Warning(Exception ex, string message, string title = "Warning")
        => Warning(message, title, ex);

    public static void Info(string message, string title = "Info")
    {
        AppLogger.LogInfo($"{title}: {message}");
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static DialogResult Confirm(string message, string title = "Confirm",
        MessageBoxIcon icon = MessageBoxIcon.Question)
        => MessageBox.Show(message, title, MessageBoxButtons.YesNo, icon);

    public static string? Prompt(string message, string title = "Input", string defaultValue = "")
    {
        using var dialog = new Form
        {
            Width = 460,
            Height = 175,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            Text = title
        };

        var label = new Label
        {
            Left = 12,
            Top = 12,
            Width = 420,
            AutoSize = false,
            Height = 26,
            Text = message
        };

        var textBox = new TextBox
        {
            Left = 12,
            Top = 44,
            Width = 420,
            Text = defaultValue ?? string.Empty
        };

        var ok = new Button
        {
            Left = 266,
            Top = 84,
            Width = 80,
            Text = "OK",
            DialogResult = DialogResult.OK
        };

        var cancel = new Button
        {
            Left = 352,
            Top = 84,
            Width = 80,
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.Add(label);
        dialog.Controls.Add(textBox);
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        var result = dialog.ShowDialog();
        if (result != DialogResult.OK)
        {
            return null;
        }

        return textBox.Text;
    }
}
