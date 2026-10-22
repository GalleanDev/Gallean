namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldMercenaryPurchasedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldMercenarySite site = (WorldMercenarySite)state.Sites[ev.SiteId];
        WorldPlayer player = state.Players[stack.OwnerId];

        WorldResourceSet updatedResources = ResourceSetCalculator.Subtract(player.Resources, ev.Cost);

        WorldStackBuilder stackBuilder = new(Context.Definitions);

        foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
        {
            stackBuilder.PlaceUnit(unit, position);
        }

        stackBuilder.PlaceUnit(ev.Unit, ev.Position);

        if (!stackBuilder.TryBuild(out UnitGrid<WorldUnit>? units, out WorldCommandError? error))
        {
            throw new InvalidOperationException($"Unexpected stack composition: {error}.");
        }

        WorldStack updatedStack = stack with
        {
            Units = LeaderEquipmentCalculator.UpdateUnitModifiers(units, stack.Equipment, Context.Definitions)
        };

        WorldMercenarySite updatedSite = site;
        int targetIndex = -1;

        for (int index = 0; index < site.Units.Length; index++)
        {
            if (site.Units[index].DefinitionId == ev.Unit.DefinitionId && site.Units[index].Level == ev.Unit.Level)
            {
                targetIndex = index;

                break;
            }
        }

        if (site.Units[targetIndex].IsUnique)
        {
            updatedSite = site with { Units = site.Units.RemoveAt(targetIndex) };
        }

        return state with
        {
            Players = state.Players.SetItem(player.Id, player with { Resources = updatedResources }),
            Stacks = state.Stacks.SetItem(updatedStack.Id, updatedStack),
            Sites = state.Sites.SetItem(updatedSite.Id, updatedSite)
        };
    }
}
