using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GamePortraitSource(
    GameImages faces,
    GameImages events,
    GameImages lordFaces,
    GameImages thiefActions,
    GameGraphicsResolver graphicsResolver)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors(
        IReadOnlyCollection<UnitDefinitionId> unitIds,
        IReadOnlyCollection<ScenarioEventPortraitId> eventPortraitIds)
    {
        HashSet<ScenarioEventPortraitId> portraitIds = [];

        foreach (UnitDefinitionId unitId in unitIds)
        {
            portraitIds.Add(ScenarioEventPortraitId.Unit(unitId));
        }

        foreach (UnitDefinitionId unitId in unitIds)
        {
            AssetSpriteKey.UnitFace face = new(unitId);
            AssetSpriteKey.UnitPortrait portrait = new(unitId);
            AssetSpriteKey.UnitEventPortrait eventPortrait = new(ScenarioEventPortraitId.Unit(unitId));

            yield return RequiredSprite(face, source => source.Portraits.GetUnitFace(face.DefinitionId));
            yield return RequiredSprite(portrait, source => source.Portraits.GetUnitPortrait(portrait.DefinitionId));
            yield return OptionalSprite(eventPortrait, source => source.Portraits.GetUnitEventPortrait(eventPortrait.PortraitId));
        }

        foreach (ScenarioEventPortraitId portraitId in eventPortraitIds)
        {
            if (portraitIds.Add(portraitId))
            {
                AssetSpriteKey.UnitEventPortrait key = new(portraitId);

                yield return OptionalSprite(key, source => source.Portraits.GetUnitEventPortrait(key.PortraitId));
            }
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            for (int variant = 0; variant < AssetSpriteKey.LordFace.GetVariantCount(race); variant++)
            {
                AssetSpriteKey.LordFace small = new(race, variant, IsLarge: false);
                AssetSpriteKey.LordFace large = new(race, variant, IsLarge: true);

                yield return OptionalSprite(small, source => source.Portraits.GetLordFace(small.Race, small.Variant, small.IsLarge));
                yield return OptionalSprite(large, source => source.Portraits.GetLordFace(large.Race, large.Variant, large.IsLarge));
            }
        }

        foreach (WorldThiefActionType type in Enum.GetValues<WorldThiefActionType>())
        {
            AssetSpriteKey.ThiefActionImage small = new(type, IsLarge: false);
            AssetSpriteKey.ThiefActionImage large = new(type, IsLarge: true);

            yield return RequiredSprite(small, source => source.Portraits.GetThiefActionImage(small.Type, small.IsLarge));
            yield return RequiredSprite(large, source => source.Portraits.GetThiefActionImage(large.Type, large.IsLarge));
        }
    }

    private AssetSprite? GetUnitFace(UnitDefinitionId unitId)
    {
        UnitDefinitionId graphicsId = graphicsResolver.ResolveUnitFaceGraphicsId(unitId);

        return faces.TryGetSprite(Portraits.UnitFace(graphicsId.Value));
    }

    private AssetSprite? GetUnitPortrait(UnitDefinitionId unitId)
    {
        UnitDefinitionId graphicsId = graphicsResolver.ResolveUnitFaceGraphicsId(unitId);

        return faces.TryGetSprite(Portraits.UnitPortrait(graphicsId.Value));
    }

    private AssetSprite? GetUnitEventPortrait(ScenarioEventPortraitId portraitId)
    {
        UnitDefinitionId unitId = new(portraitId.Value);

        return events.TryGetCompositeSprite(graphicsResolver.ResolveUnitFaceGraphicsId(unitId).Value) ??
               events.TryGetCompositeSprite(graphicsResolver.ResolveUnitBattleGraphicsId(unitId).Value);
    }

    private AssetSprite? GetLordFace(WorldRace race, int variant, bool isLarge)
    {
        if (isLarge)
        {
            return lordFaces.TryGetSprite(Portraits.LordFace(race, isLarge: true, variant));
        }
        else
        {
            return lordFaces.TryGetSprite(Portraits.LordFace(race, isLarge: false, variant)) ??
                   lordFaces.TryGetSprite(Portraits.LordFace(race, isLarge: true, variant));
        }
    }

    private AssetSprite? GetThiefActionImage(WorldThiefActionType type, bool isLarge)
    {
        return thiefActions.TryGetSprite(Portraits.ThiefActionImage(type, isLarge));
    }
}
