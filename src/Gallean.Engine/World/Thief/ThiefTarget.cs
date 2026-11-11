namespace Gallean.Engine;

internal closed record ThiefTarget(WorldPlayerId? OwnerId, int Protection)
{
    public abstract TResult Accept<TContext, TResult>(IThiefTargetVisitor<TContext, TResult> visitor, TContext context);
}
