namespace Packer;

internal static class EnhancementMap
{
    public static readonly Guid Refine = new("2ed23ad3-f09c-477f-b3e3-0ce3bb53bbe6");
    public static readonly Guid Alpha = new("9425579e-408f-470d-a0e3-ba4a0375cff7");

    public static IReadOnlyList<EnhancementType> GetEnhancements(AssetKey key)
    {
        return key switch
        {
            AssetAnimationKey animation => GetAnimationEnhancements(animation),
            AssetSpriteKey sprite => GetSpriteEnhancements(sprite),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    public static IReadOnlyList<EnhancementType> GetOutputEnhancements(
        IReadOnlyList<EnhancementType> enhancements,
        EnhancementType enhancement)
    {
        int index = FindIndex(enhancements, enhancement);

        return [.. enhancements.Take(index + 1)];
    }

    public static IReadOnlyList<EnhancementType> GetInputEnhancements(
        IReadOnlyList<EnhancementType> enhancements,
        EnhancementType enhancement)
    {
        int index = FindIndex(enhancements, enhancement);

        return [.. enhancements.Take(index)];
    }

    private static int FindIndex(IReadOnlyList<EnhancementType> enhancements, EnhancementType enhancement)
    {
        for (int index = 0; index < enhancements.Count; index++)
        {
            if (enhancements[index] == enhancement)
            {
                return index;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(enhancement), enhancement, null);
    }

    private static IReadOnlyList<EnhancementType> GetSpriteEnhancements(AssetSpriteKey key)
    {
        return key switch
        {
            AssetSpriteKey.Bag => [EnhancementType.Upscale],
            AssetSpriteKey.BattleBackground background when RefineMap.CanRefine(background) => [EnhancementType.Refine],
            AssetSpriteKey.BattleBackground => [],
            AssetSpriteKey.BattleCorpse corpse when RefineMap.CanRefine(corpse) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface],
            AssetSpriteKey.BattleCorpse => [EnhancementType.Upscale],
            AssetSpriteKey.BuildingIcon => [EnhancementType.Upscale],
            AssetSpriteKey.CapitalBackground background when RefineMap.CanRefine(background) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetSpriteKey.CapitalBackground => [EnhancementType.Upscale],
            AssetSpriteKey.CityShield => [EnhancementType.Upscale],
            AssetSpriteKey.Ground => [EnhancementType.Upscale],
            AssetSpriteKey.GroundBorder border => border.Set switch
            {
                AssetSpriteKey.GroundBorderSet.Mask => [],
                AssetSpriteKey.GroundBorderSet.Coast => [EnhancementType.Upscale]
            },
            AssetSpriteKey.ItemIcon => [EnhancementType.Upscale],
            AssetSpriteKey.LordFace => [EnhancementType.Upscale],
            AssetSpriteKey.MainMenuBackground background when RefineMap.CanRefine(background) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetSpriteKey.MainMenuBackground => [EnhancementType.Upscale],
            AssetSpriteKey.MainMenuBackgroundV2 => [],
            AssetSpriteKey.Mountain => [EnhancementType.Upscale],
            AssetSpriteKey.RaceSelectionBackground background when RefineMap.CanRefine(background) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetSpriteKey.RaceSelectionBackground => [EnhancementType.Upscale],
            AssetSpriteKey.Road => [EnhancementType.Upscale],
            AssetSpriteKey.Rod => [EnhancementType.Upscale],
            AssetSpriteKey.RuinBanner => [EnhancementType.Upscale],
            AssetSpriteKey.CityImage => [EnhancementType.Upscale],
            AssetSpriteKey.CapitalImage => [EnhancementType.Upscale],
            AssetSpriteKey.SiteImage => [EnhancementType.Upscale],
            AssetSpriteKey.SpellIcon => [EnhancementType.Upscale],
            AssetSpriteKey.StackBanner => [EnhancementType.Upscale],
            AssetSpriteKey.ThiefActionImage => [EnhancementType.Upscale],
            AssetSpriteKey.Tombstone => [EnhancementType.Upscale],
            AssetSpriteKey.Tree => [EnhancementType.Upscale],
            AssetSpriteKey.UnitEventPortrait => [EnhancementType.Upscale],
            AssetSpriteKey.UnitFace => [EnhancementType.Upscale],
            AssetSpriteKey.UnitPortrait => [EnhancementType.Upscale],
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static IReadOnlyList<EnhancementType> GetAnimationEnhancements(AssetAnimationKey key)
    {
        return key switch
        {
            AssetAnimationKey.BattleEffect.Attack attack => attack.Layer switch
            {
                AssetAnimationKey.BattleLayer.Shadow => [EnhancementType.Upscale],
                AssetAnimationKey.BattleLayer.Body when RefineMap.CanRefine(attack) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
                AssetAnimationKey.BattleLayer.Body => [EnhancementType.Upscale],
                AssetAnimationKey.BattleLayer.Aura when RefineMap.CanRefine(attack) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
                AssetAnimationKey.BattleLayer.Aura => [EnhancementType.Upscale]
            },
            AssetAnimationKey.BattleEffect.Blister blister when RefineMap.CanRefine(blister) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.BattleEffect.Blister => [EnhancementType.Upscale],
            AssetAnimationKey.BattleEffect.Death death when RefineMap.CanRefine(death) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.BattleEffect.Death => [EnhancementType.Upscale],
            AssetAnimationKey.BattleEffect.Heal heal when RefineMap.CanRefine(heal) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.BattleEffect.Heal => [EnhancementType.Upscale],
            AssetAnimationKey.BattleEffect.Item item when RefineMap.CanRefine(item) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.BattleEffect.Item => [EnhancementType.Upscale],
            AssetAnimationKey.BattleEffect.LevelUp levelUp when RefineMap.CanRefine(levelUp) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.BattleEffect.LevelUp => [EnhancementType.Upscale],
            AssetAnimationKey.BattleEffect.Status status when RefineMap.CanRefine(status) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.BattleEffect.Status => [EnhancementType.Upscale],
            AssetAnimationKey.BattleUnit unit when RefineMap.CanRefine(unit) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface],
            AssetAnimationKey.BattleUnit => [EnhancementType.Upscale],
            AssetAnimationKey.BattleUnitAura aura when RefineMap.CanRefine(aura) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.BattleUnitAura => [EnhancementType.Upscale],
            AssetAnimationKey.BattleUnitShadow => [EnhancementType.Upscale],
            AssetAnimationKey.Capital => [EnhancementType.Upscale],
            AssetAnimationKey.BattleBackgroundOverlay => [],
            AssetAnimationKey.CapitalBackgroundOverlay overlay when RefineMap.CanRefine(overlay) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.CapitalBackgroundOverlay => [EnhancementType.Upscale],
            AssetAnimationKey.CapitalBuilding building when RefineMap.CanRefine(building) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.CapitalBuilding => [EnhancementType.Upscale],
            AssetAnimationKey.City => [EnhancementType.Upscale],
            AssetAnimationKey.CityShadow => [EnhancementType.Upscale],
            AssetAnimationKey.Cloud => [EnhancementType.Upscale],
            AssetAnimationKey.Crystal => [EnhancementType.Upscale],
            AssetAnimationKey.Cursor cursor when RefineMap.CanRefine(cursor) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.Cursor => [EnhancementType.Upscale],
            AssetAnimationKey.Landmark => [EnhancementType.Upscale],
            AssetAnimationKey.MainMenuBackgroundOverlay overlay when RefineMap.CanRefine(overlay) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.MainMenuBackgroundOverlay => [EnhancementType.Upscale],
            AssetAnimationKey.MountainRemoveEffect => [EnhancementType.Upscale],
            AssetAnimationKey.RaceSelectionBackgroundOverlay overlay when RefineMap.CanRefine(overlay) => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha],
            AssetAnimationKey.RaceSelectionBackgroundOverlay => [EnhancementType.Upscale],
            AssetAnimationKey.Ruin => [EnhancementType.Upscale],
            AssetAnimationKey.Site => [EnhancementType.Upscale],
            AssetAnimationKey.SiteBanner => [EnhancementType.Upscale],
            AssetAnimationKey.SiteShadow => [EnhancementType.Upscale],
            AssetAnimationKey.StackBattleEffect => [EnhancementType.Upscale],
            AssetAnimationKey.StackBoat => [EnhancementType.Upscale],
            AssetAnimationKey.StackBoatShadow => [EnhancementType.Upscale],
            AssetAnimationKey.StackCreateEffect => [EnhancementType.Upscale],
            AssetAnimationKey.StackDestroyEffect => [EnhancementType.Upscale],
            AssetAnimationKey.StackOwnerChangeEffect => [EnhancementType.Upscale],
            AssetAnimationKey.StackSummonDestroyEffect => [EnhancementType.Upscale],
            AssetAnimationKey.StackUnit => [EnhancementType.Upscale],
            AssetAnimationKey.StackUnitAura => [EnhancementType.Upscale],
            AssetAnimationKey.StackUnitShadow => [EnhancementType.Upscale],
            AssetAnimationKey.Water => [],
            AssetAnimationKey.WaterLandmark => [EnhancementType.Upscale],
            AssetAnimationKey.WorldSpellEffect => [EnhancementType.Upscale],
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
}
