namespace Gallean.Engine;

public sealed record WorldFortificationBattleStartedEvent(
    WorldStackId AttackerStackId,
    WorldFortificationId DefenderFortificationId,
    WorldPosition Position,
    BattleState InitialState) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
