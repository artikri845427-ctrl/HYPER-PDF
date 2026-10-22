// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HyperPDF.Converters;

/// <summary>Converts a boolean into a <see cref="Visibility"/> value.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is bool boolean && boolean;
        if (parameter is string text && bool.TryParse(text, out bool inverted))
        {
            flag = inverted ? !flag : flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}

/// <summary>Converts a boolean into the opposite <see cref="Visibility"/> value.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    private readonly BoolToVisibilityConverter _inner = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        object result = _inner.Convert(value, targetType, "True", culture);
        return result is Visibility visibility && visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return _inner.ConvertBack(value, targetType, parameter, culture);
    }
}
