using System;
using System.Windows;
using System.Windows.Controls;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Reusable pagination bar for data grids and lists.
/// Provides page navigation, page size selection, and item count display.
/// </summary>
public partial class PaginationBar : UserControl
{
    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalItems;
    private int _pageSize = 50;

    /// <summary>
    /// Fired when the page changes. Parameter is the new page number.
    /// </summary>
    public event Action<int>? PageChanged;

    /// <summary>
    /// Fired when the page size changes. Parameter is the new page size.
    /// </summary>
    public event Action<int>? PageSizeChanged;

    public int CurrentPage => _currentPage;
    public int TotalPages => _totalPages;
    public int PageSize => _pageSize;

    public PaginationBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update the pagination state.
    /// </summary>
    public void Update(int currentPage, int totalPages, int totalItems)
    {
        _currentPage = Math.Max(1, currentPage);
        _totalPages = Math.Max(1, totalPages);
        _totalItems = totalItems;

        currentPageText.Text = _currentPage.ToString();
        pageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
        totalItemsText.Text = $"{_totalItems:N0} total item(s)";

        // Enable/disable buttons
        firstButton.IsEnabled = _currentPage > 1;
        prevButton.IsEnabled = _currentPage > 1;
        nextButton.IsEnabled = _currentPage < _totalPages;
        lastButton.IsEnabled = _currentPage < _totalPages;

        firstButton.Opacity = firstButton.IsEnabled ? 1.0 : 0.4;
        prevButton.Opacity = prevButton.IsEnabled ? 1.0 : 0.4;
        nextButton.Opacity = nextButton.IsEnabled ? 1.0 : 0.4;
        lastButton.Opacity = lastButton.IsEnabled ? 1.0 : 0.4;
    }

    /// <summary>
    /// Reset to page 1.
    /// </summary>
    public void Reset()
    {
        Update(1, 1, 0);
    }

    private void FirstButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage = 1;
            PageChanged?.Invoke(_currentPage);
        }
    }

    private void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            PageChanged?.Invoke(_currentPage);
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            PageChanged?.Invoke(_currentPage);
        }
    }

    private void LastButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage = _totalPages;
            PageChanged?.Invoke(_currentPage);
        }
    }

    private void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (pageSizeCombo.SelectedItem is ComboBoxItem item)
        {
            if (int.TryParse(item.Content?.ToString(), out int newSize) && newSize != _pageSize)
            {
                _pageSize = newSize;
                _currentPage = 1;
                PageSizeChanged?.Invoke(_pageSize);
            }
        }
    }
}
