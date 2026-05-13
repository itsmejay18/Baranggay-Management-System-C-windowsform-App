using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace baranggaysystem1.Views.Converters;

public sealed class PathToBitmapConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!(value is string text) || string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.UriSource = new Uri(text);
			bitmapImage.EndInit();
			((Freezable)bitmapImage).Freeze();
			return bitmapImage;
		}
		catch
		{
			return null;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
