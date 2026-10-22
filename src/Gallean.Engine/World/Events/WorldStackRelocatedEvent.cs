namespace Gallean.Engine;

public sealed record WorldStackRelocatedEvent(
    WorldStackId StackId,
    WorldPosition SourcePosition,
    WorldPosition TargetPosition,
    WorldStackDirection NewDirection) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
