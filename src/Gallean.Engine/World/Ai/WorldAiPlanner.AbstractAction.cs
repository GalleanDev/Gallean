using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    internal abstract record AbstractAction(WorldAiContext Context, int Priority)
    {
        protected const int MaxScore = UtilityScore.MaxScore;

        protected WorldAiActionPlan MakePlan(int score, IWorldCommand<Empty> command, WorldAiMemory memory)
        {
            score = Math.Clamp(score, 0, MaxScore);

            return new WorldAiActionPlan(new WorldAiActionScore(Priority, score), command, memory);
        }
    }
}
