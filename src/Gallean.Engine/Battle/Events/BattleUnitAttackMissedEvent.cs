namespace Gallean.Engine;

public sealed record BattleUnitAttackMissedEvent(
    BattleUnitId AttackerId,
    BattleUnitId TargetId,
    UnitAttackDefinitionId AttackId) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
