using System.Globalization;
using Avalonia.Data.Converters;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class UiSizeConverter : IValueConverter
{
    public static UiSizeConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UiSize size)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return UiSizeFormatter.Format(size);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
