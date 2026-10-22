namespace Gallean.Engine;

public abstract record WorldAbstractSpellEffect
{
    public abstract TResult Accept<TContext, TResult>(IWorldSpellEffectVisitor<TContext, TResult> visitor, TContext context);
}
