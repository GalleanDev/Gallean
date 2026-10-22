using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class WorldDifficultyConverter : IValueConverter
{
    public static WorldDifficultyConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WorldDifficulty difficulty)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return WorldDifficultyFormatter.Format(difficulty);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
