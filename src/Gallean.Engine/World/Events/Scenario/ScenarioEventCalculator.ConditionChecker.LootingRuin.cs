namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class LootingRuin(ScenarioEventLootingRuinCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckRuinLooted(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckRuinLooted(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckRuinLooted(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckRuinLooted(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckRuinLooted(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckRuinLooted(context);
            }

            private bool CheckRuinLooted(ScenarioEventConditionContext context)
            {
                if (!context.State.Fortifications.TryGetValue(condition.Ruin, out WorldFortification? fortification))
                {
                    return false;
                }

                if (fortification is not WorldRuin ruin)
                {
                    return false;
                }

                return ruin.LooterId is not null && IsAffectedRace(ruin.LooterId.Value, context);
            }
        }
    }
}
