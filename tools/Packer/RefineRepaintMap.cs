using Gallean.Engine.Contracts;

namespace Packer;

internal static class RefineRepaintMap
{
    private static readonly IReadOnlyList<RefineRegion> MainMenuInterface = [new(470, 0, 330, 428)];

    private static readonly IReadOnlyList<RefineRegion> RaceSelectionInterface =
    [
        new(316, 505, 161, 34),
        new(230, 533, 330, 67),
        new(130, 548, 532, 45)
    ];

    public static IReadOnlyList<RefineRegion> GetRegions(AssetKey key)
    {
        return key switch
        {
            AssetAnimationKey => [],
            AssetSpriteKey sprite => GetSpriteRegions(sprite),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static IReadOnlyList<RefineRegion> GetSpriteRegions(AssetSpriteKey key)
    {
        return key switch
        {
            AssetSpriteKey.Bag => [],
            AssetSpriteKey.BattleBackground => [],
            AssetSpriteKey.BattleCorpse => [],
            AssetSpriteKey.BuildingIcon => [],
            AssetSpriteKey.CapitalBackground => [],
            AssetSpriteKey.CityShield => [],
            AssetSpriteKey.Ground => [],
            AssetSpriteKey.GroundBorder => [],
            AssetSpriteKey.ItemIcon => [],
            AssetSpriteKey.LordFace => [],
            AssetSpriteKey.MainMenuBackground => MainMenuInterface,
            AssetSpriteKey.MainMenuBackgroundV2 => [],
            AssetSpriteKey.Mountain => [],
            AssetSpriteKey.RaceSelectionBackground background => GetRaceSelectionRegions(background.Race),
            AssetSpriteKey.Road => [],
            AssetSpriteKey.Rod => [],
            AssetSpriteKey.RuinBanner => [],
            AssetSpriteKey.CityImage => [],
            AssetSpriteKey.CapitalImage => [],
            AssetSpriteKey.SiteImage => [],
            AssetSpriteKey.SpellIcon => [],
            AssetSpriteKey.StackBanner => [],
            AssetSpriteKey.ThiefActionImage => [],
            AssetSpriteKey.Tombstone => [],
            AssetSpriteKey.Tree => [],
            AssetSpriteKey.UnitEventPortrait => [],
            AssetSpriteKey.UnitFace => [],
            AssetSpriteKey.UnitPortrait => [],
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static IReadOnlyList<RefineRegion> GetRaceSelectionRegions(WorldRace race)
    {
        return race switch
        {
            WorldRace.Human => RaceSelectionInterface,
            WorldRace.Undead => RaceSelectionInterface,
            WorldRace.Heretic => RaceSelectionInterface,
            WorldRace.Dwarf => RaceSelectionInterface,
            WorldRace.Neutral => [],
            WorldRace.Elf => []
        };
    }
}
