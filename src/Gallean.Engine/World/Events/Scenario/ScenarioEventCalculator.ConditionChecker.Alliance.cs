namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckAlliance(ScenarioEventAllianceCondition condition, ScenarioEventConditionContext context)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, condition.Player1, condition.Player2);

            return relation.Alliance;
        }
    }
}
