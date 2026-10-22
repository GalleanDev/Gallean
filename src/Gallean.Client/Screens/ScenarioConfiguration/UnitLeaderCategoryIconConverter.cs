using System.Globalization;
using Avalonia.Data.Converters;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class UnitLeaderCategoryIconConverter : IValueConverter
{
    public static UnitLeaderCategoryIconConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UnitLeaderCategory category)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return category switch
        {
            UnitLeaderCategory.Fighter => IconMoniker.Damage,
            UnitLeaderCategory.Explorer => IconMoniker.Boot,
            UnitLeaderCategory.Mage => IconMoniker.AbilityTalismanUse,
            UnitLeaderCategory.Rod => IconMoniker.AbilityRod,
            UnitLeaderCategory.Thief => IconMoniker.ThiefSpy
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
