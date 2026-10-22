using Gallean.Engine;

namespace Gallean.Client;

internal static class LordCategoryFormatter
{
    public static string FormatTitle(LordCategory category)
    {
        return category switch
        {
            LordCategory.Warrior => Tr("Warrior Lord"),
            LordCategory.Mage => Tr("Mage Lord"),
            LordCategory.Guildmaster => Tr("Guildmaster")
        };
    }

    public static string FormatDescription(LordCategory category)
    {
        return category switch
        {
            LordCategory.Warrior => Tr("The Warrior Lord's units recover 15% of their lost hit points each day, but spell research is limited to level four."),
            LordCategory.Mage => Tr("The Mage Lord can research every spell at half cost and cast spells twice per day."),
            LordCategory.Guildmaster => Tr("The Guildmaster's spell research is limited to level four, but thieves can perform more actions and cities cost 50% less to upgrade.")
        };
    }
}
