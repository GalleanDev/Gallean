namespace Gallean.Engine;

public sealed record WorldStackAutoFittedEvent(
    WorldStackId StackId,
    UnitGrid<WorldUnit> StackUnits,
    UnitGrid<WorldUnit> GarrisonUnits,
    WorldInventory StackInventory,
    WorldInventory GarrisonInventory,
    LeaderEquipment Equipment) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
