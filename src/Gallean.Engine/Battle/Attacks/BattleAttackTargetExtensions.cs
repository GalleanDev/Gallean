using Gallean.Common;

namespace Gallean.Engine;

public static class BattleAttackTargetExtensions
{
    extension(BattleAttackTarget target)
    {
        public BattleUnitId? GetUnitId()
        {
            return target.Accept(BattleAttackTargetUnitIdVisitor.Instance, Empty.Value);
        }

        public BattleAttackTileTarget? GetTileTarget()
        {
            return target.Accept(BattleAttackTargetTileVisitor.Instance, Empty.Value);
        }
    }

    extension(IEnumerable<BattleAttackTarget> targets)
    {
        public IEnumerable<BattleUnitId> GetUnitIds()
        {
            foreach (BattleAttackTarget target in targets)
            {
                if (target.GetUnitId() is { } unitId)
                {
                    yield return unitId;
                }
            }
        }

        public IEnumerable<BattleAttackTileTarget> GetTileTargets()
        {
            foreach (BattleAttackTarget target in targets)
            {
                if (target.GetTileTarget() is { } tileTarget)
                {
                    yield return tileTarget;
                }
            }
        }
    }
}
