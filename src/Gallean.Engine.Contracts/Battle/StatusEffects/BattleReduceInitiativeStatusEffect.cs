namespace Gallean.Engine.Contracts;

public sealed record BattleReduceInitiativeStatusEffect(int Percent, bool IsInfinite) : BattleAbstractStatModifierEffect(Percent, IsInfinite)
{
    public override TResult Accept<TContext, TResult>(IBattleStatusEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
