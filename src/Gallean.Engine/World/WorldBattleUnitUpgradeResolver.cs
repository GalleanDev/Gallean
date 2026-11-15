using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed class WorldBattleUnitUpgradeResolver(
    WorldPlayerBuildings attackerBuildings,
    WorldPlayerBuildings defenderBuildings,
    IEngineContext context) : IBattleUnitUpgradeResolver
{
    public BattleUnitUpgrade? UpgradeUnit(BattleUnit unit)
    {
        UnitDefinition definition = context.Definitions.GetUnit(unit.DefinitionId);
        UnitStats stats = UnitStatsCalculator.GetStats(definition, unit.Level, unit.Modifiers, context.Definitions, context.Globals);
        UnitExperience experience = UnitExperienceCalculator.GetExperience(definition, unit.Level, unit.Experience, stats, context.Scenario.Definition);

        if (experience.IsMaximumLevel)
        {
            return null;
        }

        WorldPlayerBuildings buildings = unit.Side == BattleSide.Attacker ? attackerBuildings : defenderBuildings;
        UnitUpgrade upgrade = UnitUpgradeCalculator.GetUpgrade(unit.DefinitionId, unit.IsTypeLocked, buildings, context.Definitions);

        return upgrade switch
        {
            UnitUpgrade.TypeUpgrade typeUpgrade => CreateTypeUpgrade(typeUpgrade.ToUnitId, unit.Modifiers),
            UnitUpgrade.DynamicUpgrade => CreateDynamicUpgrade(definition, unit.Level + 1, unit.Modifiers),
            UnitUpgrade.Blocked => null
        };
    }

    private BattleUnitUpgrade CreateTypeUpgrade(UnitDefinitionId targetId, ImmutableArray<UnitModifier> modifiers)
    {
        UnitDefinition target = context.Definitions.GetUnit(targetId);
        UnitStats stats = UnitStatsCalculator.GetStats(target, target.Level, modifiers, context.Definitions, context.Globals);

        return new BattleUnitUpgrade(target.Id, target.Level, stats.MaxHitpoints, stats.MaxHitpoints);
    }

    private BattleUnitUpgrade CreateDynamicUpgrade(UnitDefinition definition, int level, ImmutableArray<UnitModifier> modifiers)
    {
        UnitStats stats = UnitStatsCalculator.GetStats(definition, level, modifiers, context.Definitions, context.Globals);

        return new BattleUnitUpgrade(definition.Id, level, stats.MaxHitpoints, stats.MaxHitpoints);
    }
}
