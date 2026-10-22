namespace Gallean.Engine;

internal sealed record ThiefRuinTarget(WorldRuin Ruin)
    : ThiefTarget(OwnerId: null, Protection: 0)
{
    public override TResult Accept<TContext, TResult>(IThiefTargetVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
