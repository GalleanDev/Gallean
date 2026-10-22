namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class DiplomacyRelations(ScenarioEventDiplomacyRelationsCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLevel(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLevel(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLevel(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLevel(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLevel(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLevel(context);
            }

            private bool CheckLevel(ScenarioEventConditionContext context)
            {
                WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, condition.Player1, condition.Player2);
                WorldRelationLevel level = DiplomacyCalculator.ResolveRelationLevel(relation.Current, context.Globals);

                return level == condition.Level;
            }
        }
    }
}
