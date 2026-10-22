namespace Gallean.Engine;

public sealed record WorldLeaderUpgradeSelectedEvent(
    WorldStackId StackId,
    UnitModifierDefinitionId ModifierId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
