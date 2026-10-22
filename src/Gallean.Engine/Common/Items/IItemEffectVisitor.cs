namespace Gallean.Engine;

public interface IItemEffectVisitor<in TContext, out TResult>
{
    TResult Visit(HealItemEffect effect, TContext context);
    TResult Visit(AttackItemEffect effect, TContext context);
    TResult Visit(ReviveItemEffect effect, TContext context);
    TResult Visit(TemporaryBoostItemEffect effect, TContext context);
    TResult Visit(PermanentBoostItemEffect effect, TContext context);
    TResult Visit(SpellItemEffect effect, TContext context);
}
