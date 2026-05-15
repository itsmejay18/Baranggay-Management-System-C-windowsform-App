using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using FontAwesome.Sharp;
namespace baranggaysystem1.Views.Dialogs;

/// <summary>
/// Cross-module global search window. Searches residents, clearances,
/// blotter cases, and payments from a single unified search box.
/// </summary>
public partial class GlobalSearchWindow : Window
{
    private CancellationTokenSource? _searchCts;
    private readonly List<GlobalSearchResult> _results = new();

    public GlobalSearchWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => searchBox.Focus();
        PreviewKeyDown += OnPreviewKeyDown;
        Deactivated += (s, e) => { if (IsVisible) Close(); };
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = searchBox.Text?.Trim() ?? "";
        if (query.Length < 2)
        {
            ShowEmptyState();
            resultCountLabel.Text = "Type at least 2 characters to search";
            return;
        }

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(250, token); // debounce
            if (token.IsCancellationRequested) return;

            string activeFilter = GetActiveFilter();
            var results = await Task.Run(() => ExecuteSearch(query, activeFilter), token);
            if (token.IsCancellationRequested) return;

            _results.Clear();
            _results.AddRange(results);
            RenderResults(results);
            resultCountLabel.Text = $"{results.Count} result(s) found";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            AppLogger.LogError("Global search failed.", ex);
            resultCountLabel.Text = "Search error occurred";
        }
    }

    private void FilterTab_Checked(object sender, RoutedEventArgs e)
    {
        if (IsLoaded && searchBox.Text?.Trim().Length >= 2)
        {
            SearchBox_TextChanged(searchBox, null!);
        }
    }

    private string GetActiveFilter()
    {
        if (filterResidents?.IsChecked == true) return "residents";
        if (filterClearances?.IsChecked == true) return "clearances";
        if (filterBlotter?.IsChecked == true) return "blotter";
        if (filterPayments?.IsChecked == true) return "payments";
        return "all";
    }

    private static List<GlobalSearchResult> ExecuteSearch(string query, string filter)
    {
        var results = new List<GlobalSearchResult>();
        string escaped = query.Replace("'", "''");

        if (filter == "all" || filter == "residents")
        {
            try
            {
                var dt = DbHelper.LoadTable(
                    $"SELECT resident_id, CONCAT(first_name, ' ', last_name) AS full_name, contact_no, status " +
                    $"FROM resident WHERE COALESCE(is_deleted, 0) = 0 AND " +
                    $"(CONCAT(first_name, ' ', last_name) LIKE '%{escaped}%' OR contact_no LIKE '%{escaped}%' OR resident_id LIKE '%{escaped}%') " +
                    $"LIMIT 10");
                foreach (DataRow row in dt.Rows)
                {
                    results.Add(new GlobalSearchResult
                    {
                        Module = "Residents",
                        Icon = IconChar.Users,
                        IconColor = "#16A34A",
                        Title = row["full_name"]?.ToString() ?? "Unknown",
                        Subtitle = $"ID #{row["resident_id"]} · {row["status"]} · {row["contact_no"]}",
                        Route = "ResidentWorkspace",
                        RecordId = Convert.ToInt32(row["resident_id"])
                    });
                }
            }
            catch { }
        }

        if (filter == "all" || filter == "clearances")
        {
            try
            {
                var dt = DbHelper.LoadTable(
                    $"SELECT dr.doc_request_id, dr.tracking_code, dr.certification_type, dr.status, " +
                    $"CONCAT(r.first_name, ' ', r.last_name) AS resident_name " +
                    $"FROM document_request dr LEFT JOIN resident r ON dr.resident_id = r.resident_id " +
                    $"WHERE (dr.tracking_code LIKE '%{escaped}%' OR CONCAT(r.first_name, ' ', r.last_name) LIKE '%{escaped}%' " +
                    $"OR dr.certification_type LIKE '%{escaped}%') LIMIT 10");
                foreach (DataRow row in dt.Rows)
                {
                    results.Add(new GlobalSearchResult
                    {
                        Module = "Clearances",
                        Icon = IconChar.Stamp,
                        IconColor = "#D97706",
                        Title = $"{row["tracking_code"]} — {row["certification_type"]}",
                        Subtitle = $"{row["resident_name"]} · {row["status"]}",
                        Route = "Clearances",
                        RecordId = Convert.ToInt32(row["doc_request_id"])
                    });
                }
            }
            catch { }
        }

        if (filter == "all" || filter == "blotter")
        {
            try
            {
                var dt = DbHelper.LoadTable(
                    $"SELECT case_id, case_no, complainant_name, respondent_name, incident_type, status " +
                    $"FROM blotter_case WHERE " +
                    $"(case_no LIKE '%{escaped}%' OR complainant_name LIKE '%{escaped}%' OR respondent_name LIKE '%{escaped}%' " +
                    $"OR incident_type LIKE '%{escaped}%') LIMIT 10");
                foreach (DataRow row in dt.Rows)
                {
                    results.Add(new GlobalSearchResult
                    {
                        Module = "Blotter",
                        Icon = IconChar.Gavel,
                        IconColor = "#DC2626",
                        Title = $"{row["case_no"]} — {row["incident_type"]}",
                        Subtitle = $"{row["complainant_name"]} vs {row["respondent_name"]} · {row["status"]}",
                        Route = "ResidentCases",
                        RecordId = Convert.ToInt32(row["case_id"])
                    });
                }
            }
            catch { }
        }

        if (filter == "all" || filter == "payments")
        {
            try
            {
                var dt = DbHelper.LoadTable(
                    $"SELECT dp.payment_id, dp.or_number, dp.amount, dp.paid_at, " +
                    $"CONCAT(r.first_name, ' ', r.last_name) AS resident_name " +
                    $"FROM document_payment dp LEFT JOIN resident r ON dp.resident_id = r.resident_id " +
                    $"WHERE (dp.or_number LIKE '%{escaped}%' OR CONCAT(r.first_name, ' ', r.last_name) LIKE '%{escaped}%') LIMIT 10");
                foreach (DataRow row in dt.Rows)
                {
                    results.Add(new GlobalSearchResult
                    {
                        Module = "Payments",
                        Icon = IconChar.MoneyBill,
                        IconColor = "#2563EB",
                        Title = $"OR {row["or_number"]} — PHP {row["amount"]}",
                        Subtitle = $"{row["resident_name"]} · {row["paid_at"]}",
                        Route = "ResidentPayments",
                        RecordId = Convert.ToInt32(row["payment_id"])
                    });
                }
            }
            catch { }
        }

        return results;
    }

    private void RenderResults(List<GlobalSearchResult> results)
    {
        resultsPanel.Children.Clear();

        if (results.Count == 0)
        {
            var noResults = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) };
            noResults.Children.Add(new TextBlock { Text = "No results found", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("Slate500Brush"), HorizontalAlignment = HorizontalAlignment.Center });
            noResults.Children.Add(new TextBlock { Text = "Try a different search term or filter", FontSize = 11, Foreground = (Brush)FindResource("Slate400Brush"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
            resultsPanel.Children.Add(noResults);
            return;
        }

        // Group by module
        var grouped = results.GroupBy(r => r.Module);
        foreach (var group in grouped)
        {
            var header = new TextBlock
            {
                Text = group.Key.ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("Slate400Brush"),
                Margin = new Thickness(0, 12, 0, 6)
            };
            resultsPanel.Children.Add(header);

            foreach (var result in group)
            {
                var card = BuildResultCard(result);
                resultsPanel.Children.Add(card);
            }
        }
    }

    private Border BuildResultCard(GlobalSearchResult result)
    {
        var card = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 4),
            Cursor = Cursors.Hand,
            BorderBrush = (Brush)FindResource("Slate100Brush"),
            BorderThickness = new Thickness(1)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Icon
        var iconBorder = new Border
        {
            Width = 32, Height = 32, CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.IconColor + "18")),
            VerticalAlignment = VerticalAlignment.Center
        };
        var icon = new IconBlock { Icon = result.Icon, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.IconColor));
        iconBorder.Child = icon;
        Grid.SetColumn(iconBorder, 0);
        grid.Children.Add(iconBorder);

        // Text
        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
        textPanel.Children.Add(new TextBlock { Text = result.Title, FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        textPanel.Children.Add(new TextBlock { Text = result.Subtitle, FontSize = 10, Foreground = (Brush)FindResource("Slate500Brush"), Margin = new Thickness(0, 2, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(textPanel, 1);
        grid.Children.Add(textPanel);

        // Module badge
        var badge = new Border
        {
            Background = (Brush)FindResource("Slate50Brush"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.Child = new TextBlock { Text = result.Module, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("Slate500Brush") };
        Grid.SetColumn(badge, 2);
        grid.Children.Add(badge);

        card.Child = grid;

        // Hover effect
        card.MouseEnter += (s, e) => card.Background = (Brush)FindResource("Slate50Brush");
        card.MouseLeave += (s, e) => card.Background = Brushes.White;

        // Click to navigate
        card.MouseLeftButtonDown += (s, e) =>
        {
            if (Application.Current.MainWindow is Views.MainWindow mainWindow)
            {
                mainWindow.NavigatePage(result.Route);
            }
            Close();
        };

        return card;
    }

    private void ShowEmptyState()
    {
        resultsPanel.Children.Clear();
        resultsPanel.Children.Add(emptyState);
        emptyState.Visibility = Visibility.Visible;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}

/// <summary>
/// Represents a single search result from any module.
/// </summary>
internal sealed class GlobalSearchResult
{
    public string Module { get; init; } = string.Empty;
    public IconChar Icon { get; init; }
    public string IconColor { get; init; } = "#64748B";
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Route { get; init; } = "Home";
    public int RecordId { get; init; }
}
