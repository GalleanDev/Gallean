namespace Gallean.Engine.Contracts;

public sealed record WorldCity(
    WorldFortificationId Id,
    ScenarioTextId NameTextId,
    WorldCityDefinitionId DefinitionId,
    WorldPlayerId OwnerId,
    WorldSubraceId SubraceId,
    WorldFootprint Footprint,
    UnitGrid<WorldUnit> Garrison,
    WorldInventory Inventory,
    int AiPriority,
    int RiotTurns,
    int GrowthTurn) : WorldFortification(Id, NameTextId, Footprint, Garrison, Inventory, AiPriority)
{
    public override TResult Accept<TContext, TResult>(IWorldFortificationVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
