namespace Gallean.Engine.Contracts;

public sealed record BattlePoisonStatusEffect(int RemainingTurns, int Damage) : BattleAbstractDamageOverTimeEffect(RemainingTurns, Damage)
{
    public override TResult Accept<TContext, TResult>(IBattleStatusEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
