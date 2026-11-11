namespace Gallean.Engine;

public closed record UnitUpgrade
{
    public sealed record TypeUpgrade(UnitDefinitionId ToUnitId) : UnitUpgrade;

    public sealed record DynamicUpgrade : UnitUpgrade;

    public sealed record Blocked : UnitUpgrade;
}
