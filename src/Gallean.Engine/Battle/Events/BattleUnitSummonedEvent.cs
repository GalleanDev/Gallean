namespace Gallean.Engine;

public sealed record BattleUnitSummonedEvent(
    BattleUnitId UnitId,
    UnitDefinitionId DefinitionId,
    BattleSide Side,
    GridPosition Position,
    int Hitpoints,
    int Level,
    BattleUnitId SummonerId) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
