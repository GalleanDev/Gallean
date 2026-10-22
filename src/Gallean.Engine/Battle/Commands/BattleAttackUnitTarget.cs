namespace Gallean.Engine;

public sealed record BattleAttackUnitTarget(BattleUnitId UnitId) : BattleAttackTarget
{
    public override TResult Accept<TContext, TResult>(IBattleAttackTargetVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
