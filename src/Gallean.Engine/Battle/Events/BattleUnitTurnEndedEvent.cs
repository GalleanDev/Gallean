namespace Gallean.Engine;

public sealed record BattleUnitTurnEndedEvent(BattleUnitId UnitId) : BattleEvent, IScopeEndEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
