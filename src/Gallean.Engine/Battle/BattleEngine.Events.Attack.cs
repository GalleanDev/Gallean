namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitAttackEvent ev, BattleState state)
    {
        UnitDefinition definition = Context.Definitions.GetUnit(state.GetUnit(ev.AttackerId).DefinitionId);

        bool hasSecondAttack = definition.AttacksTwice && state.CurrentUnitTurn.Type switch
        {
            BattleUnitTurnType.Normal => true,
            BattleUnitTurnType.SecondAttack => false,
            BattleUnitTurnType.GrantedAttack => true,
            BattleUnitTurnType.Resumed => true,
            BattleUnitTurnType.Doppelganger => false
        };

        if (hasSecondAttack && ev.AttackIds[0] == definition.Attack.Id)
        {
            state = state with
            {
                TurnQueue = state.TurnQueue.Insert(0, new BattleUnitTurn(ev.AttackerId, BattleUnitTurnType.SecondAttack))
            };
        }

        return state;
    }
}
