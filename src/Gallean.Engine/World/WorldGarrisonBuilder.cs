using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal sealed class WorldGarrisonBuilder(IGameDefinitionProvider definitions, UnitGrid<WorldUnit> currentUnits, int capacity)
{
    private readonly UnitGrid<WorldUnit>.Builder builder = new();

    private readonly int allowedCapacity = Math.Max(capacity, GarrisonCalculator.GetOccupiedCapacity(currentUnits, definitions));

    public WorldGarrisonBuilder PlaceUnit(WorldUnit unit, GridPosition position)
    {
        builder.Place(unit, position, definitions.GetUnit(unit.DefinitionId).IsLarge);
        return this;
    }

    public bool TryPlaceUnit(WorldUnit unit, GridPosition position)
    {
        return builder.TryPlace(unit, position, definitions.GetUnit(unit.DefinitionId).IsLarge);
    }

    public bool TryBuild(
        [NotNullWhen(true)] out UnitGrid<WorldUnit>? units,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        UnitGrid<WorldUnit> builtUnits = builder.Build();

        if (GarrisonCalculator.GetOccupiedCapacity(builtUnits, definitions) > allowedCapacity)
        {
            units = null;
            error = WorldCommandError.GarrisonCapacityExceeded;
            return false;
        }

        units = builtUnits;
        error = null;
        return true;
    }

    public UnitGrid<WorldUnit> Build()
    {
        if (!TryBuild(out UnitGrid<WorldUnit>? units, out WorldCommandError? error))
        {
            throw new InvalidOperationException($"Unexpected garrison composition: {error}.");
        }

        return units;
    }
}
