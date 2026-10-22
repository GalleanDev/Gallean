namespace Gallean.Engine;

public sealed record WorldSummonStackSpellEffect(UnitDefinitionId Unit) : WorldAbstractSpellEffect
{
    public override TResult Accept<TContext, TResult>(IWorldSpellEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
