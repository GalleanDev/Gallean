using System.Globalization;
using Avalonia.Data.Converters;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class LanguageConverter : IValueConverter
{
    public static LanguageConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Language language)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return LanguageFormatter.Format(language);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
