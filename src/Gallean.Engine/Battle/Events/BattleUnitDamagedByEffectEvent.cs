namespace Gallean.Engine;

public sealed record BattleUnitDamagedByEffectEvent(
    BattleUnitId UnitId,
    BattleAbstractStatusEffect Effect,
    int Damage,
    int HitpointsAfter) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
