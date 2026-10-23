using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldSpellCastEvent ev, WorldState state)
    {
        if (ev.CasterId is not null)
        {
            WorldPlayer player = state.Players[ev.CasterId.Value];

            WorldPlayer updatedPlayer = player with
            {
                Resources = ResourceSetCalculator.Subtract(player.Resources, ev.Cost),
                TurnActions = RecordCast(player.TurnActions, ev)
            };

            return state with { Players = state.Players.SetItem(player.Id, updatedPlayer) };
        }
        else
        {
            return state;
        }
    }

    private static WorldPlayerTurnActions RecordCast(WorldPlayerTurnActions actions, WorldSpellCastEvent ev)
    {
        return ev.Source switch
        {
            WorldSpellCastSource.Player => actions with { SpellCasts = IncrementSpellCasts(actions.SpellCasts, ev.SpellId) },
            WorldSpellCastSource.Scenario => actions,
            WorldSpellCastSource.Item => actions
        };
    }

    private static ImmutableSortedDictionary<WorldSpellDefinitionId, int> IncrementSpellCasts(
        ImmutableSortedDictionary<WorldSpellDefinitionId, int> casts,
        WorldSpellDefinitionId spellId)
    {
        casts.TryGetValue(spellId, out int count);

        return casts.SetItem(spellId, count + 1);
    }
}
