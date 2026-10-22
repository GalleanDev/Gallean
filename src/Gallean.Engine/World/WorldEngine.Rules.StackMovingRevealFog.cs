using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class StackMovingRevealFogRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldStackMovingEvent ev, WorldState state)
        {
            return RevealFog(ev.StackId, state);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldStackRelocatedEvent ev, WorldState state)
        {
            return RevealFog(ev.StackId, state);
        }

        private EventRuleResult<WorldEvent> RevealFog(WorldStackId stackId, WorldState state)
        {
            if (!state.Stacks.TryGetValue(stackId, out WorldStack? stack))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            ImmutableBitmap before = state.Players[stack.OwnerId].FogOfWar;
            ImmutableBitmap after = FogOfWarCalculator.Update(state, stack.OwnerId, Context);
            ImmutableArray<WorldPosition> revealed = FogOfWarCalculator.Diff(before, after);

            if (revealed.IsEmpty)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(new WorldFogRevealedEvent(stack.OwnerId, revealed));
        }
    }
}
