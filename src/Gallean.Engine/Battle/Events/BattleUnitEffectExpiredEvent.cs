namespace Gallean.Engine;

public sealed record BattleUnitEffectExpiredEvent(BattleUnitId UnitId, BattleAbstractStatusEffect Effect) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
