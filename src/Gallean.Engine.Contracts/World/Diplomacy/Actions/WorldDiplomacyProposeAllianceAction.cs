namespace Gallean.Engine.Contracts;

public sealed record WorldDiplomacyProposeAllianceAction : WorldDiplomacyProposalAction
{
    public override TResult AcceptProposal<TContext, TResult>(
        IWorldDiplomacyProposalActionVisitor<TContext, TResult> visitor,
        TContext context)
    {
        return visitor.Visit(this, context);
    }
}
