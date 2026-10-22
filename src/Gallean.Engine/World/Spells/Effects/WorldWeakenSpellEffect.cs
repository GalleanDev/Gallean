namespace Gallean.Engine;

public sealed record WorldWeakenSpellEffect(UnitModifierDefinitionId Modifier) : WorldAbstractSpellEffect
{
    public override TResult Accept<TContext, TResult>(IWorldSpellEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
