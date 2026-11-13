using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed record WorldScenarioEffectEvent(
    WorldScenarioEventTriggeringEvent TriggeredEvent,
    ScenarioEventEffect Effect,
    ImmutableArray<ScenarioEventEffect> RemainingEffects,
    ImmutableArray<WorldScenarioCheckEvent> Checks) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
