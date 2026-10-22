using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal interface IWorldAnimationVisitor<in TContext, out TResult>
{
    TResult Visit(StackMove animation, TContext context);
    TResult Visit(Effect animation, TContext context);
    TResult Visit(SpellCast animation, TContext context);
    TResult Visit(Pause animation, TContext context);
    TResult Visit(StackBattle animation, TContext context);
    TResult Visit(BattleTransition animation, TContext context);
}
