namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckRelationLevel(ScenarioEventDiplomacyRelationsCondition condition, ScenarioEventConditionContext context)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, condition.Player1, condition.Player2);
            WorldRelationLevel level = DiplomacyCalculator.ResolveRelationLevel(relation.Current, context.Globals);

            return level == condition.Level;
        }
    }
}
