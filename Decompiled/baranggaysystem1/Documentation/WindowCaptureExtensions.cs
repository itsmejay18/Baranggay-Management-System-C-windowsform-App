using System.Windows;

namespace baranggaysystem1.Documentation;

internal static class WindowCaptureExtensions
{
	public static bool IsClosed(this Window window)
	{
		if (!window.IsLoaded)
		{
			return !window.IsVisible;
		}
		return false;
	}
}
