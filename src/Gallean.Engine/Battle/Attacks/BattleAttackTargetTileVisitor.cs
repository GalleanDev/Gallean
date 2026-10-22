using Gallean.Common;

namespace Gallean.Engine;

internal sealed class BattleAttackTargetTileVisitor : IBattleAttackTargetVisitor<Empty, BattleAttackTileTarget?>, IStaticInstance<BattleAttackTargetTileVisitor>
{
    public BattleAttackTileTarget? Visit(BattleAttackUnitTarget target, Empty context)
    {
        return null;
    }

    public BattleAttackTileTarget? Visit(BattleAttackTileTarget target, Empty context)
    {
        return target;
    }
}
