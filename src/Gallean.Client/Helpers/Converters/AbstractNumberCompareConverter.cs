using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal abstract class AbstractNumberCompareConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IComparable number)
        {
            object compareTo = System.Convert.ChangeType(parameter, value.GetType(), CultureInfo.InvariantCulture)!;

            return Matches(number.CompareTo(compareTo));
        }
        else
        {
            return false;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    protected abstract bool Matches(int value);
}
