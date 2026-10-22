namespace Gallean.Engine;

public sealed record BattleUnitAttackHitEvent(
    BattleUnitId AttackerId,
    BattleUnitId TargetId,
    UnitAttackDefinitionId AttackId,
    int Damage) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
