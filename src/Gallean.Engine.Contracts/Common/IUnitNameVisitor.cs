namespace Gallean.Engine.Contracts;

public interface IUnitNameVisitor<out TResult>
{
    TResult Visit(LiteralUnitName name);
    TResult Visit(ScenarioUnitName name);
    TResult Visit(LeaderUnitName name);
}
