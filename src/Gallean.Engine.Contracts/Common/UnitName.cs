namespace Gallean.Engine.Contracts;

public abstract record UnitName
{
    public abstract TResult Accept<TResult>(IUnitNameVisitor<TResult> visitor);
}
