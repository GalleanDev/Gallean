namespace Gallean.Engine.Contracts;

public sealed record LiteralUnitName(string Text) : UnitName
{
    public override TResult Accept<TResult>(IUnitNameVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}
