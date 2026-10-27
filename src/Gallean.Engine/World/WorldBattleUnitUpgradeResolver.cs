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

        switch (upgrade)
        {
            case UnitUpgrade.TypeUpgrade typeUpgrade:
                UnitDefinition target = context.Definitions.GetUnit(typeUpgrade.ToUnitId);
                return CreateUpgrade(target, target.Level, unit.Modifiers);
            case UnitUpgrade.DynamicUpgrade:
                return CreateUpgrade(definition, unit.Level + 1, unit.Modifiers);
            case UnitUpgrade.Blocked:
                return null;
            default:
                throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
        }
    }

    private BattleUnitUpgrade CreateUpgrade(UnitDefinition definition, int level, ImmutableArray<UnitModifier> modifiers)
    {
        UnitStats stats = UnitStatsCalculator.GetStats(definition, level, modifiers, context.Definitions, context.Globals);

        return new BattleUnitUpgrade(definition.Id, level, stats.MaxHitpoints, stats.MaxHitpoints);
    }
}
