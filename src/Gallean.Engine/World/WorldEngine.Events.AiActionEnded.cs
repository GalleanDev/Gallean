namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldAiActionEndedEvent ev, WorldState state)
    {
        return state with { AiMemories = state.AiMemories.SetItem(ev.PlayerId, ev.Memory) };
    }
}
