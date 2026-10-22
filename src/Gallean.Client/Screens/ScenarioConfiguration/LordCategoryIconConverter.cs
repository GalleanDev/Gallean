using System.Globalization;
using Avalonia.Data.Converters;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class LordCategoryIconConverter : IValueConverter
{
    public static LordCategoryIconConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LordCategory category)
        {
            throw new ArgumentException("Unexpected value type.", nameof(value));
        }

        return category switch
        {
            LordCategory.Warrior => IconMoniker.AbilityWeaponMaster,
            LordCategory.Mage => IconMoniker.Spellbook,
            LordCategory.Guildmaster => IconMoniker.ThiefSpy
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
