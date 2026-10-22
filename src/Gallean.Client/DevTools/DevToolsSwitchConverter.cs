using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class DevToolsSwitchConverter : IValueConverter
{
    public static DevToolsSwitchConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool enabled)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        if (enabled)
        {
            return Tr("On");
        }
        else
        {
            return Tr("Off");
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
