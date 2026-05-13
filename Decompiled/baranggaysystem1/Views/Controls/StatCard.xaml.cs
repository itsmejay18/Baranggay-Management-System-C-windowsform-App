using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Reusable statistics card for dashboards.
/// Displays a metric with icon, label, value, and optional trend.
/// </summary>
public partial class StatCard : UserControl
{
    public StatCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The metric label (e.g., "Total Residents").
    /// </summary>
    public string Label
    {
        get => labelText.Text;
        set => labelText.Text = value ?? "Metric";
    }

    /// <summary>
    /// The metric value (e.g., "1,234").
    /// </summary>
    public string Value
    {
        get => valueText.Text;
        set => valueText.Text = value ?? "0";
    }

    /// <summary>
    /// The FontAwesome icon.
    /// </summary>
    public IconChar Icon
    {
        get => ((IconBlockBase<IconChar>)(object)iconElement).Icon;
        set => ((IconBlockBase<IconChar>)(object)iconElement).Icon = value;
    }

    /// <summary>
    /// Background color of the icon circle.
    /// </summary>
    public Brush IconBackground
    {
        get => iconBorder.Background;
        set => iconBorder.Background = value;
    }

    /// <summary>
    /// Foreground color of the icon.
    /// </summary>
    public Brush IconForeground
    {
        get => ((TextBlock)(object)iconElement).Foreground;
        set => ((TextBlock)(object)iconElement).Foreground = value;
    }

    /// <summary>
    /// Trend text (e.g., "+12 this month").
    /// </summary>
    public string TrendText
    {
        get => trendText.Text;
        set
        {
            trendText.Text = value ?? "";
            trendPanel.Visibility = string.IsNullOrWhiteSpace(value)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    /// <summary>
    /// Trend direction: "Up", "Down", or "Neutral".
    /// </summary>
    public string TrendDirection
    {
        set
        {
            switch (value?.ToLowerInvariant())
            {
                case "up":
                    trendArrow.Text = "↑";
                    trendArrow.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
                    break;
                case "down":
                    trendArrow.Text = "↓";
                    trendArrow.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                    break;
                default:
                    trendArrow.Text = "→";
                    trendArrow.Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                    break;
            }
        }
    }
}
