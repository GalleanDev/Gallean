namespace Gallean.Engine;

internal sealed record ThiefFortificationTarget(WorldFortification Fortification, WorldPlayerId Owner, int Protection)
    : ThiefTarget(Owner, Protection)
{
    public override TResult Accept<TContext, TResult>(IThiefTargetVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
