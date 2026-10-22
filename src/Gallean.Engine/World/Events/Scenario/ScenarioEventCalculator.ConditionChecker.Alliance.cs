namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class Alliance(ScenarioEventAllianceCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAlliance(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAlliance(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAlliance(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAlliance(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAlliance(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAlliance(context);
            }

            private bool CheckAlliance(ScenarioEventConditionContext context)
            {
                WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, condition.Player1, condition.Player2);

                return relation.Alliance;
            }
        }
    }
}
