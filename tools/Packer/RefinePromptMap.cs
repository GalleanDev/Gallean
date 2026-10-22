using System.Collections.Frozen;
using Gallean.Engine.Contracts;

namespace Packer;

internal static class RefinePromptMap
{
    private static readonly FrozenSet<AssetAnimationKey.BattleUnitAura> BattleUnitAuras =
    [
        new(new UnitDefinitionId("g000uu8004"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker)
    ];

    private static readonly FrozenSet<AssetAnimationKey.CapitalBuilding> CapitalBuildings =
    [
        new(new WorldPlayerBuildingDefinitionId("g000bb0004")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0063")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0072")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0100")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0101")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0103")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0109")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0116")),
        new(new WorldPlayerBuildingDefinitionId("g000bb0120"))
    ];

    public static RefinePrompt? GetPrompt(AssetKey key)
    {
        return key switch
        {
            AssetAnimationKey.BattleUnit unit => new RefinePrompt(new RefinePromptId($"battle-unit/{unit.DefinitionId.Value}/{unit.Side}".ToLowerInvariant()), []),
            AssetAnimationKey.BattleBackgroundOverlay => null,
            AssetAnimationKey.BattleEffect.Attack => null,
            AssetAnimationKey.BattleEffect.Blister => null,
            AssetAnimationKey.BattleEffect.Death => null,
            AssetAnimationKey.BattleEffect.Heal => null,
            AssetAnimationKey.BattleEffect.Item => null,
            AssetAnimationKey.BattleEffect.LevelUp => null,
            AssetAnimationKey.BattleEffect.Status => null,
            AssetAnimationKey.BattleUnitAura aura when BattleUnitAuras.Contains(aura) => new RefinePrompt(new RefinePromptId(aura.CanonicalId), []),
            AssetAnimationKey.BattleUnitAura => null,
            AssetAnimationKey.BattleUnitShadow => null,
            AssetAnimationKey.Capital => null,
            AssetAnimationKey.CapitalBackgroundOverlay => null,
            AssetAnimationKey.CapitalBuilding building when CapitalBuildings.Contains(building) => new RefinePrompt(new RefinePromptId(building.CanonicalId), []),
            AssetAnimationKey.CapitalBuilding => null,
            AssetAnimationKey.City => null,
            AssetAnimationKey.CityShadow => null,
            AssetAnimationKey.Cloud => null,
            AssetAnimationKey.Crystal => null,
            AssetAnimationKey.Cursor cursor => new RefinePrompt(new RefinePromptId(cursor.CanonicalId), []),
            AssetAnimationKey.Landmark => null,
            AssetAnimationKey.MainMenuBackgroundOverlay => null,
            AssetAnimationKey.MountainRemoveEffect => null,
            AssetAnimationKey.RaceSelectionBackgroundOverlay => null,
            AssetAnimationKey.Ruin => null,
            AssetAnimationKey.Site => null,
            AssetAnimationKey.SiteBanner => null,
            AssetAnimationKey.SiteShadow => null,
            AssetAnimationKey.StackBattleEffect => null,
            AssetAnimationKey.StackBoat => null,
            AssetAnimationKey.StackBoatShadow => null,
            AssetAnimationKey.StackCreateEffect => null,
            AssetAnimationKey.StackDestroyEffect => null,
            AssetAnimationKey.StackOwnerChangeEffect => null,
            AssetAnimationKey.StackSummonDestroyEffect => null,
            AssetAnimationKey.StackUnit => null,
            AssetAnimationKey.StackUnitAura => null,
            AssetAnimationKey.StackUnitShadow => null,
            AssetAnimationKey.Water => null,
            AssetAnimationKey.WaterLandmark => null,
            AssetAnimationKey.WorldSpellEffect => null,
            AssetSpriteKey.Ground => null,
            AssetSpriteKey.GroundBorder => null,
            AssetSpriteKey.Tree => null,
            AssetSpriteKey.Mountain => null,
            AssetSpriteKey.Bag => null,
            AssetSpriteKey.Tombstone => null,
            AssetSpriteKey.CapitalBackground => null,
            AssetSpriteKey.MainMenuBackground background => new RefinePrompt(new RefinePromptId(background.CanonicalId), []),
            AssetSpriteKey.MainMenuBackgroundV2 => null,
            AssetSpriteKey.RaceSelectionBackground background => GetRaceSelectionPrompt(background),
            AssetSpriteKey.BattleBackground => null,
            AssetSpriteKey.BattleCorpse => new RefinePrompt(new RefinePromptId("battle-corpse"), []),
            AssetSpriteKey.UnitFace => null,
            AssetSpriteKey.UnitPortrait => null,
            AssetSpriteKey.UnitEventPortrait => null,
            AssetSpriteKey.SpellIcon => null,
            AssetSpriteKey.BuildingIcon => null,
            AssetSpriteKey.ItemIcon => null,
            AssetSpriteKey.ThiefActionImage => null,
            AssetSpriteKey.CityImage => null,
            AssetSpriteKey.CapitalImage => null,
            AssetSpriteKey.SiteImage => null,
            AssetSpriteKey.LordFace => null,
            AssetSpriteKey.StackBanner => null,
            AssetSpriteKey.CityShield => null,
            AssetSpriteKey.RuinBanner => null,
            AssetSpriteKey.Rod => null,
            AssetSpriteKey.Road => null,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static RefinePrompt GetRaceSelectionPrompt(AssetSpriteKey.RaceSelectionBackground background)
    {
        RefinePromptId id = new(background.CanonicalId);

        return background.Race switch
        {
            WorldRace.Human => new RefinePrompt(id, [GetSkullRegion(background)]),
            WorldRace.Undead => new RefinePrompt(id, [GetSkullRegion(background)]),
            WorldRace.Heretic => new RefinePrompt(id, [GetSkullRegion(background)]),
            WorldRace.Dwarf => new RefinePrompt(id, [GetSkullRegion(background)]),
            WorldRace.Neutral => new RefinePrompt(id, []),
            WorldRace.Elf => new RefinePrompt(id, [])
        };
    }

    private static RefinePromptRegion GetSkullRegion(AssetSpriteKey.RaceSelectionBackground background)
    {
        return new RefinePromptRegion(new RefineRegion(544, 496, 40, 60), new RefinePromptId($"{background.CanonicalId}/skull"));
    }
}
