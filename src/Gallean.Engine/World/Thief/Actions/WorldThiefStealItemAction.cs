namespace Gallean.Engine;

public sealed record WorldThiefStealItemAction(WorldPosition TargetPosition, ItemDefinitionId ItemId) : WorldThiefAction
{
    public override TResult Accept<TContext, TResult>(IWorldThiefActionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
