using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldRoundStartedEvent ev, WorldState state)
    {
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications = state.Fortifications;

        foreach (WorldFortification fortification in fortifications.Values)
        {
            if (fortification is WorldRuin ruin)
            {
                fortifications = fortifications.SetItem(ruin.Id, ruin with
                {
                    Garrison = WorldHealCalculator.RegenerateGarrison(state, ruin, Context)
                });
            }
        }

        return state with
        {
            Fortifications = fortifications,
            TurnInfo = state.TurnInfo with { TurnNumber = state.TurnInfo.TurnNumber + 1 }
        };
    }
}
