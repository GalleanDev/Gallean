using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ProposeDiplomacyActionResolver(WorldDiplomacyProposalAction action) : IWorldDiplomacyActionResolver
{
    public WorldCommandResult Resolve(WorldState state, IEngineContext context, WorldPlayerId player, WorldPlayerId targetPlayer)
    {
        WorldDiplomacyProposal proposal = new(player, targetPlayer, action);
        DiplomacyProposalCalculator.SendInfo info = DiplomacyProposalCalculator.GetSendInfo(proposal, state, context);

        if (info.Error is not null)
        {
            return WorldCommandResult.Fail(info.Error.Value);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldDiplomacyProposalSentEvent(proposal)]);
    }
}
