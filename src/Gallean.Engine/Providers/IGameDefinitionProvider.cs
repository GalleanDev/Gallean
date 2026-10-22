using System.Collections.Immutable;

namespace Gallean.Engine;

public interface IGameDefinitionProvider
{
    UnitDefinition GetUnit(UnitDefinitionId id);
    UnitDefinition? TryGetUnit(UnitDefinitionId id);
    UnitAttackDefinition GetAttack(UnitAttackDefinitionId id);
    ImmutableArray<UnitUpgradeDefinition> GetUnitUpgrades(UnitDefinitionId id);
    UnitDynamicUpgradeDefinition GetUnitDynamicUpgrade(UnitDefinitionId id);
    LordDefinition GetLord(LordDefinitionId id);
    ImmutableArray<LeaderUpgradeDefinition> GetLeaderUpgrades(LordDefinitionId id);
    UnitModifierDefinition GetUnitModifier(UnitModifierDefinitionId id);
    ItemDefinition GetItem(ItemDefinitionId id);
    WorldCityDefinition GetCity(WorldCityDefinitionId id);
    WorldLandmarkDefinition GetLandmark(WorldLandmarkDefinitionId id);
    WorldPlayerBuildingDefinition GetPlayerBuilding(WorldPlayerBuildingDefinitionId id);
    WorldAiAttitudeDefinition GetAiAttitude(WorldAiAttitudeType type);
    WorldRaceDefinition GetRace(WorldRace race);
    WorldSpellDefinition GetSpell(WorldSpellDefinitionId id);
    WorldThiefActionDefinition GetThiefAction(WorldThiefActionType type);
    IReadOnlyDictionary<WorldSpellDefinitionId, WorldResourceSet> GetSpellResearchCosts(WorldRace race);
}
