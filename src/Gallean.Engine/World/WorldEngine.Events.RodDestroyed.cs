namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldRodDestroyedEvent ev, WorldState state)
    {
        return state with
        {
            Rods = state.Rods.Remove(ev.RodId)
        };
    }
}
