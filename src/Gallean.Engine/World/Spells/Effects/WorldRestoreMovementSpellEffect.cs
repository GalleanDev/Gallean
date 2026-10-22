namespace Gallean.Engine;

public sealed record WorldRestoreMovementSpellEffect(int Percent) : WorldAbstractSpellEffect
{
    public override TResult Accept<TContext, TResult>(IWorldSpellEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
