using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private abstract record AbstractAction(IEngineContext Context, int Priority)
    {
        protected const int MaxScore = UtilityScore.MaxScore;

        public abstract BattleAiActionPlan? Plan(BattleState state, BattleUnit unit);

        protected BattleAiActionPlan MakePlan(int score, IBattleCommand<Empty> command)
        {
            score = Math.Clamp(score, 0, MaxScore);

            return new BattleAiActionPlan(new BattleAiActionScore(Priority, score), command);
        }
    }
}
