using Gallean.Engine;

namespace Gallean.Client;

internal static class ItemSoundResolver
{
    public static AssetSoundKey ResolveWorld(ItemCategory category)
    {
        AssetSoundKey.BattleEffect.PotionType? potion = GetPotionType(category);

        if (potion is not null)
        {
            return new AssetSoundKey.BattleEffect.Potion(potion.Value);
        }
        else
        {
            return new AssetSoundKey.ItemUse();
        }
    }

    public static AssetSoundKey.BattleEffect.Potion? ResolveBattle(ItemCategory category)
    {
        AssetSoundKey.BattleEffect.PotionType? potion = GetPotionType(category);

        if (potion is not null)
        {
            return new AssetSoundKey.BattleEffect.Potion(potion.Value);
        }
        else
        {
            return null;
        }
    }

    private static AssetSoundKey.BattleEffect.PotionType? GetPotionType(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Armor => null,
            ItemCategory.Jewel => null,
            ItemCategory.Weapon => null,
            ItemCategory.Banner => null,
            ItemCategory.PotionTemporaryBoost => AssetSoundKey.BattleEffect.PotionType.Boost,
            ItemCategory.PotionHeal => AssetSoundKey.BattleEffect.PotionType.Heal,
            ItemCategory.PotionRevive => AssetSoundKey.BattleEffect.PotionType.Revive,
            ItemCategory.PotionPermanentBoost => AssetSoundKey.BattleEffect.PotionType.Boost,
            ItemCategory.Scroll => null,
            ItemCategory.Wand => null,
            ItemCategory.Valuable => null,
            ItemCategory.Orb => null,
            ItemCategory.Talisman => null,
            ItemCategory.TravelItem => null,
            ItemCategory.Special => null
        };
    }
}
