namespace Gallean.Engine.Contracts;

public sealed record ScenarioUnitName(ScenarioTextId Id) : UnitName
{
    public override TResult Accept<TResult>(IUnitNameVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}
