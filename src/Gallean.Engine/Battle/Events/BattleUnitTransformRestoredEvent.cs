namespace Gallean.Engine;

public sealed record BattleUnitTransformRestoredEvent(
    BattleUnitId UnitId,
    BattleTransformType Type,
    UnitDefinitionId DefinitionId) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
