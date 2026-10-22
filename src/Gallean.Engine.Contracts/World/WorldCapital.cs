namespace Gallean.Engine.Contracts;

public sealed record WorldCapital(
    WorldFortificationId Id,
    ScenarioTextId NameTextId,
    WorldPlayerId OwnerId,
    WorldFootprint Footprint,
    UnitGrid<WorldUnit> Garrison,
    WorldInventory Inventory,
    int AiPriority,
    WorldPlayerId? CapturedById) : WorldFortification(Id, NameTextId, Footprint, Garrison, Inventory, AiPriority)
{
    public override TResult Accept<TContext, TResult>(IWorldFortificationVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
