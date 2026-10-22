using Gallean.Common;

namespace Gallean.Engine;

internal sealed class DrainLifeOverflowAttackResolver : BattleAbstractDrainAttackResolver, IStaticInstance<DrainLifeOverflowAttackResolver>
{
    protected override bool DistributesDrainOverflow => true;

    protected override int GetAttackDrainPercent(GlobalVariables globals)
    {
        return globals.DrainOverflowHealPercent;
    }
}
