namespace Gallean.Engine;

public sealed record WorldDiplomacyProposalAnsweredEvent(
    WorldDiplomacyProposal Proposal,
    bool Accepted) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
