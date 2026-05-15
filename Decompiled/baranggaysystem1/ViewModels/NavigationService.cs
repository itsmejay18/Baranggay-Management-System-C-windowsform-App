using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;

namespace baranggaysystem1.ViewModels;

public sealed class NavigationService
{
	private static readonly Lazy<NavigationService> _instance = new Lazy<NavigationService>(() => new NavigationService());

	private ContentControl? _contentHost;

	/// <summary>
	/// Maximum number of cached page instances.
	/// Requirement 7.7: The page cache SHALL hold a maximum of 30 page instances.
	/// </summary>
	public const int MaxCacheCapacity = 30;

	/// <summary>
	/// Maps route keys to their cache entries (page instance + linked list node).
	/// </summary>
	private readonly Dictionary<string, LinkedListNode<LruCacheEntry>> _pageCache =
		new Dictionary<string, LinkedListNode<LruCacheEntry>>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Doubly-linked list maintaining access order for LRU eviction.
	/// Most-recently-accessed entries are at the front; least-recently-accessed at the back.
	/// </summary>
	private readonly LinkedList<LruCacheEntry> _accessOrder = new LinkedList<LruCacheEntry>();

	/// <summary>
	/// Set of route keys that are currently protected from eviction because they are
	/// the origin page of an active fullscreen session.
	/// Requirement 7.7: Fullscreen view origin pages are not evicted during active sessions.
	/// </summary>
	private readonly HashSet<string> _protectedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	// Debouncing state (Requirement 8)
	private DateTime _lastNavigationTime = DateTime.MinValue;
	private bool _isTransitionInProgress;
	private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(200);

	// Single active view constraint state (Requirement 11)
	private UIElement? _currentView;

	// Flag to temporarily bypass the unsaved changes guard
	// (used when NavigateBackFromFullscreen has already confirmed discard)
	private bool _bypassUnsavedChangesGuard;

	/// <summary>
	/// Event raised when a navigation failure occurs (target view fails to instantiate).
	/// Requirement 11.4: Report the failure via an error indication.
	/// </summary>
	public event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

	public static NavigationService Instance => _instance.Value;

	/// <summary>
	/// Gets whether a transition animation is currently in progress.
	/// While true, new navigation requests are blocked.
	/// </summary>
	public bool IsTransitionInProgress
	{
		get => _isTransitionInProgress;
		private set => _isTransitionInProgress = value;
	}

	/// <summary>
	/// Gets the currently active view in the content host.
	/// Requirement 11.1: Exactly one view is displayed at any point in time.
	/// </summary>
	public UIElement? CurrentView => _currentView;

	private NavigationService()
	{
	}

	public void Initialize(ContentControl host)
	{
		_contentHost = host;
		// Capture any existing content as the current view
		_currentView = host.Content as UIElement;
	}

