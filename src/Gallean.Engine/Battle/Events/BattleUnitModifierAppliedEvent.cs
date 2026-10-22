namespace Gallean.Engine;

public sealed record BattleUnitModifierAppliedEvent(
    BattleUnitId UnitId,
    UnitModifier Modifier) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
