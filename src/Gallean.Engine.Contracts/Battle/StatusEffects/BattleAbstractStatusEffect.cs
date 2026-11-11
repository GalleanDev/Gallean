namespace Gallean.Engine.Contracts;

public closed record BattleAbstractStatusEffect
{
    public abstract TResult Accept<TContext, TResult>(IBattleStatusEffectVisitor<TContext, TResult> visitor, TContext context);
}
