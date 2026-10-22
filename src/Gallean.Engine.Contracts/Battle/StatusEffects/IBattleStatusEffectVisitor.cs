namespace Gallean.Engine.Contracts;

public interface IBattleStatusEffectVisitor<in TContext, out TResult>
{
    TResult Visit(BattleDefendStatusEffect effect, TContext context);
    TResult Visit(BattleParalyzeStatusEffect effect, TContext context);
    TResult Visit(BattlePetrifyStatusEffect effect, TContext context);
    TResult Visit(BattlePoisonStatusEffect effect, TContext context);
    TResult Visit(BattleFrostbiteStatusEffect effect, TContext context);
    TResult Visit(BattleBlisterStatusEffect effect, TContext context);
    TResult Visit(BattleRetreatStatusEffect effect, TContext context);
    TResult Visit(BattleRetreatedStatusEffect effect, TContext context);
    TResult Visit(BattleFortificationStatusEffect effect, TContext context);
    TResult Visit(BattleIncreaseDamageStatusEffect effect, TContext context);
    TResult Visit(BattleReduceDamageStatusEffect effect, TContext context);
    TResult Visit(BattleReduceInitiativeStatusEffect effect, TContext context);
    TResult Visit(BattleShatterArmorStatusEffect effect, TContext context);
    TResult Visit(BattleCureStatusEffect effect, TContext context);
    TResult Visit(BattleTransformStatusEffect effect, TContext context);
}
