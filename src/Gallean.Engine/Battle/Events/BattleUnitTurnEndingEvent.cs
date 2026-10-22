namespace Gallean.Engine;

public sealed record BattleUnitTurnEndingEvent(BattleUnitId UnitId) : BattleEvent, IScopeBeginEvent<BattleUnitTurnEndedEvent>
{
    public BattleUnitTurnEndedEvent CreateScopeEndEvent()
    {
        return new BattleUnitTurnEndedEvent(UnitId);
    }

    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
