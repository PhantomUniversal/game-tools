using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Aion2Tools.Views;

/// <summary>Stops a two-way binding writing null back. A ComboBox torn down with its page clears its
/// selection, and without this every roster character lost its class on leaving the roster page.</summary>
public sealed class IgnoreNullConverter : IValueConverter
{
    public static readonly IgnoreNullConverter Instance = new IgnoreNullConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return BindingOperations.DoNothing;
        }

        return value;
    }
}
