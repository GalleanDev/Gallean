using Gallean.Common;

namespace Gallean.Engine;

internal sealed class BattleAttackTargetUnitIdVisitor : IBattleAttackTargetVisitor<Empty, BattleUnitId?>, IStaticInstance<BattleAttackTargetUnitIdVisitor>
{
    public BattleUnitId? Visit(BattleAttackUnitTarget target, Empty context)
    {
        return target.UnitId;
    }

    public BattleUnitId? Visit(BattleAttackTileTarget target, Empty context)
    {
        return null;
    }
}
