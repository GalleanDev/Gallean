namespace Gallean.Engine;

public sealed record BattleAttackTileTarget(BattleSide Side, GridPosition Position) : BattleAttackTarget
{
    public override TResult Accept<TContext, TResult>(IBattleAttackTargetVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
