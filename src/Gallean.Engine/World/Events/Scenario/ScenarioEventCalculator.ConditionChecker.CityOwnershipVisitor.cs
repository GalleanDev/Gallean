using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class CityOwnershipVisitor :
            IWorldFortificationVisitor<ScenarioEventConditionContext, bool>,
            IStaticInstance<CityOwnershipVisitor>
        {
            public bool Visit(WorldCapital capital, ScenarioEventConditionContext context)
            {
                return capital.CapturedById is not null && IsAffectedRace(capital.CapturedById.Value, context);
            }

            public bool Visit(WorldCity city, ScenarioEventConditionContext context)
            {
                return IsAffectedRace(city.OwnerId, context);
            }

            public bool Visit(WorldRuin ruin, ScenarioEventConditionContext context)
            {
                return false;
            }
        }
    }
}
