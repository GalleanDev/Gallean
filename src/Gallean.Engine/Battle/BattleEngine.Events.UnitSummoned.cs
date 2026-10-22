namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitSummonedEvent ev, BattleState state)
    {
        BattleUnit summon = new(
            ev.UnitId,
            null,
            ev.DefinitionId,
            ev.Side,
            ev.Hitpoints,
            0,
            0,
            ev.Level,
            [],
            [],
            ev.SummonerId,
            [],
            Name: null,
            IsTypeLocked: false);

        summon = summon with
        {
            Wards = BattleUnitWardCalculator.GetInitialWards(summon, Context.Definitions)
        };

        bool isLarge = Context.Definitions.GetUnit(ev.DefinitionId).IsLarge;

        UnitGrid<BattleUnit> grid = new UnitGrid<BattleUnit>.Builder(state.GetGrid(ev.Side))
            .PlaceOverlay(summon, ev.Position, isLarge)
            .Build();

        if (ev.Side == BattleSide.Attacker)
        {
            return state with { AttackerGrid = grid };
        }
        else
        {
            return state with { DefenderGrid = grid };
        }
    }
}
