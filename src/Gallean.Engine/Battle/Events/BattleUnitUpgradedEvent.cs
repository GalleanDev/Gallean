namespace Gallean.Engine;

public sealed record BattleUnitUpgradedEvent(
    BattleUnitId UnitId,
    UnitDefinitionId FromDefinitionId,
    UnitDefinitionId ToDefinitionId,
    int Level,
    int Hitpoints,
    int MaxHitpoints) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
