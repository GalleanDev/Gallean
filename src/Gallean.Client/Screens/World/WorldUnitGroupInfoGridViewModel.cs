using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldUnitGroupInfoGridViewModel(IReadOnlyList<WorldUnitGroupInfoUnitViewModel> Units, bool IsFacingLeft)
{
    public static WorldUnitGroupInfoGridViewModel Empty { get; } = new([], IsFacingLeft: false);

    public static WorldUnitGroupInfoGridViewModel Create(
        UnitGrid<WorldUnit> grid,
        bool isFacingLeft,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        List<WorldUnitGroupInfoUnitViewModel> units = [];

        foreach ((WorldUnit unit, GridPosition position) in grid.GetUnits())
        {
            bool isLarge = grid.IsLargeAt(position);

            units.Add(WorldUnitGroupInfoUnitViewModel.Create(unit, position, isLarge, isFacingLeft, definitions, texts, stats));
        }

        return new WorldUnitGroupInfoGridViewModel(units, isFacingLeft);
    }
}
