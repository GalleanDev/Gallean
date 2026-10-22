namespace Gallean.Engine;

public abstract record BattleEvent
{
    public abstract TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context);
}
