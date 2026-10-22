namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class VisitingSite(ScenarioEventVisitingSiteCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckSiteVisited(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckSiteVisited(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckSiteVisited(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckSiteVisited(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckSiteVisited(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckSiteVisited(context);
            }

            private bool CheckSiteVisited(ScenarioEventConditionContext context)
            {
                if (!context.State.Sites.TryGetValue(condition.Site, out WorldSite? site))
                {
                    return false;
                }

                foreach (WorldPlayerId visitorId in site.VisitorIds)
                {
                    if (IsAffectedRace(visitorId, context))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
