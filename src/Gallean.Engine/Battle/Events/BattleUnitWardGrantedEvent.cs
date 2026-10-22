namespace Gallean.Engine;

public sealed record BattleUnitWardGrantedEvent(BattleUnitId UnitId, BattleUnitWardModifier Modifier) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
