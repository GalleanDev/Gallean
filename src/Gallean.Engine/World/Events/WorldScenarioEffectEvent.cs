using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed record WorldScenarioEffectEvent(
    WorldScenarioEventTriggeringEvent TriggeredEvent,
    int EffectIndex,
    ImmutableArray<WorldScenarioCheckEvent> Checks) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
