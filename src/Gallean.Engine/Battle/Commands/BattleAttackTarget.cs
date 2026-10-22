namespace Gallean.Engine;

public abstract record BattleAttackTarget
{
    public abstract TResult Accept<TContext, TResult>(IBattleAttackTargetVisitor<TContext, TResult> visitor, TContext context);
}
