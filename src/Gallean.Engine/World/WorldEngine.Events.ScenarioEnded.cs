namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldScenarioEndedEvent ev, WorldState state)
    {
        if (state.ScenarioState.IsEnded)
        {
            return state;
        }

        WorldScenarioState updatedScenarioState = state.ScenarioState with { IsEnded = true, Winner = ev.WinnerId };

        return state with { ScenarioState = updatedScenarioState };
    }
}
