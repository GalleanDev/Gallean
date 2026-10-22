namespace Gallean.Engine;

internal static class MercenarySiteCalculator
{
    public sealed record PurchaseInfo(WorldResourceSet Cost, WorldCommandError? Error);

    public static PurchaseInfo GetPurchaseInfo(
        WorldMercenarySite site,
        WorldState state,
        WorldStack stack,
        WorldPlayer player,
        UnitDefinitionId unitDefinitionId,
        int level,
        IEngineContext context)
    {
        if (!Contains(site, unitDefinitionId, level))
        {
            return new PurchaseInfo(WorldResourceSet.Empty, WorldCommandError.UnitNotAvailable);
        }

        UnitCosts costs = context.UnitStats.GetUnitCosts(unitDefinitionId, level);
        WorldResourceSet cost = PurchaseCalculator.GetPurchaseCost(costs.PurchaseCost, stack, context);

        if (!ResourceSetCalculator.CanAfford(player.Resources, cost))
        {
            return new PurchaseInfo(cost, WorldCommandError.InsufficientResources);
        }

        UnitDefinition definition = context.Definitions.GetUnit(unitDefinitionId);
        UnitStats stats = UnitStatsCalculator.GetStats(definition, level, [], context.Definitions, context.Globals);
        WorldUnit unit = new(WorldCommandHelpers.GenerateUnitIds(state).First(), unitDefinitionId, stats.MaxHitpoints, 0, level, [], null, false);

        WorldStackBuilder builder = new(context.Definitions);

        foreach ((WorldUnit existingUnit, GridPosition position) in stack.Units.GetUnits())
        {
            builder.PlaceUnit(existingUnit, position);
        }

        foreach (GridPosition position in GridPosition.All)
        {
            if (builder.TryPlaceUnit(unit, position))
            {
                if (!builder.TryBuild(out _, out WorldCommandError? error))
                {
                    return new PurchaseInfo(cost, error);
                }

                return new PurchaseInfo(cost, Error: null);
            }
        }

        return new PurchaseInfo(cost, WorldCommandError.TargetOccupied);
    }

    private static bool Contains(WorldMercenarySite site, UnitDefinitionId unitDefinitionId, int level)
    {
        foreach (WorldMercenaryUnit mercenaryUnit in site.Units)
        {
            if (mercenaryUnit.DefinitionId == unitDefinitionId && mercenaryUnit.Level == level)
            {
                return true;
            }
        }

        return false;
    }
}
