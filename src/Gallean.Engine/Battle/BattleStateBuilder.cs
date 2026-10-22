namespace Gallean.Engine;

internal sealed class BattleStateBuilder(IGameDefinitionProvider definitions)
{
    private readonly List<BattleItem> items = [];
    private readonly List<(BattleUnit Unit, GridPosition Position)> positionedUnits = [];
    private int nextAttackerIndex;
    private int nextDefenderIndex;

    public BattleStateBuilder AddItem(BattleItem item)
    {
        items.Add(item);
        return this;
    }

    public BattleStateBuilder Add(BattleUnit unit)
    {
        int index = unit.Side == BattleSide.Attacker ? nextAttackerIndex : nextDefenderIndex;
        GridPosition position = GridPosition.FromIndex(index);

        positionedUnits.Add((unit, position));

        UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);
        int advance = definition.IsLarge ? GridPosition.FlankCount : 1;

        if (unit.Side == BattleSide.Attacker)
        {
            nextAttackerIndex += advance;
        }
        else
        {
            nextDefenderIndex += advance;
        }

        return this;
    }

    public BattleStateBuilder Add(BattleUnit unit, GridPosition position)
    {
        positionedUnits.Add((unit, position));
        return this;
    }

    public BattleState Build(BattleModifiers modifiers)
    {
        UnitGrid<BattleUnit> attackerGrid = BuildGrid(positionedUnits, BattleSide.Attacker, definitions);
        UnitGrid<BattleUnit> defenderGrid = BuildGrid(positionedUnits, BattleSide.Defender, definitions);

        return new BattleState(attackerGrid, defenderGrid, default, [], [], [.. items], BattleRound.Normal(1), null, modifiers);
    }

    private static UnitGrid<BattleUnit> BuildGrid(
        List<(BattleUnit Unit, GridPosition Position)> units,
        BattleSide side,
        IGameDefinitionProvider definitions)
    {
        UnitGrid<BattleUnit>.Builder builder = new();

        foreach ((BattleUnit unit, GridPosition position) in units)
        {
            if (unit.Side == side)
            {
                UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);
                BattleUnit unitWithWards = unit with
                {
                    Wards = BattleUnitWardCalculator.GetInitialWards(unit, definitions)
                };

                builder.Place(unitWithWards, position, definition.IsLarge);
            }
        }

        return builder.Build();
    }
}
