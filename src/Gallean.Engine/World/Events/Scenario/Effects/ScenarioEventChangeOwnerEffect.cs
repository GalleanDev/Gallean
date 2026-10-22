namespace Gallean.Engine;

public sealed record ScenarioEventChangeOwnerEffect(
    ScenarioStackReference Stack,
    WorldSubraceId Subrace,
    bool FirstOnly,
    bool PlayAnimation) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
