namespace Gallean.Engine;

public closed record WorldAbstractSpellEffect
{
    public abstract TResult Accept<TContext, TResult>(IWorldSpellEffectVisitor<TContext, TResult> visitor, TContext context);
}
