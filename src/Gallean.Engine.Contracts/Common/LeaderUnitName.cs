namespace Gallean.Engine.Contracts;

public sealed record LeaderUnitName(LeaderNameId Id) : UnitName
{
    public override TResult Accept<TResult>(IUnitNameVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}
