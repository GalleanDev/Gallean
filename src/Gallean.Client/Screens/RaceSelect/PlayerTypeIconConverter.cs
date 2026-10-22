using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class PlayerTypeIconConverter : IValueConverter
{
    public static PlayerTypeIconConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlayerType type)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return type switch
        {
            PlayerType.Human => IconMoniker.Human,
            PlayerType.Computer => IconMoniker.Computer
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
