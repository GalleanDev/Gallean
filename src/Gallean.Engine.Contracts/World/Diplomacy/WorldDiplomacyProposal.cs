namespace Gallean.Engine.Contracts;

public sealed record WorldDiplomacyProposal(
    WorldPlayerId PlayerId,
    WorldPlayerId TargetPlayerId,
    WorldDiplomacyProposalAction Action);
