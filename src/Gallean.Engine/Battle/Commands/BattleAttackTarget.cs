namespace Gallean.Engine;

public closed record BattleAttackTarget
{
    public abstract TResult Accept<TContext, TResult>(IBattleAttackTargetVisitor<TContext, TResult> visitor, TContext context);
}
