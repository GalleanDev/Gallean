using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private sealed class BattleTransitionVisitor : IWorldAnimationVisitor<Empty, BattleTransition?>, IStaticInstance<BattleTransitionVisitor>
    {
        public BattleTransition? Visit(StackMove animation, Empty context) => null;
        public BattleTransition? Visit(Effect animation, Empty context) => null;
        public BattleTransition? Visit(SpellCast animation, Empty context) => null;
        public BattleTransition? Visit(Pause animation, Empty context) => null;
        public BattleTransition? Visit(StackBattle animation, Empty context) => null;
        public BattleTransition? Visit(BattleTransition animation, Empty context) => animation;
    }
}
