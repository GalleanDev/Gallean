namespace Gallean.Engine;

public sealed record WorldCityRiotStartedEvent(
    WorldFortificationId CityId,
    int Turns) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
