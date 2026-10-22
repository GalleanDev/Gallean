using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal interface ISpellCastLayerVisitor<in TContext, out TResult>
{
    TResult Visit(SpellCastLayer.Effect layer, TContext context);
    TResult Visit(SpellCastLayer.Summon layer, TContext context);
}
