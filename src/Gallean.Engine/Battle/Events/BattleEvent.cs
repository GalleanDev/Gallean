namespace Gallean.Engine;

public closed record BattleEvent
{
    public abstract TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context);
}
