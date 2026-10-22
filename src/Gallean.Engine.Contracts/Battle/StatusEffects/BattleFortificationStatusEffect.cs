namespace Gallean.Engine.Contracts;

public sealed record BattleFortificationStatusEffect(int Armor) : BattleAbstractStatusEffect
{
    public override TResult Accept<TContext, TResult>(IBattleStatusEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
