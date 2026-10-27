namespace Gallean.Engine;

public sealed record BattleUnitTransformAppliedEvent(
    BattleUnitId SourceId,
    BattleUnitId TargetId,
    BattleTransformType Type,
    UnitDefinitionId DefinitionId,
    int Level,
    int HitpointsAfter,
    int MaxHitpointsAfter,
    int? RemainingTurns) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
