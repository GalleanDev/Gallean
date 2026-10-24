namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldScenarioPopupEvent ev, WorldState state)
    {
        WorldScenarioMessage message = new(
            state.TurnInfo.TurnNumber,
            ev.EventId,
            ev.TextId,
            ev.PortraitId,
            ev.PortraitName,
            ev.IsPortraitLeft,
            ev.TriggererId,
            ev.Audience);

        return state with
        {
            ScenarioState = state.ScenarioState with
            {
                Messages = state.ScenarioState.Messages.Add(message)
            }
        };
    }
}
