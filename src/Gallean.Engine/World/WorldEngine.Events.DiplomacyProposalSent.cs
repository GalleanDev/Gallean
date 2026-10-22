using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldDiplomacyProposalSentEvent ev, WorldState state)
    {
        WorldDiplomacyProposal proposal = ev.Proposal;
        WorldPlayer targetPlayer = state.Players[proposal.TargetPlayerId];
        WorldPlayer updatedTargetPlayer = targetPlayer with
        {
            IncomingDiplomacyProposals = targetPlayer.IncomingDiplomacyProposals.Add(proposal)
        };

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players.SetItem(targetPlayer.Id, updatedTargetPlayer);

        if (proposal.Action is WorldDiplomacyExchangeMapAction)
        {
            WorldPlayer player = state.Players[proposal.PlayerId];
            WorldPlayerTurnActions turnActions = player.TurnActions with
            {
                MapExchangeProposals = player.TurnActions.MapExchangeProposals.Add(targetPlayer.Id)
            };

            players = players.SetItem(player.Id, player with { TurnActions = turnActions });
        }

        return state with { Players = players };
    }
}
