namespace Gallean.Engine.Contracts;

public sealed record UnitAttackSourceWard(UnitAttackSource Source, UnitWardCategory Category) : UnitWard(Category)
{
    public override TResult Accept<TContext, TResult>(IUnitWardVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
