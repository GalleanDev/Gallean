using Gallean.Common;

namespace Gallean.Engine;

internal sealed class NullBattleUnitUpgradeResolver : IBattleUnitUpgradeResolver, IStaticInstance<NullBattleUnitUpgradeResolver>
{
    public BattleUnitUpgrade? UpgradeUnit(BattleUnit unit)
    {
        return null;
    }
}
