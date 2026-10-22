namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackRemovedEvent ev, WorldState state)
    {
        return WorldCommandHelpers.RemoveStack(state, ev.StackId, destroyer: null, destroyerPlayerId: null, Context.Globals);
    }
}
