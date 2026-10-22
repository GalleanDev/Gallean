namespace Gallean.Engine;

public sealed record ReviveItemEffect : ItemEffect
{
    public override TResult Accept<TContext, TResult>(IItemEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
