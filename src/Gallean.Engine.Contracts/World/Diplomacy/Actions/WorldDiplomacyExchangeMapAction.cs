namespace Gallean.Engine.Contracts;

public sealed record WorldDiplomacyExchangeMapAction : WorldDiplomacyProposalAction
{
    public override TResult Accept<TContext, TResult>(IWorldDiplomacyActionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override TResult AcceptProposal<TContext, TResult>(
        IWorldDiplomacyProposalActionVisitor<TContext, TResult> visitor,
        TContext context)
    {
        return visitor.Visit(this, context);
    }
}
