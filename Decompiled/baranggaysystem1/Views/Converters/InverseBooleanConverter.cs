using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace baranggaysystem1.Views.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public sealed class InverseBooleanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!(value is bool flag))
		{
			return DependencyProperty.UnsetValue;
		}
		return !flag;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!(value is bool flag))
		{
			return DependencyProperty.UnsetValue;
		}
		return !flag;
	}
}
