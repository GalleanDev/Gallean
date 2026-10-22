namespace Gallean.Engine;

public abstract record ItemEffect
{
    public abstract TResult Accept<TContext, TResult>(IItemEffectVisitor<TContext, TResult> visitor, TContext context);
}
