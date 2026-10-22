namespace Gallean.Engine;

internal interface IWorldAiTrace
{
    void EvaluateEconomyAction(WorldPlayerId playerId, WorldAiPlanner.AbstractAction action, WorldAiActionPlan? plan);
    void EvaluateStackAction(WorldStack stack, WorldAiPlanner.AbstractAction action, WorldAiActionPlan? plan);
    void SelectEconomyAction(WorldPlayerId playerId, WorldAiPlanner.AbstractAction action, WorldAiActionPlan plan);
    void SelectStackAction(WorldStack stack, WorldAiPlanner.AbstractAction action, WorldAiActionPlan plan);
    void EstimateStackBattle(WorldStack stack, WorldPosition targetPosition, WorldPosition battlePosition, WorldAiEstimator.BattleEstimate battleEstimate, int score);
    void SkipStackBattle(WorldStack stack, WorldPosition targetPosition, WorldPosition battlePosition, WorldAiEstimator.PowerEstimate powerEstimate);
}
