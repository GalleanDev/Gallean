namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class EnteringCity(ScenarioEventEnteringCityCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckEnteredCity(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckEnteredCity(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckEnteredCity(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckEnteredCity(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckEnteredCity(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckEnteredCity(context);
            }

            private bool CheckEnteredCity(ScenarioEventConditionContext context)
            {
                if (!context.State.Fortifications.TryGetValue(condition.Fortification, out WorldFortification? fortification))
                {
                    return false;
                }

                WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

                return ownerId is not null &&
                       IsAffectedRace(ownerId.Value, context) &&
                       AnyStackAt(condition.Fortification, context);
            }
        }
    }
}
