using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Tempovium.Converters;

public class SecondsToTimestampConverter : IValueConverter
{
    public static readonly SecondsToTimestampConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);

            if (time.TotalHours >= 1)
                return time.ToString(@"hh\:mm\:ss");

            return time.ToString(@"mm\:ss");
        }

        return "00:00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}