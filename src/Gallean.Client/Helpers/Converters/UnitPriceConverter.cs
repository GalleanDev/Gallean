using System.Globalization;
using Avalonia.Data.Converters;

namespace Gallean.Client;

internal sealed class UnitPriceConverter : IMultiValueConverter
{
    public static UnitPriceConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is [WorldUnitId unitId, IReadOnlyDictionary<WorldUnitId, ResourceSetViewModel> prices])
        {
            return prices.GetValueOrDefault(unitId);
        }
        else
        {
            return null;
        }
    }
}
