using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class EnumNotEqualsConverter : IValueConverter
{
    public static EnumNotEqualsConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !Equals(value, parameter);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
