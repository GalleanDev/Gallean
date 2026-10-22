namespace Gallean.Engine;

public sealed record WorldFortificationEnteredEvent(WorldStackId StackId, WorldFortificationId FortificationId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
