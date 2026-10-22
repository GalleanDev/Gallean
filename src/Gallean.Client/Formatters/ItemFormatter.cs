using Gallean.Engine;

namespace Gallean.Client;

internal static class ItemFormatter
{
    public static string FormatName(
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        ItemDefinitionId definitionId,
        int? charges)
    {
        string name = texts.GetGlobalText(definitions.GetItem(definitionId).NameTextId).Text;

        if (charges is null)
        {
            return name;
        }
        else
        {
            return $"{name} ({charges})";
        }
    }

    public static string FormatDescription(
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        ItemDefinitionId definitionId)
    {
        return texts.GetGlobalText(definitions.GetItem(definitionId).DescriptionTextId).Text;
    }

    public static string FormatCategory(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Armor => Tr("Armor"),
            ItemCategory.Jewel => Tr("Jewel"),
            ItemCategory.Weapon => Tr("Weapon"),
            ItemCategory.Banner => Tr("Banner"),
            ItemCategory.PotionTemporaryBoost => Tr("Potion"),
            ItemCategory.PotionHeal => Tr("Healing potion"),
            ItemCategory.PotionRevive => Tr("Revival potion"),
            ItemCategory.PotionPermanentBoost => Tr("Elixir"),
            ItemCategory.Scroll => Tr("Scroll"),
            ItemCategory.Wand => Tr("Staff"),
            ItemCategory.Valuable => Tr("Valuable"),
            ItemCategory.Orb => Tr("Orb"),
            ItemCategory.Talisman => Tr("Talisman"),
            ItemCategory.TravelItem => Tr("Travel item"),
            ItemCategory.Special => Tr("Special item")
        };
    }
}
