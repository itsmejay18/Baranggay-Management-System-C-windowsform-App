using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Toast notification control for non-intrusive feedback messages.
/// Slides in from the top-right, auto-dismisses after a configurable duration.
/// </summary>
public partial class ToastNotification : UserControl
{
    private readonly DispatcherTimer _dismissTimer;

    public ToastNotification()
    {
        InitializeComponent();
        _dismissTimer = new DispatcherTimer();
        _dismissTimer.Tick += (s, e) =>
        {
            _dismissTimer.Stop();
            Dismiss();
        };
    }

    /// <summary>
    /// Show a toast notification.
    /// </summary>
    public void Show(string title, string? message = null, ToastType type = ToastType.Info, int durationMs = 4000)
    {
        toastTitle.Text = title;

        if (!string.IsNullOrWhiteSpace(message))
        {
            toastMessage.Text = message;
            toastMessage.Visibility = Visibility.Visible;
        }
        else
        {
            toastMessage.Visibility = Visibility.Collapsed;
        }

        // Set icon and color based on type
        switch (type)
        {
            case ToastType.Success:
                toastIcon.Text = "✓";
                toastBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
                break;
            case ToastType.Warning:
                toastIcon.Text = "⚠";
                toastBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                break;
            case ToastType.Error:
                toastIcon.Text = "✗";
                toastBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                break;
            default:
                toastIcon.Text = "ℹ";
                toastBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
                break;
        }

        // Animate in
        Visibility = Visibility.Visible;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        var slideIn = new ThicknessAnimation(
            new Thickness(0, -20, 0, 0),
            new Thickness(0),
            TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(OpacityProperty, fadeIn);
        BeginAnimation(MarginProperty, slideIn);

        // Auto-dismiss
        _dismissTimer.Interval = TimeSpan.FromMilliseconds(durationMs);
        _dismissTimer.Start();
    }

    private void Dismiss()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (s, e) =>
        {
            Visibility = Visibility.Collapsed;
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _dismissTimer.Stop();
        Dismiss();
    }
}

/// <summary>
/// Toast notification severity type.
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Static helper for showing toast notifications from anywhere in the app.
/// Requires a ToastNotification control to be placed in the MainWindow.
/// </summary>
public static class ToastService
{
    private static ToastNotification? _toastControl;

    /// <summary>
    /// Register the toast control instance (call from MainWindow.Loaded).
    /// </summary>
    public static void Register(ToastNotification control)
    {
        _toastControl = control;
    }

    /// <summary>
    /// Show a toast notification.
    /// </summary>
    public static void Show(string title, string? message = null, ToastType type = ToastType.Info, int durationMs = 4000)
    {
        if (_toastControl == null) return;

        Application.Current?.Dispatcher.Invoke(() =>
        {
            _toastControl.Show(title, message, type, durationMs);
        });
    }

    public static void Success(string title, string? message = null) =>
        Show(title, message, ToastType.Success);

    public static void Warning(string title, string? message = null) =>
        Show(title, message, ToastType.Warning);

    public static void Error(string title, string? message = null) =>
        Show(title, message, ToastType.Error);

    public static void Info(string title, string? message = null) =>
        Show(title, message, ToastType.Info);
}
