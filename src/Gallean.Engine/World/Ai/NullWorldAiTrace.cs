using Gallean.Common;

namespace Gallean.Engine;

internal sealed class NullWorldAiTrace : IWorldAiTrace, IStaticInstance<NullWorldAiTrace>
{
    public void EvaluateEconomyAction(WorldPlayerId playerId, WorldAiPlanner.AbstractAction action, WorldAiActionPlan? plan)
    {
    }

    public void EvaluateStackAction(WorldStack stack, WorldAiPlanner.AbstractAction action, WorldAiActionPlan? plan)
    {
    }

    public void SelectEconomyAction(WorldPlayerId playerId, WorldAiPlanner.AbstractAction action, WorldAiActionPlan plan)
    {
    }

    public void SelectStackAction(WorldStack stack, WorldAiPlanner.AbstractAction action, WorldAiActionPlan plan)
    {
    }

    public void EstimateStackBattle(WorldStack stack, WorldPosition targetPosition, WorldPosition battlePosition, WorldAiEstimator.BattleEstimate battleEstimate, int score)
    {
    }

    public void SkipStackBattle(WorldStack stack, WorldPosition targetPosition, WorldPosition battlePosition, WorldAiEstimator.PowerEstimate powerEstimate)
    {
    }
}
