using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldScenarioEventTriggeringEvent ev, WorldState state)
    {
        ScenarioEventDefinition definition = Context.Scenario.GetEvent(ev.EventId);

        if (definition.OccurOnce)
        {
            state = state with
            {
                ScenarioState = state.ScenarioState with
                {
                    EventStates = state.ScenarioState.EventStates.SetItem(ev.EventId, false)
                }
            };
        }

        foreach (ScenarioEventEffect effect in definition.Effects)
        {
            ScenarioEventEffectStateVisitor.Context context = new(state, Context, ev);
            state = effect.Accept(ScenarioEventEffectStateVisitor.Instance, context);
        }

        return state;
    }
}