	/// <summary>
	/// Checks whether the current view has unsaved changes and prompts the user
	/// for confirmation before allowing navigation away.
	/// Returns true if navigation should proceed, false if it should be blocked.
	/// </summary>
	/// <remarks>
	/// Requirements 3.1, 3.2: Intercept sidebar menu clicks and keyboard shortcuts
	/// when a fullscreen form has dirty state. Show confirmation dialog with
	/// "Discard Changes" and "Keep Editing" options.
	/// Requirement 3.3: If discard, proceed with navigation and reset Dirty_State.
	/// Requirement 3.4: If cancel, dismiss dialog and preserve all form state.
	/// Requirement 3.5: If content does not implement dirty tracking, allow navigation.
	/// </remarks>
	public bool GuardUnsavedChanges()
	{
		// If bypass flag is set, skip the guard (already confirmed by back button)
		if (_bypassUnsavedChangesGuard)
		{
			return true;
		}

		// Check if the current view is a FullscreenViewHost with dirty content
		if (_currentView is FullscreenViewHost host)
		{
			// Check if ContentArea is FullscreenFormBase and IsDirty is true
			if (host.ContentArea is FullscreenFormBase form && form.IsDirty)
			{
				// Show confirmation dialog with "Discard Changes" and "Keep Editing" options
				var owner = Application.Current?.MainWindow;
				bool confirmed = ConfirmationDialog.Show(
					owner,
					"Unsaved Changes",
					"You have unsaved changes. Are you sure you want to leave? Your changes will be lost.",
					"Discard Changes",
					"Keep Editing",
					ConfirmationType.Warning);

				if (confirmed)
				{
					// User chose to discard: reset dirty state and allow navigation
					form.IsDirty = false;
					return true;
				}
				else
				{
					// User chose to keep editing: block navigation, preserve form state
					return false;
				}
			}

			// Check the host-level unsaved changes flag (for non-FullscreenFormBase content)
			if (host.HasUnsavedChangesFlag)
			{
				var owner = Application.Current?.MainWindow;
				bool confirmed = ConfirmationDialog.Show(
					owner,
					"Unsaved Changes",
					"You have unsaved changes. Are you sure you want to leave? Your changes will be lost.",
					"Discard Changes",
					"Keep Editing",
					ConfirmationType.Warning);

				if (confirmed)
				{
					host.SetUnsavedChanges(false);
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		// No fullscreen view or no dirty state — allow navigation
		return true;
	}

	/// <summary>
	/// Temporarily bypasses the unsaved changes guard for the next navigation.
	/// Used by NavigateBackFromFullscreen when the back button has already
	/// confirmed the discard with the user.
	/// </summary>
	public void BypassUnsavedChangesGuard()
	{
		_bypassUnsavedChangesGuard = true;
	}

	/// <summary>
	/// Resets the bypass flag after navigation completes.
	/// </summary>
	public void ResetBypassFlag()
	{
		_bypassUnsavedChangesGuard = false;
	}

	/// <summary>
	/// Determines whether a navigation request should be accepted based on debounce rules.
	/// Returns true if the request is allowed, false if it should be discarded.
	/// </summary>
	/// <remarks>
	/// Navigation is blocked when:
	/// - A transition animation is in progress (Requirement 8.2)
	/// - The request arrives within 200ms of the last accepted navigation (Requirement 8.1)
	/// </remarks>
	public bool CanNavigate()
	{
		// Block navigation while a transition animation is in progress (Req 8.2)
		if (_isTransitionInProgress)
		{
			return false;
		}

		// Block navigation within the 200ms debounce window (Req 8.1)
		var elapsed = DateTime.UtcNow - _lastNavigationTime;
		if (elapsed < DebounceWindow)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Navigates to the specified page, enforcing the single active view constraint.
	/// Requirement 11.1: Exactly one view displayed in pageHost.Content at any time.
	/// Requirement 11.2: Remove previous view from visual tree before new view becomes visible.
	/// Requirement 11.3: Complete or cancel in-progress transitions before applying new navigation.
	/// </summary>
	public void NavigateTo(UIElement page)
	{
		if (_contentHost == null)
		{
			throw new InvalidOperationException("NavigationService not initialized.");
		}

		if (page == null)
		{
			throw new ArgumentNullException(nameof(page));
		}

		// Requirement 11.3: Complete or cancel in-progress transitions before applying new navigation
		if (_isTransitionInProgress)
		{
			CancelInProgressTransition();
		}

		try
		{
			_isTransitionInProgress = true;

			// Record the navigation timestamp for debounce tracking
			_lastNavigationTime = DateTime.UtcNow;

			// Requirement 11.1 & 11.2: Setting Content replaces the previous view with exactly
			// one new view. The previous view is removed from the visual tree automatically
			// by the ContentControl before the new view becomes visible.
			_contentHost.Content = page;
			_currentView = page;
		}
		catch
		{
			// Requirement 11.4: On failure, current view is retained (Content unchanged on throw)
			// Reset debounce state so next request is accepted immediately (Req 8.3)
			ResetDebounceState();
			throw;
		}
		finally
		{
			_isTransitionInProgress = false;
		}
	}

	/// <summary>
	/// Navigates to a view created by the factory, with instantiation failure protection.
	/// Requirement 11.4: Retain current view if target view fails to instantiate.
	/// </summary>
	public void NavigateTo<T>(Func<T> factory) where T : UIElement
	{
		if (factory == null)
		{
			throw new ArgumentNullException(nameof(factory));
		}

		T? page;
		try
		{
			page = factory();
		}
		catch (Exception ex)
		{
			// Requirement 11.4: Retain current view if target view fails to instantiate
			OnNavigationFailed(ex, typeof(T).Name);
			return;
		}

		if (page == null)
		{
			// Requirement 11.4: Retain current view unchanged
			OnNavigationFailed(
				new InvalidOperationException("Factory returned null."),
				typeof(T).Name);
			return;
		}

		NavigateTo(page);
	}

	/// <summary>
	/// Signals that a transition animation has started.
	/// While in progress, all navigation requests are blocked (Requirement 8.2).
	/// </summary>
	public void BeginTransition()
	{
		_isTransitionInProgress = true;
	}

	/// <summary>
	/// Signals that the transition animation has completed.
	/// New navigation requests are accepted normally after this call (Requirement 8.4).
	/// </summary>
	public void EndTransition()
	{
		_isTransitionInProgress = false;
	}

	/// <summary>
	/// Resets the debounce state so the next navigation request is accepted immediately.
	/// Called when a navigation request fails (Requirement 8.3).
	/// </summary>
	public void ResetDebounceState()
	{
		_lastNavigationTime = DateTime.MinValue;
		_isTransitionInProgress = false;
	}

	/// <summary>
	/// Gets a cached page instance or creates and caches a new one using the factory.
	/// If the factory throws, the current view is retained (Requirement 11.4).
	/// Implements LRU cache with max capacity of 30 (Requirement 7.7).
	/// Accessing a cached page promotes it to most-recently-used.
	/// </summary>
	public UIElement GetOrCreate(string cacheKey, Func<UIElement> factory)
	{
		if (_pageCache.TryGetValue(cacheKey, out LinkedListNode<LruCacheEntry>? node))
		{
			// Promote to most-recently-used (move to front of access order list)
			_accessOrder.Remove(node);
			_accessOrder.AddFirst(node);
			return node.Value.Page;
		}

		UIElement page;
		try
		{
			page = factory();
		}
		catch (Exception ex)
		{
			// Requirement 11.4: Report failure; caller should handle gracefully
			OnNavigationFailed(ex, cacheKey);
			throw;
		}

		// Evict least-recently-accessed page if at capacity (Requirement 7.7)
		EvictIfNeeded();

		// Add new entry to cache and front of access order list
		var entry = new LruCacheEntry(cacheKey, page);
		var newNode = _accessOrder.AddFirst(entry);
		_pageCache[cacheKey] = newNode;
		return page;
	}

	/// <summary>
	/// Removes a specific page from the cache (e.g., when it needs to be refreshed).
	/// </summary>
	public void InvalidateCache(string cacheKey)
	{
		if (_pageCache.TryGetValue(cacheKey, out LinkedListNode<LruCacheEntry>? node))
		{
			_accessOrder.Remove(node);
			_pageCache.Remove(cacheKey);
		}
	}

	/// <summary>
	/// Clears the entire page cache and access order tracking.
	/// </summary>
	public void ClearCache()
	{
		_pageCache.Clear();
		_accessOrder.Clear();
	}

	/// <summary>
	/// Marks a route as protected from LRU eviction.
	/// Called when a fullscreen view is opened, protecting the origin page.
	/// Requirement 7.7: Fullscreen view origin pages are not evicted during active sessions.
	/// </summary>
	public void ProtectRoute(string routeKey)
	{
		if (!string.IsNullOrWhiteSpace(routeKey))
		{
			_protectedRoutes.Add(routeKey);
		}
	}

	/// <summary>
	/// Removes eviction protection from a route.
	/// Called when navigating back from a fullscreen view (session ends).
	/// </summary>
	public void UnprotectRoute(string routeKey)
	{
		if (!string.IsNullOrWhiteSpace(routeKey))
		{
			_protectedRoutes.Remove(routeKey);
		}
	}

	/// <summary>
	/// Gets the current number of cached pages.
	/// </summary>
	public int CacheCount => _pageCache.Count;

	/// <summary>
	/// Checks whether a route is currently protected from eviction.
	/// </summary>
	public bool IsRouteProtected(string routeKey)
	{
		return _protectedRoutes.Contains(routeKey);
	}

	/// <summary>
	/// Evicts the least-recently-accessed page from the cache if the cache is at capacity.
	/// Protected routes (origin pages of active fullscreen sessions) are skipped.
	/// Requirement 7.7: Evict least-recently-accessed cached page when limit is reached.
	/// </summary>
	private void EvictIfNeeded()
	{
		while (_pageCache.Count >= MaxCacheCapacity)
		{
			// Find the least-recently-accessed entry that is NOT protected
			var candidate = _accessOrder.Last;
			LinkedListNode<LruCacheEntry>? evictTarget = null;

			while (candidate != null)
			{
				if (!_protectedRoutes.Contains(candidate.Value.CacheKey))
				{
					evictTarget = candidate;
					break;
				}
				candidate = candidate.Previous;
			}

			if (evictTarget == null)
			{
				// All entries are protected — cannot evict, allow cache to exceed limit
				break;
			}

			// Remove the eviction target from both the dictionary and the access order list
			_pageCache.Remove(evictTarget.Value.CacheKey);
			_accessOrder.Remove(evictTarget);
		}
	}

	/// <summary>
	/// Cancels any in-progress transition animations on the current view.
	/// Requirement 11.3: Complete or cancel in-progress transitions before applying new navigation.
	/// </summary>
	private void CancelInProgressTransition()
	{
		if (_currentView is FrameworkElement currentElement)
		{
			// Stop any running animations by clearing them (sets property to current value)
			currentElement.BeginAnimation(UIElement.OpacityProperty, null);
			if (currentElement.RenderTransform is TranslateTransform translate)
			{
				translate.BeginAnimation(TranslateTransform.YProperty, null);
			}
		}

		_isTransitionInProgress = false;
	}

	/// <summary>
	/// Raises the NavigationFailed event and logs the failure.
	/// Requirement 11.4: Report the failure via an error indication.
	/// </summary>
	private void OnNavigationFailed(Exception exception, string targetDescription)
	{
		Debug.WriteLine(
			$"[NavigationService] Navigation failed for '{targetDescription}': {exception.Message}");

		NavigationFailed?.Invoke(this, new NavigationFailedEventArgs(exception, targetDescription));
	}
}

/// <summary>
/// Event arguments for navigation failure events.
/// Requirement 11.4: Report the failure via an error indication.
/// </summary>
public class NavigationFailedEventArgs : EventArgs
{
	/// <summary>
	/// The exception that caused the navigation failure.
	/// </summary>
	public Exception Exception { get; }

	/// <summary>
	/// A description of the target view that failed to instantiate.
	/// </summary>
	public string TargetDescription { get; }

	public NavigationFailedEventArgs(Exception exception, string targetDescription)
	{
		Exception = exception ?? throw new ArgumentNullException(nameof(exception));
		TargetDescription = targetDescription ?? string.Empty;
	}
}

/// <summary>
/// Represents a single entry in the LRU page cache.
/// Stores the cache key and the cached page instance.
/// </summary>
public class LruCacheEntry
{
	/// <summary>
	/// The route key used to identify this cached page.
	/// </summary>
	public string CacheKey { get; }

	/// <summary>
	/// The cached page instance.
	/// </summary>
	public UIElement Page { get; }

	public LruCacheEntry(string cacheKey, UIElement page)
	{
		CacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
		Page = page ?? throw new ArgumentNullException(nameof(page));
	}
}
