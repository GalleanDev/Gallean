using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPurchaseMercenaryCommand(
    WorldStackId StackId,
    WorldSiteId SiteId,
    UnitDefinitionId UnitDefinitionId,
    int Level,
    GridPosition Position) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Sites.TryGetValue(SiteId, out WorldSite? site))
        {
            return WorldCommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (site is not WorldMercenarySite mercenarySite)
        {
            return WorldCommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (stack.Position != mercenarySite.Entrance)
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotAtSite);
        }

        if (!UnitGrid<WorldUnit>.IsValidPosition(Position))
        {
            return WorldCommandResult.Fail(WorldCommandError.InvalidUnitPosition);
        }

        MercenarySiteCalculator.PurchaseInfo purchase =
            MercenarySiteCalculator.GetPurchaseInfo(mercenarySite, state, stack, player, UnitDefinitionId, Level, context);

        if (purchase.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        UnitDefinition unitDefinition = context.Definitions.GetUnit(UnitDefinitionId);
        WorldStackBuilder stackBuilder = new(context.Definitions);

        foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
        {
            stackBuilder.PlaceUnit(unit, position);
        }

        UnitStats mercenaryStats = UnitStatsCalculator.GetStats(unitDefinition, Level, [], context.Definitions, context.Globals);
        string? mercenaryName = LeaderNameCalculator.TryGetName(unitDefinition, context.Definitions, context.Random);
        WorldUnit newUnit = new(
            WorldCommandHelpers.GenerateUnitIds(state).First(),
            UnitDefinitionId,
            mercenaryStats.MaxHitpoints,
            0,
            Level,
            [],
            mercenaryName,
            IsTypeLocked: false);

        if (!stackBuilder.TryPlaceUnit(newUnit, Position))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOccupied);
        }

        if (!stackBuilder.TryBuild(out _, out WorldCommandError? stackError))
        {
            return WorldCommandResult.Fail(stackError.Value);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldMercenaryPurchasedEvent(
                    SiteId,
                    StackId,
                    newUnit,
                    Position,
                    purchase.Cost)
            ]);
    }
}
