namespace Gallean.Engine;

public sealed record WorldThiefStealSpellAction(WorldPosition TargetPosition, WorldSpellDefinitionId SpellId) : WorldThiefAction
{
    public override TResult Accept<TContext, TResult>(IWorldThiefActionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
