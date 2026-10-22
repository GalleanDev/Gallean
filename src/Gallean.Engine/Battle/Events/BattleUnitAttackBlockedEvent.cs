namespace Gallean.Engine;

public sealed record BattleUnitAttackBlockedEvent(
    BattleUnitId AttackerId,
    BattleUnitId TargetId,
    UnitAttackDefinitionId AttackId,
    UnitWard Ward) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
