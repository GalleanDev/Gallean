namespace Gallean.Engine;

internal sealed record ThiefStackTarget(WorldStack Stack, int Protection)
    : ThiefTarget(Stack.OwnerId, Protection)
{
    public override TResult Accept<TContext, TResult>(IThiefTargetVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
