namespace Gallean.Engine;

public sealed record WorldAiActionStartedEvent(WorldAiActionPlan Plan) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
