using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal static class WorldUnitGroupCalculator
{
    private sealed record UnitPlacement(
        WorldUnit Unit,
        WorldUnitGroup Group,
        GridPosition Position);

    public static bool TryTransferUnit(
        WorldState state,
        IEngineContext context,
        WorldUnitGroup source,
        WorldUnitGroup target,
        WorldUnitId unitId,
        GridPosition targetPosition,
        [NotNullWhen(true)] out WorldState? newState,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        newState = null;

        IGameDefinitionProvider definitions = context.Definitions;

        if (!UnitGrid<WorldUnit>.IsValidPosition(targetPosition))
        {
            error = WorldCommandError.InvalidUnitPosition;
            return false;
        }

        UnitGrid<WorldUnit> sourceUnits = GetUnits(state, source);
        UnitGrid<WorldUnit> targetUnits = GetUnits(state, target);

        if (!sourceUnits.TryGetUnit(unitId, out (WorldUnit Unit, GridPosition Position) moved))
        {
            error = WorldCommandError.UnitNotFound;
            return false;
        }

        ImmutableArray<UnitPlacement> placements = PlanSwap(
            definitions,
            source,
            sourceUnits,
            target,
            targetUnits,
            moved.Unit,
            moved.Position,
            targetPosition);

        if (HasLeader(definitions, placements))
        {
            error = WorldCommandError.LeaderTransferForbidden;
            return false;
        }

        if (!TryBuildGroup(state, context, source, sourceUnits, placements, out UnitGrid<WorldUnit>? newSourceUnits, out error))
        {
            return false;
        }

        if (!TryBuildGroup(state, context, target, targetUnits, placements, out UnitGrid<WorldUnit>? newTargetUnits, out error))
        {
            return false;
        }

        LeaderEquipment sourceEquipment = GetEquipment(state, source);
        newState = WithUnits(
            state, source,
            LeaderEquipmentCalculator.UpdateUnitModifiers(newSourceUnits, sourceEquipment, definitions));

        LeaderEquipment targetEquipment = GetEquipment(state, target);
        newState = WithUnits(
            newState, target,
            LeaderEquipmentCalculator.UpdateUnitModifiers(newTargetUnits, targetEquipment, definitions));

        error = null;
        return true;
    }

    public static bool TryRepositionUnit(
        WorldState state,
        IEngineContext context,
        WorldUnitGroup group,
        WorldUnitId unitId,
        GridPosition targetPosition,
        [NotNullWhen(true)] out WorldState? newState,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        newState = null;

        IGameDefinitionProvider definitions = context.Definitions;

        if (!UnitGrid<WorldUnit>.IsValidPosition(targetPosition))
        {
            error = WorldCommandError.InvalidUnitPosition;
            return false;
        }

        UnitGrid<WorldUnit> units = GetUnits(state, group);

        if (!units.TryGetUnit(unitId, out (WorldUnit Unit, GridPosition Position) moved))
        {
            error = WorldCommandError.UnitNotFound;
            return false;
        }

        ImmutableArray<UnitPlacement> placements = PlanSwap(
            definitions,
            group,
            units,
            group,
            units,
            moved.Unit,
            moved.Position,
            targetPosition);

        if (!TryBuildGroup(state, context, group, units, placements, out UnitGrid<WorldUnit>? newUnits, out error))
        {
            return false;
        }

        newState = WithUnits(state, group, newUnits);

        return true;
    }

    public static bool TryGetCity(
        WorldState state,
        WorldUnitGroup group,
        [NotNullWhen(true)] out WorldFortification? city,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        city = null;

        WorldFortification? fortification;

        if (group is WorldUnitGroup.Stack stackGroup)
        {
            if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, stackGroup.StackId, out WorldStack? stack))
            {
                error = WorldCommandError.StackNotFound;
                return false;
            }

            WorldPlan plan = WorldPlan.For(state);
            fortification = plan.GetFortificationAt(stack.Position);

            if (fortification is not (WorldCity or WorldCapital))
            {
                error = WorldCommandError.StackNotInCity;
                return false;
            }

            if (FortificationCalculator.GetOwner(fortification) != stack.OwnerId)
            {
                error = WorldCommandError.StackNotInCity;
                return false;
            }
        }
        else if (group is WorldUnitGroup.Fortification fortificationGroup)
        {
            if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, fortificationGroup.FortificationId, out fortification))
            {
                error = WorldCommandError.GarrisonNotFound;
                return false;
            }

            if (fortification is not (WorldCity or WorldCapital))
            {
                error = WorldCommandError.GarrisonNotFound;
                return false;
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(group), group, null);
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            error = WorldCommandError.CityRioting;
            return false;
        }

        city = fortification;
        error = null;

        return true;
    }

    public static UnitGrid<WorldUnit> GetUnits(WorldState state, WorldUnitGroup group)
    {
        return group switch
        {
            WorldUnitGroup.Stack data => state.Stacks[data.StackId].Units,
            WorldUnitGroup.Fortification data => state.Fortifications[data.FortificationId].Garrison,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };
    }

    public static LeaderEquipment GetEquipment(WorldState state, WorldUnitGroup group)
    {
        return group switch
        {
            WorldUnitGroup.Stack data => state.Stacks[data.StackId].Equipment,
            WorldUnitGroup.Fortification => LeaderEquipment.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };
    }

    public static WorldPlayerId? GetOwner(WorldState state, WorldUnitGroup group)
    {
        return group switch
        {
            WorldUnitGroup.Stack data => state.Stacks[data.StackId].OwnerId,
            WorldUnitGroup.Fortification data => FortificationCalculator.GetOwner(state.Fortifications[data.FortificationId]),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };
    }

    public static WorldInventory GetInventory(WorldState state, WorldUnitGroup group)
    {
        return group switch
        {
            WorldUnitGroup.Stack data => state.Stacks[data.StackId].Inventory,
            WorldUnitGroup.Fortification data => state.Fortifications[data.FortificationId].Inventory,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };
    }

    public static WorldState WithUnits(WorldState state, WorldUnitGroup group, UnitGrid<WorldUnit> units)
    {
        switch (group)
        {
            case WorldUnitGroup.Stack stackGroup:
                WorldStack stack = state.Stacks[stackGroup.StackId];
                return state with { Stacks = state.Stacks.SetItem(stack.Id, stack with { Units = units }) };

            case WorldUnitGroup.Fortification fortificationGroup:
                WorldFortification fortification = state.Fortifications[fortificationGroup.FortificationId];
                return state with { Fortifications = state.Fortifications.SetItem(fortification.Id, fortification with { Garrison = units }) };

            default:
                throw new ArgumentOutOfRangeException(nameof(group), group, null);
        }
    }

    public static WorldState WithInventory(WorldState state, WorldUnitGroup group, WorldInventory inventory)
    {
        switch (group)
        {
            case WorldUnitGroup.Stack stackGroup:
                WorldStack stack = state.Stacks[stackGroup.StackId];
                return state with { Stacks = state.Stacks.SetItem(stack.Id, stack with { Inventory = inventory }) };

            case WorldUnitGroup.Fortification fortificationGroup:
                WorldFortification fortification = state.Fortifications[fortificationGroup.FortificationId];
                return state with { Fortifications = state.Fortifications.SetItem(fortification.Id, fortification with { Inventory = inventory }) };

            default:
                throw new ArgumentOutOfRangeException(nameof(group), group, null);
        }
    }

    private static ImmutableArray<UnitPlacement> PlanSwap(
        IGameDefinitionProvider definitions,
        WorldUnitGroup source,
        UnitGrid<WorldUnit> sourceUnits,
        WorldUnitGroup target,
        UnitGrid<WorldUnit> targetUnits,
        WorldUnit movedUnit,
        GridPosition sourcePosition,
        GridPosition targetPosition)
    {
        if (definitions.GetUnit(movedUnit.DefinitionId).IsLarge || targetUnits.IsLargeAt(targetPosition))
        {
            return PlanFlankSwap(definitions, source, sourceUnits, target, targetUnits, movedUnit, sourcePosition, targetPosition);
        }
        else
        {
            return PlanSlotSwap(source, target, targetUnits, movedUnit, sourcePosition, targetPosition);
        }
    }

    private static ImmutableArray<UnitPlacement> PlanSlotSwap(
        WorldUnitGroup source,
        WorldUnitGroup target,
        UnitGrid<WorldUnit> targetUnits,
        WorldUnit movedUnit,
        GridPosition sourcePosition,
        GridPosition targetPosition)
    {
        WorldUnit? targetUnit = targetUnits[targetPosition];

        if (targetUnit is null || targetUnit.Id == movedUnit.Id)
        {
            return [new UnitPlacement(movedUnit, target, targetPosition)];
        }
        else
        {
            return
            [
                new UnitPlacement(movedUnit, target, targetPosition),
                new UnitPlacement(targetUnit, source, sourcePosition)
            ];
        }
    }

    private static ImmutableArray<UnitPlacement> PlanFlankSwap(
        IGameDefinitionProvider definitions,
        WorldUnitGroup source,
        UnitGrid<WorldUnit> sourceUnits,
        WorldUnitGroup target,
        UnitGrid<WorldUnit> targetUnits,
        WorldUnit movedUnit,
        GridPosition sourcePosition,
        GridPosition targetPosition)
    {
        ImmutableArray<UnitPlacement>.Builder placements = ImmutableArray.CreateBuilder<UnitPlacement>();

        placements.Add(new UnitPlacement(movedUnit, target, targetPosition));

        if (!definitions.GetUnit(movedUnit.DefinitionId).IsLarge)
        {
            WorldUnit? flankUnit = sourceUnits[GetOppositeLinePosition(sourcePosition)];

            if (flankUnit is not null)
            {
                placements.Add(new UnitPlacement(flankUnit, target, GetOppositeLinePosition(targetPosition)));
            }
        }

        foreach ((WorldUnit unit, GridPosition position) in targetUnits.GetUnits())
        {
            if (position.Flank == targetPosition.Flank && unit.Id != movedUnit.Id)
            {
                placements.Add(new UnitPlacement(unit, source, new GridPosition(position.Line, sourcePosition.Flank)));
            }
        }

        return placements.ToImmutable();
    }

    private static GridPosition GetOppositeLinePosition(GridPosition position)
    {
        if (position.IsFrontLine)
        {
            return GridPosition.AtBackLine(position.Flank);
        }
        else
        {
            return GridPosition.AtFrontLine(position.Flank);
        }
    }

    private static bool HasLeader(IGameDefinitionProvider definitions, ImmutableArray<UnitPlacement> placements)
    {
        foreach (UnitPlacement placement in placements)
        {
            if (definitions.GetUnit(placement.Unit.DefinitionId).Leadership is > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBuildGroup(
        WorldState state,
        IEngineContext context,
        WorldUnitGroup group,
        UnitGrid<WorldUnit> units,
        ImmutableArray<UnitPlacement> placements,
        [NotNullWhen(true)] out UnitGrid<WorldUnit>? newUnits,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        if (group is WorldUnitGroup.Stack)
        {
            WorldStackBuilder builder = new(context.Definitions);

            foreach ((WorldUnit unit, GridPosition unitPosition) in units.GetUnits())
            {
                if (!FindUnit(placements, unit))
                {
                    if (!builder.TryPlaceUnit(unit, unitPosition))
                    {
                        newUnits = null;
                        error = WorldCommandError.TargetOccupied;
                        return false;
                    }
                }
            }

            foreach (UnitPlacement placement in placements)
            {
                if (placement.Group == group)
                {
                    if (!builder.TryPlaceUnit(placement.Unit, placement.Position))
                    {
                        newUnits = null;
                        error = WorldCommandError.TargetOccupied;
                        return false;
                    }
                }
            }

            return builder.TryBuild(out newUnits, out error);
        }
        else
        {
            WorldGarrisonBuilder builder = CreateGarrisonBuilder(state, context, group, units);

            foreach ((WorldUnit unit, GridPosition unitPosition) in units.GetUnits())
            {
                if (!FindUnit(placements, unit))
                {
                    if (!builder.TryPlaceUnit(unit, unitPosition))
                    {
                        newUnits = null;
                        error = WorldCommandError.TargetOccupied;
                        return false;
                    }
                }
            }

            foreach (UnitPlacement placement in placements)
            {
                if (placement.Group == group)
                {
                    if (!builder.TryPlaceUnit(placement.Unit, placement.Position))
                    {
                        newUnits = null;
                        error = WorldCommandError.TargetOccupied;
                        return false;
                    }
                }
            }

            return builder.TryBuild(out newUnits, out error);
        }
    }

    private static WorldGarrisonBuilder CreateGarrisonBuilder(
        WorldState state,
        IEngineContext context,
        WorldUnitGroup group,
        UnitGrid<WorldUnit> units)
    {
        WorldUnitGroup.Fortification fortificationGroup = (WorldUnitGroup.Fortification)group;
        WorldFortification fortification = state.Fortifications[fortificationGroup.FortificationId];
        int capacity = GarrisonCalculator.GetTotalCapacity(fortification, context.Globals);

        return new WorldGarrisonBuilder(context.Definitions, units, capacity);
    }

    private static bool FindUnit(ImmutableArray<UnitPlacement> placements, WorldUnit unit)
    {
        foreach (UnitPlacement placement in placements)
        {
            if (placement.Unit.Id == unit.Id)
            {
                return true;
            }
        }

        return false;
    }
}
