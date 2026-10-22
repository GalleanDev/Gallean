namespace Gallean.Engine;

internal static partial class StackAutoFitCalculator
{
    public sealed record Result(
        UnitGrid<WorldUnit> StackUnits,
        UnitGrid<WorldUnit> GarrisonUnits,
        WorldInventory StackInventory,
        WorldInventory GarrisonInventory,
        LeaderEquipment Equipment);
}
