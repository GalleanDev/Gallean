using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class FortificationCapturingReceiveSpellsRule(IEngineContext context) : WorldEventRule(context)
    {
        private const int ChanceMax = 100;

        public override EventRuleResult<WorldEvent> Visit(WorldFortificationCapturingEvent ev, WorldState state)
        {
            if (state.Fortifications[ev.FortificationId] is not WorldCapital)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlayer capturedPlayer = state.Players[ev.PreviousOwnerId];
            WorldPlayer capturingPlayer = state.Players[ev.NewOwnerId];

            if (!WorldHelpers.IsRacePlayable(capturedPlayer.Race) || !WorldHelpers.IsRacePlayable(capturingPlayer.Race))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (Context.Random.Next(ChanceMax) >= Context.Globals.CapitalCaptureSpellChance)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            ImmutableArray<WorldSpellDefinitionId> spells =
                [.. capturedPlayer.Spellbook.Acquired.Except(capturingPlayer.Spellbook.Acquired)];

            if (spells.IsEmpty)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(new WorldSpellsReceivedEvent(capturingPlayer.Id, spells));
        }
    }
}
