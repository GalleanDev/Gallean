namespace Packer;

internal static class UpscaleMap
{
    private static readonly Guid TransformVsr = new("7638e1d7-13d7-4ed4-abbb-d514797df049");
    private static readonly Guid TransformMosr = new("b3d1f5a7-6c24-4e93-8f70-2a5c9de41b68");

    public static Guid GetTransform(UpscalerType upscaler)
    {
        return upscaler switch
        {
            UpscalerType.Vsr => TransformVsr,
            UpscalerType.Mosr => TransformMosr
        };
    }

    public static UpscalerType GetUpscaler(AssetKey key)
    {
        return key switch
        {
            AssetAnimationKey animation => GetAnimationUpscaler(animation),
            AssetSpriteKey sprite => GetSpriteUpscaler(sprite),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static UpscalerType GetSpriteUpscaler(AssetSpriteKey key)
    {
        return key switch
        {
            AssetSpriteKey.Bag => UpscalerType.Mosr,
            AssetSpriteKey.BattleBackground => UpscalerType.Mosr,
            AssetSpriteKey.BattleCorpse => UpscalerType.Mosr,
            AssetSpriteKey.BuildingIcon => UpscalerType.Mosr,
            AssetSpriteKey.CapitalBackground => UpscalerType.Mosr,
            AssetSpriteKey.CityShield => UpscalerType.Mosr,
            AssetSpriteKey.Ground => UpscalerType.Mosr,
            AssetSpriteKey.GroundBorder => UpscalerType.Mosr,
            AssetSpriteKey.ItemIcon => UpscalerType.Mosr,
            AssetSpriteKey.LordFace => UpscalerType.Mosr,
            AssetSpriteKey.MainMenuBackground => UpscalerType.Vsr,
            AssetSpriteKey.MainMenuBackgroundV2 => UpscalerType.Mosr,
            AssetSpriteKey.Mountain => UpscalerType.Mosr,
            AssetSpriteKey.RaceSelectionBackground => UpscalerType.Vsr,
            AssetSpriteKey.Road => UpscalerType.Mosr,
            AssetSpriteKey.Rod => UpscalerType.Mosr,
            AssetSpriteKey.RuinBanner => UpscalerType.Mosr,
            AssetSpriteKey.CityImage => UpscalerType.Mosr,
            AssetSpriteKey.CapitalImage => UpscalerType.Mosr,
            AssetSpriteKey.SiteImage => UpscalerType.Mosr,
            AssetSpriteKey.SpellIcon => UpscalerType.Mosr,
            AssetSpriteKey.StackBanner => UpscalerType.Mosr,
            AssetSpriteKey.ThiefActionImage => UpscalerType.Mosr,
            AssetSpriteKey.Tombstone => UpscalerType.Mosr,
            AssetSpriteKey.Tree => UpscalerType.Mosr,
            AssetSpriteKey.UnitEventPortrait => UpscalerType.Mosr,
            AssetSpriteKey.UnitFace => UpscalerType.Mosr,
            AssetSpriteKey.UnitPortrait => UpscalerType.Mosr,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static UpscalerType GetAnimationUpscaler(AssetAnimationKey key)
    {
        return key switch
        {
            AssetAnimationKey.BattleEffect.Attack => UpscalerType.Vsr,
            AssetAnimationKey.BattleEffect.Blister => UpscalerType.Vsr,
            AssetAnimationKey.BattleEffect.Death => UpscalerType.Vsr,
            AssetAnimationKey.BattleEffect.Heal => UpscalerType.Vsr,
            AssetAnimationKey.BattleEffect.Item => UpscalerType.Vsr,
            AssetAnimationKey.BattleEffect.LevelUp => UpscalerType.Vsr,
            AssetAnimationKey.BattleEffect.Status => UpscalerType.Vsr,
            AssetAnimationKey.BattleUnit => UpscalerType.Vsr,
            AssetAnimationKey.BattleUnitAura => UpscalerType.Vsr,
            AssetAnimationKey.BattleUnitShadow => UpscalerType.Mosr,
            AssetAnimationKey.Capital => UpscalerType.Mosr,
            AssetAnimationKey.BattleBackgroundOverlay => UpscalerType.Mosr,
            AssetAnimationKey.CapitalBackgroundOverlay => UpscalerType.Vsr,
            AssetAnimationKey.CapitalBuilding => UpscalerType.Vsr,
            AssetAnimationKey.City => UpscalerType.Mosr,
            AssetAnimationKey.CityShadow => UpscalerType.Mosr,
            AssetAnimationKey.Cloud => UpscalerType.Mosr,
            AssetAnimationKey.Crystal => UpscalerType.Mosr,
            AssetAnimationKey.Cursor => UpscalerType.Vsr,
            AssetAnimationKey.Landmark => UpscalerType.Mosr,
            AssetAnimationKey.MainMenuBackgroundOverlay => UpscalerType.Vsr,
            AssetAnimationKey.MountainRemoveEffect => UpscalerType.Mosr,
            AssetAnimationKey.RaceSelectionBackgroundOverlay => UpscalerType.Vsr,
            AssetAnimationKey.Ruin => UpscalerType.Mosr,
            AssetAnimationKey.Site => UpscalerType.Mosr,
            AssetAnimationKey.SiteBanner => UpscalerType.Mosr,
            AssetAnimationKey.SiteShadow => UpscalerType.Mosr,
            AssetAnimationKey.StackBattleEffect => UpscalerType.Mosr,
            AssetAnimationKey.StackBoat => UpscalerType.Mosr,
            AssetAnimationKey.StackBoatShadow => UpscalerType.Mosr,
            AssetAnimationKey.StackCreateEffect => UpscalerType.Mosr,
            AssetAnimationKey.StackDestroyEffect => UpscalerType.Mosr,
            AssetAnimationKey.StackOwnerChangeEffect => UpscalerType.Mosr,
            AssetAnimationKey.StackSummonDestroyEffect => UpscalerType.Mosr,
            AssetAnimationKey.StackUnit => UpscalerType.Mosr,
            AssetAnimationKey.StackUnitAura => UpscalerType.Mosr,
            AssetAnimationKey.StackUnitShadow => UpscalerType.Mosr,
            AssetAnimationKey.Water => UpscalerType.Mosr,
            AssetAnimationKey.WaterLandmark => UpscalerType.Mosr,
            AssetAnimationKey.WorldSpellEffect => UpscalerType.Mosr,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
}
