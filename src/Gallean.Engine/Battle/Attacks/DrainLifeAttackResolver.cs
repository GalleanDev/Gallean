using Gallean.Common;

namespace Gallean.Engine;

internal sealed class DrainLifeAttackResolver : BattleAbstractDrainAttackResolver, IStaticInstance<DrainLifeAttackResolver>
{
    protected override int GetAttackDrainPercent(GlobalVariables globals)
    {
        return globals.DrainHealPercent;
    }
}
