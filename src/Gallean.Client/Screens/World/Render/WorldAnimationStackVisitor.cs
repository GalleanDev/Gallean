using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed class WorldAnimationStackVisitor : IWorldAnimationVisitor<Empty, WorldStackId?>, IStaticInstance<WorldAnimationStackVisitor>
{
    public WorldStackId? Visit(StackMove animation, Empty context) => animation.StackId;
    public WorldStackId? Visit(Effect animation, Empty context) => null;
    public WorldStackId? Visit(SpellCast animation, Empty context) => animation.SummonedStackId;
    public WorldStackId? Visit(Pause animation, Empty context) => null;
    public WorldStackId? Visit(StackBattle animation, Empty context) => null;
    public WorldStackId? Visit(BattleTransition animation, Empty context) => null;
}
