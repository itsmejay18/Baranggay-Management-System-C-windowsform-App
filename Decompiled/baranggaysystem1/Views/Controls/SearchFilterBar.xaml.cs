using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Reusable search and filter bar with debounced input.
/// Provides consistent search UX across all module list pages.
/// </summary>
public partial class SearchFilterBar : UserControl
{
    private readonly DispatcherTimer _debounceTimer;
    private string _lastSearchText = string.Empty;

    /// <summary>
    /// Fired when the search text changes (debounced, 300ms delay).
    /// </summary>
    public event Action<string>? SearchChanged;

    /// <summary>
    /// Fired when Enter is pressed in the search box.
    /// </summary>
    public event Action<string>? SearchSubmitted;

    /// <summary>
    /// Fired when the filter toggle button is clicked.
    /// </summary>
    public event Action? FilterToggleClicked;

    public SearchFilterBar()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += OnDebounceElapsed;

        searchTextBox.GotFocus += (s, e) => placeholderText.Visibility = Visibility.Collapsed;
        searchTextBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrEmpty(searchTextBox.Text))
                placeholderText.Visibility = Visibility.Visible;
        };
    }

    /// <summary>
    /// Placeholder text for the search box.
    /// </summary>
    public string Placeholder
    {
        get => placeholderText.Text;
        set => placeholderText.Text = value ?? "Search...";
    }

    /// <summary>
    /// Current search text.
    /// </summary>
    public string SearchText => searchTextBox.Text?.Trim() ?? "";

    /// <summary>
    /// Show/hide the filter toggle button.
    /// </summary>
    public bool ShowFilterButton
    {
        get => filterToggle.Visibility == Visibility.Visible;
        set => filterToggle.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Update the result count display.
    /// </summary>
    public void SetResultCount(int count, int? total = null)
    {
        if (total.HasValue)
        {
            resultCountText.Text = $"{count:N0} of {total.Value:N0}";
        }
        else
        {
            resultCountText.Text = $"{count:N0} result(s)";
        }
        resultCountText.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Clear the result count display.
    /// </summary>
    public void ClearResultCount()
    {
        resultCountText.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Programmatically set the search text.
    /// </summary>
    public void SetText(string text)
    {
        searchTextBox.Text = text ?? "";
    }

    /// <summary>
    /// Focus the search input.
    /// </summary>
    public void FocusSearch()
    {
        searchTextBox.Focus();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool hasText = !string.IsNullOrEmpty(searchTextBox.Text);
        clearButton.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

        if (!hasText)
            placeholderText.Visibility = Visibility.Visible;

        // Restart debounce timer
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _debounceTimer.Stop();
            string text = searchTextBox.Text?.Trim() ?? "";
            _lastSearchText = text;
            SearchSubmitted?.Invoke(text);
            SearchChanged?.Invoke(text);
        }
        else if (e.Key == Key.Escape)
        {
            searchTextBox.Text = "";
            SearchChanged?.Invoke("");
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        searchTextBox.Text = "";
        _lastSearchText = "";
        SearchChanged?.Invoke("");
        searchTextBox.Focus();
    }

    private void FilterToggle_Click(object sender, RoutedEventArgs e)
    {
        FilterToggleClicked?.Invoke();
    }

    private void OnDebounceElapsed(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        string text = searchTextBox.Text?.Trim() ?? "";

        if (text != _lastSearchText)
        {
            _lastSearchText = text;
            SearchChanged?.Invoke(text);
        }
    }
}
