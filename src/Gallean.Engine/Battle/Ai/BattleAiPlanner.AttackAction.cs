using System.Collections.Immutable;

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

            foreach ((BattleAttackTarget target, ImmutableArray<UnitAttackDefinition> attacks) in TargetProvider.GetAttackTargets(state, unit))
            {
                int score = ScoreTarget(state, unit, target, attacks);

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

        private int ScoreTarget(
            BattleState state,
            BattleUnit unit,
            BattleAttackTarget target,
            ImmutableArray<UnitAttackDefinition> attacks)
        {
            UnitAttackDefinition primaryAttack = attacks[0];
            UnitAttackTypeInfo primaryAttackInfo = primaryAttack.Type.GetInfo();

            if (primaryAttackInfo is { CanMiss: false, IsWardable: false })
            {
                int score = 0;

                foreach (UnitAttackDefinition attack in attacks)
                {
                    Scorer.Context scorerContext = new(Context, Estimator, state, unit, attack);

                    score += Scorer.Score(target, scorerContext);
                }

                return score;
            }
            else
            {
                Scorer.Context scorerContext = new(Context, Estimator, state, unit, primaryAttack);

                return Scorer.Score(target, scorerContext);
            }
        }
    }
}
