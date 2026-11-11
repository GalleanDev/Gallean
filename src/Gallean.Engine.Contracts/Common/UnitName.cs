namespace Gallean.Engine.Contracts;

public closed record UnitName
{
    public abstract TResult Accept<TResult>(IUnitNameVisitor<TResult> visitor);
}
