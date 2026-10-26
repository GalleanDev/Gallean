using System.Globalization;
using Avalonia.Data.Converters;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class MovementSpeedConverter : IValueConverter
{
    public static MovementSpeedConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MovementSpeed speed)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return MovementSpeedFormatter.Format(speed);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
