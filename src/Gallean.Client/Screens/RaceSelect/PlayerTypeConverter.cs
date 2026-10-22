using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class PlayerTypeConverter : IValueConverter
{
    public static PlayerTypeConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlayerType type)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return PlayerTypeFormatter.Format(type);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
