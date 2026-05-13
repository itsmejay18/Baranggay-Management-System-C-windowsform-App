using System;
using System.Windows;
using System.Windows.Controls;

namespace baranggaysystem1.ViewModels;

public sealed class NavigationService
{
	private static readonly Lazy<NavigationService> _instance = new Lazy<NavigationService>(() => new NavigationService());

	private ContentControl? _contentHost;

	public static NavigationService Instance => _instance.Value;

	private NavigationService()
	{
	}

	public void Initialize(ContentControl host)
	{
		_contentHost = host;
	}

	public void NavigateTo(UIElement page)
	{
		if (_contentHost == null)
		{
			throw new InvalidOperationException("NavigationService not initialized.");
		}
		_contentHost.Content = page;
	}

	public void NavigateTo<T>(Func<T> factory) where T : UIElement
	{
		NavigateTo(factory());
	}
}
