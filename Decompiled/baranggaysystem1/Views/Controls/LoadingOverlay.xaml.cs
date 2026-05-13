using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Reusable loading overlay with animated spinner.
/// Place over content areas to indicate async operations in progress.
/// </summary>
public partial class LoadingOverlay : UserControl
{
    private Storyboard? _spinAnimation;

    public LoadingOverlay()
    {
        InitializeComponent();
        SetupAnimation();
    }

    /// <summary>
    /// Show the loading overlay with a message.
    /// </summary>
    public void Show(string message = "Loading...", string? subMessage = null)
    {
        loadingText.Text = message;

        if (!string.IsNullOrWhiteSpace(subMessage))
        {
            subText.Text = subMessage;
            subText.Visibility = Visibility.Visible;
        }
        else
        {
            subText.Visibility = Visibility.Collapsed;
        }

        Visibility = Visibility.Visible;
        _spinAnimation?.Begin();
    }

    /// <summary>
    /// Hide the loading overlay.
    /// </summary>
    public void Hide()
    {
        _spinAnimation?.Stop();
        Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Whether the overlay is currently visible.
    /// </summary>
    public bool IsShowing => Visibility == Visibility.Visible;

    private void SetupAnimation()
    {
        var animation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(1),
            RepeatBehavior = RepeatBehavior.Forever
        };

        _spinAnimation = new Storyboard();
        Storyboard.SetTarget(animation, spinnerArc);
        Storyboard.SetTargetProperty(animation,
            new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
        _spinAnimation.Children.Add(animation);
    }
}
