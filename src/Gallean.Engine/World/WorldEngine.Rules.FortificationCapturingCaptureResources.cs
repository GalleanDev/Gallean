namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class FortificationCapturingCaptureResourcesRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldFortificationCapturingEvent ev, WorldState state)
        {
            if (state.Fortifications[ev.FortificationId] is not WorldCapital)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlayer capturedPlayer = state.Players[ev.PreviousOwnerId];
            WorldPlayer capturingPlayer = state.Players[ev.NewOwnerId];

            if (!WorldHelpers.IsRacePlayable(capturedPlayer.Race) || !WorldHelpers.IsRacePlayable(capturingPlayer.Race))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (capturedPlayer.Resources == WorldResourceSet.Empty)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(
                new WorldResourcesCapturedEvent(capturingPlayer.Id, capturedPlayer.Id, capturedPlayer.Resources));
        }
    }
}
