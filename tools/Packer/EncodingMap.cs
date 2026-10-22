namespace Packer;

internal static class EncodingMap
{
    public static EncodingType GetEncoding(AssetKey key)
    {
        return key switch
        {
            AssetAnimationKey animation => GetAnimationEncoding(animation),
            AssetSpriteKey sprite => GetSpriteEncoding(sprite),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static EncodingType GetSpriteEncoding(AssetSpriteKey key)
    {
        return key switch
        {
            AssetSpriteKey.Bag => EncodingType.Palette,
            AssetSpriteKey.BattleBackground => EncodingType.None,
            AssetSpriteKey.BattleCorpse => EncodingType.Palette,
            AssetSpriteKey.BuildingIcon => EncodingType.Palette,
            AssetSpriteKey.CapitalBackground => EncodingType.None,
            AssetSpriteKey.CityShield => EncodingType.Palette,
            AssetSpriteKey.Ground => EncodingType.Palette,
            AssetSpriteKey.GroundBorder => EncodingType.Palette,
            AssetSpriteKey.ItemIcon => EncodingType.None,
            AssetSpriteKey.LordFace => EncodingType.None,
            AssetSpriteKey.MainMenuBackground => EncodingType.None,
            AssetSpriteKey.MainMenuBackgroundV2 => EncodingType.None,
            AssetSpriteKey.Mountain => EncodingType.Palette,
            AssetSpriteKey.RaceSelectionBackground => EncodingType.None,
            AssetSpriteKey.Road => EncodingType.Palette,
            AssetSpriteKey.Rod => EncodingType.Palette,
            AssetSpriteKey.RuinBanner => EncodingType.Palette,
            AssetSpriteKey.CityImage => EncodingType.None,
            AssetSpriteKey.CapitalImage => EncodingType.None,
            AssetSpriteKey.SiteImage => EncodingType.None,
            AssetSpriteKey.SpellIcon => EncodingType.None,
            AssetSpriteKey.StackBanner => EncodingType.Palette,
            AssetSpriteKey.ThiefActionImage => EncodingType.None,
            AssetSpriteKey.Tombstone => EncodingType.Palette,
            AssetSpriteKey.Tree => EncodingType.Palette,
            AssetSpriteKey.UnitEventPortrait => EncodingType.None,
            AssetSpriteKey.UnitFace => EncodingType.None,
            AssetSpriteKey.UnitPortrait => EncodingType.None,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static EncodingType GetAnimationEncoding(AssetAnimationKey key)
    {
        return key switch
        {
            AssetAnimationKey.BattleEffect.Attack => EncodingType.Lossy,
            AssetAnimationKey.BattleEffect.Blister => EncodingType.Lossy,
            AssetAnimationKey.BattleEffect.Death => EncodingType.Lossy,
            AssetAnimationKey.BattleEffect.Heal => EncodingType.Lossy,
            AssetAnimationKey.BattleEffect.Item => EncodingType.Lossy,
            AssetAnimationKey.BattleEffect.LevelUp => EncodingType.Lossy,
            AssetAnimationKey.BattleEffect.Status => EncodingType.Lossy,
            AssetAnimationKey.BattleUnit => EncodingType.Lossy,
            AssetAnimationKey.BattleUnitAura => EncodingType.Lossy,
            AssetAnimationKey.BattleUnitShadow => EncodingType.Palette,
            AssetAnimationKey.Capital => EncodingType.Palette,
            AssetAnimationKey.BattleBackgroundOverlay => EncodingType.None,
            AssetAnimationKey.CapitalBackgroundOverlay => EncodingType.None,
            AssetAnimationKey.CapitalBuilding => EncodingType.None,
            AssetAnimationKey.City => EncodingType.Palette,
            AssetAnimationKey.CityShadow => EncodingType.Palette,
            AssetAnimationKey.Cloud => EncodingType.Palette,
            AssetAnimationKey.Crystal => EncodingType.Palette,
            AssetAnimationKey.Cursor => EncodingType.None,
            AssetAnimationKey.Landmark => EncodingType.Palette,
            AssetAnimationKey.MainMenuBackgroundOverlay => EncodingType.None,
            AssetAnimationKey.MountainRemoveEffect => EncodingType.None,
            AssetAnimationKey.RaceSelectionBackgroundOverlay => EncodingType.None,
            AssetAnimationKey.Ruin => EncodingType.Palette,
            AssetAnimationKey.Site => EncodingType.Palette,
            AssetAnimationKey.SiteBanner => EncodingType.Palette,
            AssetAnimationKey.SiteShadow => EncodingType.Palette,
            AssetAnimationKey.StackBattleEffect => EncodingType.None,
            AssetAnimationKey.StackBoat => EncodingType.Palette,
            AssetAnimationKey.StackBoatShadow => EncodingType.Palette,
            AssetAnimationKey.StackCreateEffect => EncodingType.None,
            AssetAnimationKey.StackDestroyEffect => EncodingType.None,
            AssetAnimationKey.StackOwnerChangeEffect => EncodingType.None,
            AssetAnimationKey.StackSummonDestroyEffect => EncodingType.None,
            AssetAnimationKey.StackUnit => EncodingType.Palette,
            AssetAnimationKey.StackUnitAura => EncodingType.None,
            AssetAnimationKey.StackUnitShadow => EncodingType.Palette,
            AssetAnimationKey.Water => EncodingType.Palette,
            AssetAnimationKey.WaterLandmark => EncodingType.Palette,
            AssetAnimationKey.WorldSpellEffect => EncodingType.Lossy,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
}
