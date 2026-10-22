namespace Gallean.Engine;

public sealed record BattleAutoResolveStepEvent(int StartRoundNumber) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
