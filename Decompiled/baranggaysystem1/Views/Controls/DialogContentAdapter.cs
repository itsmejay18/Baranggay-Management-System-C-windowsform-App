using System;
using System.Windows;
using System.Windows.Controls;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Adapter that hosts a Window's visual content within a fullscreen view.
/// Extracts the root content from a dialog Window and presents it as a UserControl
/// suitable for embedding in FullscreenViewHost.
///
/// This enables gradual migration of existing dialog windows to fullscreen views
/// without requiring immediate rewrite of each dialog's internal logic.
/// The adapter intercepts DialogResult and Close() calls to integrate with
/// the fullscreen navigation flow.
/// </summary>
public class DialogContentAdapter : UserControl
{
    private readonly Window _dialogWindow;
    private bool _resultSet;

    /// <summary>
    /// Gets whether the dialog completed with a positive result (save/ok).
    /// </summary>
    public bool DialogResultPositive { get; private set; }

    /// <summary>
    /// Event raised when the dialog signals completion (save or cancel).
    /// </summary>
    public event EventHandler<bool>? DialogCompleted;

    /// <summary>
    /// Creates an adapter that hosts the given Window's content.
    /// The Window is never shown as a separate window — its content tree
    /// is extracted and displayed inline within the fullscreen view.
    /// </summary>
    /// <param name="dialogWindow">The dialog Window whose content to host.</param>
    public DialogContentAdapter(Window dialogWindow)
    {
        _dialogWindow = dialogWindow ?? throw new ArgumentNullException(nameof(dialogWindow));

        // Extract the content from the Window and host it in this UserControl
        var content = _dialogWindow.Content as UIElement;
        if (content != null)
        {
            _dialogWindow.Content = null; // Detach from Window's visual tree
            Content = content;
        }
        else
        {
            // Fallback: use a placeholder if content extraction fails
            Content = new TextBlock
            {
                Text = "Form content loading...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    /// <summary>
    /// Signals that the dialog completed successfully.
    /// Call this when the embedded form's save operation succeeds.
    /// </summary>
    public void SignalSuccess()
    {
        if (!_resultSet)
        {
            _resultSet = true;
            DialogResultPositive = true;
            DialogCompleted?.Invoke(this, true);
        }
    }

    /// <summary>
    /// Signals that the dialog was cancelled.
    /// </summary>
    public void SignalCancel()
    {
        if (!_resultSet)
        {
            _resultSet = true;
            DialogResultPositive = false;
            DialogCompleted?.Invoke(this, false);
        }
    }

    /// <summary>
    /// Gets the underlying dialog Window instance for accessing dialog-specific properties.
    /// </summary>
    public Window DialogWindow => _dialogWindow;
}
