namespace Gallean.Engine;

public closed record ItemEffect
{
    public abstract TResult Accept<TContext, TResult>(IItemEffectVisitor<TContext, TResult> visitor, TContext context);
}
