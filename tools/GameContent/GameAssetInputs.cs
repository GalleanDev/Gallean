using Gallean.Engine.Contracts;

namespace GameContent;

public sealed record GameAssetInputs(
    IReadOnlyList<UnitDefinitionId> UnitIds,
    IReadOnlyList<ScenarioEventPortraitId> EventPortraitIds,
    IReadOnlyList<WorldSpellDefinitionId> SpellIds,
    IReadOnlyList<WorldPlayerBuildingDefinitionId> BuildingIds,
    IReadOnlyList<WorldLandmarkDefinitionId> LandmarkIds,
    IReadOnlyList<ItemDefinitionId> ItemIds,
    IReadOnlyList<ScenarioId> ScenarioIds)
{
    public static GameAssetInputs Load(GlobalDatabase globalDatabase, ScenarioDatabase scenarioDatabase)
    {
        List<UnitDefinitionId> unitIds = [];

        foreach (Db.Unit unit in globalDatabase.Units.Values)
        {
            unitIds.Add(new UnitDefinitionId(unit.UnitId.Value));
        }

        List<ScenarioEventPortraitId> eventPortraitIds =
        [
            new("CENTAUR"),
            new("EMERY")
        ];

        foreach (Db.ScenarioEventEffect effect in scenarioDatabase.ScenarioEventEffects)
        {
            if (effect is { Category: Db.ScenarioEventEffectType.Popup, Image: { } image })
            {
                eventPortraitIds.Add(new ScenarioEventPortraitId(image));
            }
        }

        List<WorldSpellDefinitionId> spellIds = [];

        foreach (Db.Spell spell in globalDatabase.Spells.Values)
        {
            spellIds.Add(new WorldSpellDefinitionId(spell.SpellId.Value));
        }

        List<WorldPlayerBuildingDefinitionId> buildingIds = [];

        foreach (Db.Building building in globalDatabase.Buildings.Values)
        {
            buildingIds.Add(new WorldPlayerBuildingDefinitionId(building.BuildingId.Value));
        }

        List<WorldLandmarkDefinitionId> landmarkIds = [];

        foreach (Db.Landmark landmark in globalDatabase.Landmarks.Values)
        {
            landmarkIds.Add(new WorldLandmarkDefinitionId(landmark.LandmarkId.Value));
        }

        List<ItemDefinitionId> itemIds = [];

        foreach (Db.Item item in globalDatabase.Items.Values)
        {
            if (item.ItemCategory != Db.MagicItemType.Scroll)
            {
                itemIds.Add(new ItemDefinitionId(item.ItemId.Value));
            }
        }

        List<ScenarioId> scenarioIds = [];

        foreach (Db.ScenarioInfo info in scenarioDatabase.ScenarioInfos)
        {
            scenarioIds.Add(new ScenarioId(info.ScenarioId.Value));
        }

        return new GameAssetInputs(unitIds, eventPortraitIds, spellIds, buildingIds, landmarkIds, itemIds, scenarioIds);
    }
}
