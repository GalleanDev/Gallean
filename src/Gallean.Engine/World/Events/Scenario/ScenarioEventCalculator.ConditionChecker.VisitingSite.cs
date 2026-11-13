namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckSiteVisited(ScenarioEventVisitingSiteCondition condition, ScenarioEventConditionContext context)
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
