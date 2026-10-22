namespace Gallean.Engine;

public sealed record WorldAttackSpellEffect(UnitAttackSource Source, int Damage) : WorldAbstractSpellEffect
{
    public override TResult Accept<TContext, TResult>(IWorldSpellEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
