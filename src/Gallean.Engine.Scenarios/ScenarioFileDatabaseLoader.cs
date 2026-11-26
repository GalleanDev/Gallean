namespace Gallean.Engine.Scenarios;

public static class ScenarioFileDatabaseLoader
{
    public static ScenarioDatabase Load(IReadOnlyList<ScenarioFileTables> files)
    {
        return ScenarioDatabaseLoader.Create(
            scenarioBagRows: [.. files.SelectMany(file => file.ScenarioBags)],
            scenarioCountRows: [],
            scenarioCrystalRows: [.. files.SelectMany(file => file.ScenarioCrystals)],
            scenarioEventRows: [.. files.SelectMany(file => file.ScenarioEvents)],
            scenarioEventConditionRows: [.. files.SelectMany(file => file.ScenarioEventConditions)],
            scenarioEventEffectRows: [.. files.SelectMany(file => file.ScenarioEventEffects)],
            scenarioEventUnfogRows: [.. files.SelectMany(file => file.ScenarioEventUnfogs)],
            scenarioFogRows: [.. files.SelectMany(file => file.ScenarioFogs)],
            scenarioFortificationRows: [.. files.SelectMany(file => file.ScenarioFortifications)],
            scenarioInfoRows: [.. files.SelectMany(file => file.ScenarioInfos)],
            scenarioInventoryRows: [.. files.SelectMany(file => file.ScenarioInventories)],
            scenarioItemRows: [.. files.SelectMany(file => file.ScenarioItems)],
            scenarioLandmarkRows: [.. files.SelectMany(file => file.ScenarioLandmarks)],
            scenarioLocationRows: [.. files.SelectMany(file => file.ScenarioLocations)],
            scenarioMageRows: [.. files.SelectMany(file => file.ScenarioMages)],
            scenarioMapBlockRows: [.. files.SelectMany(file => file.ScenarioMapBlocks)],
            scenarioMapElementRows: [.. files.SelectMany(file => file.ScenarioMapElements)],
            scenarioMercenaryRows: [.. files.SelectMany(file => file.ScenarioMercenaries)],
            scenarioMerchantRows: [.. files.SelectMany(file => file.ScenarioMerchants)],
            scenarioMountainRows: [.. files.SelectMany(file => file.ScenarioMountains)],
            scenarioObjectMapRows: [.. files.SelectMany(file => file.ScenarioObjectMaps)],
            scenarioPlayerRows: [.. files.SelectMany(file => file.ScenarioPlayers)],
            scenarioRelationRows: [.. files.SelectMany(file => file.ScenarioRelations)],
            scenarioRoadRows: [.. files.SelectMany(file => file.ScenarioRoads)],
            scenarioRodRows: [.. files.SelectMany(file => file.ScenarioRods)],
            scenarioRuinRows: [.. files.SelectMany(file => file.ScenarioRuins)],
            scenarioSiteRows: [.. files.SelectMany(file => file.ScenarioSites)],
            scenarioSpellRows: [.. files.SelectMany(file => file.ScenarioSpells)],
            scenarioStackRows: [.. files.SelectMany(file => file.ScenarioStacks)],
            scenarioStackTemplateRows: [.. files.SelectMany(file => file.ScenarioStackTemplates)],
            scenarioStackTemplateModifierRows: [.. files.SelectMany(file => file.ScenarioStackTemplateModifiers)],
            scenarioSubraceRows: [.. files.SelectMany(file => file.ScenarioSubraces)],
            scenarioTalismanChargesRows: [.. files.SelectMany(file => file.ScenarioTalismanCharges)],
            scenarioUnitRows: [.. files.SelectMany(file => file.ScenarioUnits)],
            scenarioUnitGroupRows: [.. files.SelectMany(file => file.ScenarioUnitGroups)],
            scenarioUnitModifierRows: [.. files.SelectMany(file => file.ScenarioUnitModifiers)],
            scenarioVariableRows: [.. files.SelectMany(file => file.ScenarioVariables)],
            textScenarioRows: [.. files.SelectMany(file => file.TextScenarios)]);
    }
}
