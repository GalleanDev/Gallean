namespace Gallean.Engine;

public sealed record WorldThiefPoisonAction(WorldPosition TargetPosition) : WorldThiefAction
{
    public override TResult Accept<TContext, TResult>(IWorldThiefActionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
