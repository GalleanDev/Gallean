namespace Gallean.Engine;

public sealed record PermanentBoostItemEffect(UnitModifierDefinitionId ModifierId) : ItemEffect
{
    public override TResult Accept<TContext, TResult>(IItemEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
