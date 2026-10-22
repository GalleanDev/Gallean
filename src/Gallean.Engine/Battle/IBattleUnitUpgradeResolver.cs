namespace Gallean.Engine;

internal interface IBattleUnitUpgradeResolver
{
    BattleUnitUpgrade? UpgradeUnit(BattleUnit unit);
}
