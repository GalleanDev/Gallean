namespace Gallean.Engine;

public sealed record BattleEndedEvent(BattleSide WinningSide) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
