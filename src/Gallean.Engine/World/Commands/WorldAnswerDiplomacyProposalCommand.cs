using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldAnswerDiplomacyProposalCommand(
    WorldDiplomacyProposal Proposal,
    bool Accepted) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!state.Players.TryGetValue(Proposal.TargetPlayerId, out WorldPlayer? targetPlayer))
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyProposalNotFound);
        }

        if (!targetPlayer.IncomingDiplomacyProposals.Contains(Proposal))
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyProposalNotFound);
        }

        if (Accepted)
        {
            DiplomacyProposalCalculator.AcceptInfo info = DiplomacyProposalCalculator.GetAcceptInfo(Proposal, state, context);

            if (info.Error is not null)
            {
                return WorldCommandResult.Fail(info.Error.Value);
            }
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldDiplomacyProposalAnsweredEvent(Proposal, Accepted)]);
    }
}
