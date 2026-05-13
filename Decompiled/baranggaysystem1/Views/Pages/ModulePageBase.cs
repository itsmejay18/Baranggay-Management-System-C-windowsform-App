using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace baranggaysystem1.Views.Pages;

public class ModulePageBase : UserControl
{
	protected Grid Root { get; }

	protected StackPanel Header { get; }

	protected Grid ContentArea { get; }

	protected TextBlock TitleLabel { get; }

	protected TextBlock SubtitleLabel { get; }

	protected StackPanel ToolbarPanel { get; }

	protected DataGrid MainGrid { get; }

	protected TextBlock EmptyStateLabel { get; }

	protected ModulePageBase(string title, string subtitle = "")
	{
		base.Background = (Brush)Application.Current.Resources["Slate100Brush"];
		Root = new Grid();
		Root.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		Root.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		Border border = new Border
		{
			Background = Brushes.White,
			Padding = new Thickness(14.0, 8.0, 14.0, 8.0),
			BorderBrush = (Brush)Application.Current.Resources["Slate100Brush"],
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0)
		};
		Grid grid = new Grid
		{
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			}
		};
		StackPanel stackPanel = new StackPanel();
		TitleLabel = new TextBlock
		{
			Text = title,
			Style = (Style)Application.Current.Resources["HeadingTextStyle"]
		};
		SubtitleLabel = new TextBlock
		{
			Text = subtitle,
			Style = (Style)Application.Current.Resources["SubtitleTextStyle"],
			Visibility = (string.IsNullOrWhiteSpace(subtitle) ? Visibility.Collapsed : Visibility.Visible)
		};
		stackPanel.Children.Add(TitleLabel);
		stackPanel.Children.Add(SubtitleLabel);
		Grid.SetColumn(stackPanel, 0);
		grid.Children.Add(stackPanel);
		ToolbarPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		Grid.SetColumn(ToolbarPanel, 1);
		grid.Children.Add(ToolbarPanel);
		border.Child = grid;
		Grid.SetRow(border, 0);
		Root.Children.Add(border);
		Border border2 = new Border
		{
			Background = (Brush)Application.Current.Resources["Slate100Brush"],
			Padding = new Thickness(10.0, 8.0, 10.0, 8.0)
		};
		ContentArea = new Grid();
		ContentArea.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		ContentArea.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		Border border3 = new Border
		{
			Background = Brushes.White,
			CornerRadius = new CornerRadius(8.0),
			BorderBrush = (Brush)Application.Current.Resources["Slate100Brush"],
			BorderThickness = new Thickness(1.0),
			ClipToBounds = true
		};
		MainGrid = new DataGrid
		{
			Style = (Style)Application.Current.Resources["StyledDataGridStyle"],
			ColumnHeaderStyle = (Style)Application.Current.Resources["StyledDataGridColumnHeaderStyle"],
			AutoGenerateColumns = false,
			IsReadOnly = true,
			SelectionMode = DataGridSelectionMode.Single
		};
		EmptyStateLabel = new TextBlock
		{
			Text = "No records found.",
			Style = (Style)Application.Current.Resources["SubtitleTextStyle"],
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		border3.Child = new Grid
		{
			Children = 
			{
				(UIElement)MainGrid,
				(UIElement)EmptyStateLabel
			}
		};
		Grid.SetRow(border3, 1);
		ContentArea.Children.Add(border3);
		border2.Child = ContentArea;
		Grid.SetRow(border2, 1);
		Root.Children.Add(border2);
		base.Content = Root;
	}
}
