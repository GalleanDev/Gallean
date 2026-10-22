namespace Gallean.Engine;

public interface IBattleAttackTargetVisitor<in TContext, out TResult>
{
    TResult Visit(BattleAttackUnitTarget target, TContext context);
    TResult Visit(BattleAttackTileTarget target, TContext context);
}
