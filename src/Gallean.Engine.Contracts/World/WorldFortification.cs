using System.Text.Json.Serialization;

namespace Gallean.Engine.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WorldCapital), "capital")]
[JsonDerivedType(typeof(WorldCity), "city")]
[JsonDerivedType(typeof(WorldRuin), "ruin")]
public abstract record WorldFortification(
    WorldFortificationId Id,
    ScenarioTextId NameTextId,
    WorldFootprint Footprint,
    UnitGrid<WorldUnit> Garrison,
    WorldInventory Inventory,
    int AiPriority)
{
    public abstract TResult Accept<TContext, TResult>(IWorldFortificationVisitor<TContext, TResult> visitor, TContext context);
}
