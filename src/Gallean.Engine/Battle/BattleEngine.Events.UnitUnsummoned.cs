namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitUnsummonedEvent ev, BattleState state)
    {
        BattleUnit summon = state.GetUnit(ev.UnitId);
        UnitGrid<BattleUnit> grid = state.GetGrid(summon.Side);
        UnitGrid<BattleUnit>.Builder builder = new(grid);

        foreach ((BattleUnit overlayUnit, GridPosition position) in grid.GetOverlayGrid().GetUnits())
        {
            if (overlayUnit.Id == ev.UnitId)
            {
                builder.RemoveOverlay(position);
            }
        }

        UnitGrid<BattleUnit> newGrid = builder.Build();

        if (summon.Side == BattleSide.Attacker)
        {
            state = state with { AttackerGrid = newGrid };
        }
        else
        {
            state = state with { DefenderGrid = newGrid };
        }

        return state with
        {
            TurnQueue = state.TurnQueue.RemoveAll(turn => turn.UnitId == ev.UnitId),
            WaitingQueue = state.WaitingQueue.RemoveAll(turn => turn.UnitId == ev.UnitId)
        };
    }
}
