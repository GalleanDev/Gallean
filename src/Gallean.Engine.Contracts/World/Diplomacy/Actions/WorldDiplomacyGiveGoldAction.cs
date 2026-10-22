namespace Gallean.Engine.Contracts;

public sealed record WorldDiplomacyGiveGoldAction(int Gold) : WorldDiplomacyAction
{
    public override TResult Accept<TContext, TResult>(IWorldDiplomacyActionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
