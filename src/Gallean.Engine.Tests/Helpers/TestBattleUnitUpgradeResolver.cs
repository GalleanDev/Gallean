namespace Gallean.Engine.Tests;

internal sealed class TestBattleUnitUpgradeResolver : IBattleUnitUpgradeResolver
{
    private readonly Dictionary<UnitDefinitionId, BattleUnitUpgrade> upgrades = [];

    public BattleUnitUpgrade? UpgradeUnit(BattleUnit unit)
    {
        return upgrades.GetValueOrDefault(unit.DefinitionId);
    }

    public TestBattleUnitUpgradeResolver WithUpgrade(UnitDefinitionId definitionId, BattleUnitUpgrade upgrade)
    {
        upgrades[definitionId] = upgrade;
        return this;
    }
}
