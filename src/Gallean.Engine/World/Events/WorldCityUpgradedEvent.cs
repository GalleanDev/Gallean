namespace Gallean.Engine;

public sealed record WorldCityUpgradedEvent(
    WorldFortificationId CityId,
    WorldCityDefinitionId DefinitionId,
    WorldResourceSet Cost
) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
