namespace Gallean.Engine;

public sealed record WorldCityBribedEvent(
    WorldStackId StackId,
    WorldFortificationId CityId,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
