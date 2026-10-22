namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitResurrectedEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldPlayerId ownerId = WorldUnitGroupCalculator.GetOwner(state, ev.Group)!.Value;
        WorldPlayer player = state.Players[ownerId];
        WorldUnit target = units.GetUnit(ev.UnitId);

        UnitGrid<WorldUnit> updatedUnits = units.Replace(target, target with { Hitpoints = ev.Hitpoints });
        WorldPlayer updatedPlayer = player with { Resources = ResourceSetCalculator.Subtract(player.Resources, ev.Cost) };

        state = WorldUnitGroupCalculator.WithUnits(state, ev.Group, updatedUnits);

        return state with
        {
            Players = state.Players.SetItem(updatedPlayer.Id, updatedPlayer)
        };
    }
}
