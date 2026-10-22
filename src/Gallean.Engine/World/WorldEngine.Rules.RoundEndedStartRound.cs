namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class RoundEndedStartRoundRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldRoundEndedEvent ev, WorldState state)
        {
            return EventRuleResult<WorldEvent>.Append(new WorldRoundStartedEvent());
        }
    }
}
