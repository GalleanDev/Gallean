using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gallean.Client;

internal sealed class HitpointsBrushConverter : IMultiValueConverter
{
    private static readonly IBrush HealthyBrush = new ImmutableSolidColorBrush(Theme.UnitPortrait.HitpointsHealthyColor);
    private static readonly IBrush WoundedBrush = new ImmutableSolidColorBrush(Theme.UnitPortrait.HitpointsWoundedColor);
    private static readonly IBrush CriticalBrush = new ImmutableSolidColorBrush(Theme.UnitPortrait.HitpointsCriticalColor);

    public static HitpointsBrushConverter Instance { get; } = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [double hitpoints, double maxHitpoints])
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (hitpoints * 100 <= maxHitpoints * 25)
        {
            return CriticalBrush;
        }
        else if (hitpoints * 100 <= maxHitpoints * 50)
        {
            return WoundedBrush;
        }
        else
        {
            return HealthyBrush;
        }
    }
}
