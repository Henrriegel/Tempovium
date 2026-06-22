using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Tempovium.Infrastructure.Persistence;
using Tempovium.Services;

namespace Tempovium.Converters;

public class MediaPathDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return MediaPathDisplayFormatter.Format(value as string, TempoviumDataPaths.GetManagedMediaDirectory());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
