using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class LanguageIconConverter : IValueConverter
{
    public static LanguageIconConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Language language)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return LanguageIconResolver.Resolve(language);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
