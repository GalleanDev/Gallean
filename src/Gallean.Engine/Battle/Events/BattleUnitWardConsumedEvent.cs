namespace Gallean.Engine;

public sealed record BattleUnitWardConsumedEvent(BattleUnitId UnitId, UnitWard Ward) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
