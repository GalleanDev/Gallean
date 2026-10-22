using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class BattleTurnOrderCalculator
{
    public static ImmutableArray<BattleUnitTurn> GetOpeningRoundOrder(BattleState state, IEngineContext context)
    {
        List<(BattleUnitId Id, int Initiative)> units = [];

        foreach (BattleUnit unit in OrderUnits(state))
        {
            if (unit.IsActive() && IsOpeningRoundUnit(state, context, unit))
            {
                units.Add((unit.Id, RollInitiative(state, context, unit.Id)));
            }
        }

        return BuildTurnOrder(units, BattleUnitTurnType.Doppelganger);
    }

    public static ImmutableArray<BattleUnitTurn> GetNormalRoundOrder(BattleState state, IEngineContext context)
    {
        List<(BattleUnitId Id, int Initiative)> units = [];

        foreach (BattleUnit unit in OrderUnits(state))
        {
            if (unit.IsActive())
            {
                units.Add((unit.Id, RollInitiative(state, context, unit.Id)));
            }
        }

        return BuildTurnOrder(units, BattleUnitTurnType.Normal);
    }

    public static ImmutableArray<BattleUnitTurn> GetRecoveryRoundOrder(BattleState state, IEngineContext context, BattleSide side)
    {
        List<(BattleUnitId Id, int Initiative)> units = [];

        foreach (BattleUnit unit in OrderUnits(state))
        {
            if (unit.Side == side && unit.IsActive() && IsRecoveryRoundUnit(context.Definitions.GetUnit(unit.DefinitionId)))
            {
                units.Add((unit.Id, context.UnitStats.GetBattleUnitStats(state, unit.Id).Initiative));
            }
        }

        return BuildTurnOrder(units, BattleUnitTurnType.Normal);
    }

    public static BattleUnitTurn? FindNextTurn(BattleState state)
    {
        return FindFirstAlive(state, state.TurnQueue) ?? FindFirstAlive(state, state.WaitingQueue);
    }

    private static BattleUnitTurn? FindFirstAlive(BattleState state, ImmutableArray<BattleUnitTurn> turnQueue)
    {
        foreach (BattleUnitTurn turn in turnQueue)
        {
            BattleUnit? unit = state.TryGetUnit(turn.UnitId);

            if (unit is not null && unit.IsActive())
            {
                return turn;
            }
        }

        return null;
    }

    private static List<BattleUnit> OrderUnits(BattleState state)
    {
        List<BattleUnit> units = [];

        foreach (BattleUnitId unitId in state.GetAllUnitIds().OrderBy(id => id.Value))
        {
            units.Add(state.GetUnit(unitId));
        }

        return units;
    }

    private static int RollInitiative(BattleState state, IEngineContext context, BattleUnitId unitId)
    {
        int variance = context.Globals.BattleInitiativeVariance;
        int bonus = variance > 0 ? context.Random.Next(0, variance) : 0;

        return context.UnitStats.GetBattleUnitInitiative(state, unitId, bonus);
    }

    private static ImmutableArray<BattleUnitTurn> BuildTurnOrder(List<(BattleUnitId Id, int Initiative)> units, BattleUnitTurnType type)
    {
        ImmutableArray<BattleUnitTurn>.Builder turnOrder = ImmutableArray.CreateBuilder<BattleUnitTurn>();

        foreach ((BattleUnitId id, _) in units.OrderByDescending(unit => unit.Initiative))
        {
            turnOrder.Add(new BattleUnitTurn(id, type));
        }

        return turnOrder.ToImmutable();
    }

    private static bool IsOpeningRoundUnit(BattleState state, IEngineContext context, BattleUnit unit)
    {
        UnitDefinition definition = context.Definitions.GetUnit(unit.DefinitionId);

        return definition.Attack.Type == UnitAttackType.Doppelganger &&
               HasAttackTarget(state, context, unit.Id, definition.Attack);
    }

    private static bool HasAttackTarget(BattleState state, IEngineContext context, BattleUnitId attackerId, UnitAttackDefinition attack)
    {
        foreach (BattleUnitId targetId in state.GetAllUnitIds())
        {
            BattleAttackUnitTarget target = new(targetId);
            BattleAttackCalculator.AttackInfo info = BattleAttackCalculator.GetAttackInfo(state, context, attackerId, target, attack);

            if (!info.Failed)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRecoveryRoundUnit(UnitDefinition definition)
    {
        UnitAttackTypeInfo attackInfo = definition.Attack.Type.GetInfo();
        UnitAttackTypeInfo? secondaryAttackInfo = definition.SecondaryAttack?.Type.GetInfo();

        return attackInfo.IsRecovery || secondaryAttackInfo is { IsRecovery: true };
    }
}
