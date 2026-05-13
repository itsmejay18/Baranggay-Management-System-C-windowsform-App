using System;
using System.Collections.Generic;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Manages navigation history for back/forward navigation support.
/// Provides breadcrumb trail tracking for the main window.
/// </summary>
internal sealed class NavigationHistory
{
    private readonly List<NavigationEntry> _history = new();
    private int _currentIndex = -1;
    private const int MaxHistory = 50;

    /// <summary>
    /// Whether back navigation is available.
    /// </summary>
    public bool CanGoBack => _currentIndex > 0;

    /// <summary>
    /// Whether forward navigation is available.
    /// </summary>
    public bool CanGoForward => _currentIndex < _history.Count - 1;

    /// <summary>
    /// Current navigation entry.
    /// </summary>
    public NavigationEntry? Current => _currentIndex >= 0 && _currentIndex < _history.Count
        ? _history[_currentIndex]
        : null;

    /// <summary>
    /// Record a navigation to a new route.
    /// </summary>
    public void Push(string route, string title)
    {
        // Remove forward history when navigating to a new page
        if (_currentIndex < _history.Count - 1)
        {
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
        }

        _history.Add(new NavigationEntry
        {
            Route = route,
            Title = title,
            Timestamp = DateTime.Now
        });

        // Trim old history
        if (_history.Count > MaxHistory)
        {
            _history.RemoveAt(0);
        }

        _currentIndex = _history.Count - 1;
    }

    /// <summary>
    /// Go back one step. Returns the route to navigate to.
    /// </summary>
    public string? GoBack()
    {
        if (!CanGoBack) return null;
        _currentIndex--;
        return _history[_currentIndex].Route;
    }

    /// <summary>
    /// Go forward one step. Returns the route to navigate to.
    /// </summary>
    public string? GoForward()
    {
        if (!CanGoForward) return null;
        _currentIndex++;
        return _history[_currentIndex].Route;
    }

    /// <summary>
    /// Get recent navigation history for display.
    /// </summary>
    public IReadOnlyList<NavigationEntry> GetRecentHistory(int count = 10)
    {
        int start = Math.Max(0, _currentIndex - count);
        int length = Math.Min(count, _currentIndex - start + 1);
        return _history.GetRange(start, length);
    }

    /// <summary>
    /// Clear all history.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
    }
}

/// <summary>
/// A single entry in the navigation history.
/// </summary>
internal sealed class NavigationEntry
{
    public string Route { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
