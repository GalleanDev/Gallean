using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Gallean.Engine.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WorldMageSite), "mage")]
[JsonDerivedType(typeof(WorldMercenarySite), "mercenary")]
[JsonDerivedType(typeof(WorldMerchantSite), "merchant")]
[JsonDerivedType(typeof(WorldTrainerSite), "trainer")]
public abstract record WorldSite(
    WorldSiteId Id,
    WorldFootprint Footprint,
    WorldObjectVariant Variant,
    ScenarioTextId TitleTextId,
    ScenarioTextId GreetingTextId,
    ImmutableSortedSet<WorldPlayerId> VisitorIds,
    int AiPriority)
{
    public abstract TResult Accept<TContext, TResult>(IWorldSiteVisitor<TContext, TResult> visitor, TContext context);
}
