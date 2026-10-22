namespace Gallean.Engine.Contracts;

public sealed record WorldRuin(
    WorldFortificationId Id,
    ScenarioTextId NameTextId,
    WorldFootprint Footprint,
    WorldObjectVariant Variant,
    UnitGrid<WorldUnit> Garrison,
    WorldResourceSet Resources,
    ItemDefinitionId? Item,
    WorldPlayerId? LooterId,
    int AiPriority) : WorldFortification(Id, NameTextId, Footprint, Garrison, WorldInventory.Empty, AiPriority)
{
    public override TResult Accept<TContext, TResult>(IWorldFortificationVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
