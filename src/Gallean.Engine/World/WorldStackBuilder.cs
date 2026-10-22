using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal sealed class WorldStackBuilder(IGameDefinitionProvider definitions)
{
    private readonly UnitGrid<WorldUnit>.Builder builder = new();

    public WorldStackBuilder PlaceUnit(WorldUnit unit, GridPosition position)
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

        bool hasLeader = false;

        foreach ((WorldUnit unit, _) in builtUnits.GetUnits())
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            if (definition.Category == UnitCategory.Guardian)
            {
                units = null;
                error = WorldCommandError.GuardianTransferForbidden;
                return false;
            }

            if (definition.Leadership is not > 0)
            {
                continue;
            }

            if (hasLeader)
            {
                units = null;
                error = WorldCommandError.MultipleLeadersForbidden;
                return false;
            }

            hasLeader = true;
        }

        int occupiedLeadership = StackCalculator.GetOccupiedLeadership(builtUnits, definitions);
        int totalLeadership = StackCalculator.GetTotalLeadership(builtUnits, definitions);

        if (occupiedLeadership > totalLeadership)
        {
            units = null;
            error = WorldCommandError.LeadershipExceeded;
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
            throw new InvalidOperationException($"Unexpected stack composition: {error}.");
        }

        return units;
    }
}
