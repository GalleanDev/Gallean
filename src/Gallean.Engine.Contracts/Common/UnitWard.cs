namespace Gallean.Engine.Contracts;

public abstract record UnitWard(UnitWardCategory Category)
{
    public abstract TResult Accept<TContext, TResult>(IUnitWardVisitor<TContext, TResult> visitor, TContext context);
}
