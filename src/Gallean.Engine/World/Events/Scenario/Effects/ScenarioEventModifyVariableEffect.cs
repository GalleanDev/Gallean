namespace Gallean.Engine;

public sealed record ScenarioEventModifyVariableEffect(
    ScenarioVariableId Variable,
    ScenarioVariableOperation Operation,
    int Value) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
