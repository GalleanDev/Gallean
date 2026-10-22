using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class FogConcealedRevealVisionRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldFogConcealedEvent ev, WorldState state)
        {
            ImmutableBitmap before = state.Players[ev.PlayerId].FogOfWar;
            ImmutableBitmap after = FogOfWarCalculator.Update(state, ev.PlayerId, Context);
            ImmutableArray<WorldPosition> revealed = FogOfWarCalculator.Diff(before, after);

            if (revealed.IsEmpty)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(new WorldFogRevealedEvent(ev.PlayerId, revealed));
        }
    }
}
