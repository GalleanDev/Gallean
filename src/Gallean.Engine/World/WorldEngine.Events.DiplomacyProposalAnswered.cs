namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldDiplomacyProposalAnsweredEvent ev, WorldState state)
    {
        WorldPlayer targetPlayer = state.Players[ev.Proposal.TargetPlayerId];
        WorldPlayer updatedTargetPlayer = targetPlayer with
        {
            IncomingDiplomacyProposals = targetPlayer.IncomingDiplomacyProposals.Remove(ev.Proposal)
        };

        return state with { Players = state.Players.SetItem(targetPlayer.Id, updatedTargetPlayer) };
    }
}
