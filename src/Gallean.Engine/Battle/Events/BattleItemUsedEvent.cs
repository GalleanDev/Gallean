namespace Gallean.Engine;

public sealed record BattleItemUsedEvent(
    BattleSide Side,
    LeaderEquipmentSlot Slot,
    ItemDefinitionId ItemId,
    BattleUnitId UnitId,
    BattleAttackTarget Target) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
