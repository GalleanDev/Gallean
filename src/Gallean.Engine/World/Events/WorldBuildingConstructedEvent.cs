namespace Gallean.Engine;

public sealed record WorldBuildingConstructedEvent(
    WorldPlayerId PlayerId,
    WorldPlayerBuildingDefinitionId BuildingId
) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
