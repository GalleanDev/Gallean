using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class OwningCity(ScenarioEventOwningCityCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckFortificationOwned(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckFortificationOwned(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckFortificationOwned(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckFortificationOwned(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckFortificationOwned(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckFortificationOwned(context);
            }

            private bool CheckFortificationOwned(ScenarioEventConditionContext context)
            {
                if (!context.State.Fortifications.TryGetValue(condition.Fortification, out WorldFortification? fortification))
                {
                    return false;
                }

                return fortification.Accept(CityOwnershipVisitor.Instance, context);
            }
        }
    }
}
