using System.Globalization;
using Avalonia.Data.Converters;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class BattlePositionConverter : IValueConverter
{
    public static BattlePositionConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BattlePosition position)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return BattlePositionFormatter.Format(position);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
