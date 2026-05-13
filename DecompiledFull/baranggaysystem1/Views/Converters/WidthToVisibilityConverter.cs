using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace baranggaysystem1.Views.Converters;

public sealed class WidthToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double num && double.TryParse(parameter?.ToString(), out var result))
		{
			return (!(num > result)) ? Visibility.Collapsed : Visibility.Visible;
		}
		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
