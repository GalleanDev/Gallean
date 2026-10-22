using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class BattleAnimationStateBuilder
{
    private sealed class AttackTargetSideVisitor :
        IBattleAttackTargetVisitor<IReadOnlyDictionary<BattleUnitId, UnitTimeline>, BattleSide>,
        IStaticInstance<AttackTargetSideVisitor>
    {
        public BattleSide Visit(BattleAttackUnitTarget target, IReadOnlyDictionary<BattleUnitId, UnitTimeline> timelines)
        {
            UnitTimeline timeline = timelines[target.UnitId];

            return timeline.Unit.Footprint.Side;
        }

        public BattleSide Visit(BattleAttackTileTarget target, IReadOnlyDictionary<BattleUnitId, UnitTimeline> timelines)
        {
            return target.Side;
        }
    }
}
