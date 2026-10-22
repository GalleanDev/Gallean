using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class AutoSaveIntervalConverter : IValueConverter
{
    public static AutoSaveIntervalConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int interval)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return AutoSaveIntervalFormatter.Format(interval);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
