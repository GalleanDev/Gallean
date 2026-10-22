using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed partial record AttackAction(IEngineContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        private BattleAiEstimator Estimator { get; } = new(Context);
        private BattleAiTargetProvider TargetProvider { get; } = new(Context);

        public override BattleAiActionPlan? Plan(BattleState state, BattleUnit unit)
        {
            BattleAttackTarget? bestTarget = null;
            int bestScore = 0;

            foreach ((BattleAttackTarget target, UnitAttackDefinition attack) in TargetProvider.GetAttackTargets(state, unit))
            {
                Scorer.Context scorerContext = new(Context, Estimator, state, unit, attack);
                int score = target.Accept(Scorer.Instance, scorerContext);

                if (score > 0 && (bestTarget is null || score > bestScore))
                {
                    bestTarget = target;
                    bestScore = score;
                }
            }

            if (bestTarget is null)
            {
                return null;
            }
            else
            {
                return MakePlan(bestScore, new BattleAttackCommand(unit.Id, bestTarget));
            }
        }
    }
}
