using Gallean.Common;

namespace Gallean.Engine;

internal sealed class CounterfeitOrdersThiefActionResolver(WorldThiefCounterfeitOrdersAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is ThiefStackTarget stackTarget)
        {
            WorldStack stack = stackTarget.Stack;

            return AttemptShuffle(
                stack.OwnerId,
                new WorldUnitGroup.Stack(stack.Id),
                stack.Units,
                context.Random);
        }
        else if (target is ThiefFortificationTarget fortificationTarget)
        {
            WorldFortification fortification = fortificationTarget.Fortification;
            WorldPlayer owner = state.Players[fortificationTarget.Owner];

            if (!WorldHelpers.IsRacePlayable(owner.Race))
            {
                return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
            }

            return AttemptShuffle(
                fortificationTarget.Owner,
                new WorldUnitGroup.Fortification(fortification.Id),
                fortification.Garrison,
                context.Random);
        }
        else
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }
    }

    private static AttemptResult AttemptShuffle(
        WorldPlayerId ownerId,
        WorldUnitGroup group,
        UnitGrid<WorldUnit> units,
        IRandomProvider random)
    {
        int aliveCount = 0;

        foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
        {
            if (unit.IsAlive)
            {
                aliveCount++;
            }
        }

        if (aliveCount < 2)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        UnitGrid<WorldUnit> shuffled = ShuffleUnits(units, random);

        return AttemptResult.Success(
            WorldThiefActionType.CounterfeitOrders,
            ownerId,
            [new WorldUnitsShuffledEvent(group, shuffled)]);
    }

    private static UnitGrid<WorldUnit> ShuffleUnits(UnitGrid<WorldUnit> units, IRandomProvider random)
    {
        List<WorldUnit> largeUnits = [];
        List<WorldUnit> smallUnits = [];

        foreach ((WorldUnit unit, GridPosition position) in units.GetUnits())
        {
            if (units.IsLargeAt(position))
            {
                largeUnits.Add(unit);
            }
            else
            {
                smallUnits.Add(unit);
            }
        }

        int[] flanks = new int[GridPosition.FlankCount];

        for (int flank = 0; flank < GridPosition.FlankCount; flank++)
        {
            flanks[flank] = flank;
        }

        random.Shuffle(flanks);

        UnitGrid<WorldUnit>.Builder builder = new();
        List<GridPosition> freePositions = [];

        for (int index = 0; index < flanks.Length; index++)
        {
            GridPosition front = GridPosition.AtFrontLine(flanks[index]);

            if (index < largeUnits.Count)
            {
                builder.Place(largeUnits[index], front, isLarge: true);
            }
            else
            {
                freePositions.Add(front);
                freePositions.Add(GridPosition.AtBackLine(flanks[index]));
            }
        }

        GridPosition[] newPositions = [.. freePositions];

        random.Shuffle(newPositions);

        for (int index = 0; index < smallUnits.Count; index++)
        {
            builder.Place(smallUnits[index], newPositions[index], isLarge: false);
        }

        return builder.Build();
    }
}
