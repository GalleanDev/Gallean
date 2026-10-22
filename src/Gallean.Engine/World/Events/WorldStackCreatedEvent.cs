namespace Gallean.Engine;

public sealed record WorldStackCreatedEvent(
    WorldStack Stack,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
