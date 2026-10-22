namespace Gallean.Engine;

public sealed record SpellItemEffect(WorldSpellDefinitionId SpellId, WorldResourceSet CastingCost) : ItemEffect
{
    public override TResult Accept<TContext, TResult>(IItemEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
