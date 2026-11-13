namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitTurnStartedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        BattleUnit updatedUnit = unit with
        {
            StatusEffects = unit.StatusEffects.RemoveAll(effect => !IsRetained(effect))
        };

        return state.WithUnit(updatedUnit) with
        {
            CurrentUnitTurn = ev.Turn,
            TurnQueue = state.TurnQueue.Remove(ev.Turn)
        };

        static bool IsRetained(BattleAbstractStatusEffect effect)
        {
            return effect switch
            {
                BattleDefendStatusEffect => false,
                BattleParalyzeStatusEffect => true,
                BattlePetrifyStatusEffect => true,
                BattlePoisonStatusEffect => true,
                BattleFrostbiteStatusEffect => true,
                BattleBlisterStatusEffect => true,
                BattleRetreatStatusEffect => true,
                BattleRetreatedStatusEffect => true,
                BattleFortificationStatusEffect => true,
                BattleIncreaseDamageStatusEffect => true,
                BattleReduceDamageStatusEffect => true,
                BattleReduceInitiativeStatusEffect => true,
                BattleShatterArmorStatusEffect => true,
                BattleCureStatusEffect => true,
                BattleTransformStatusEffect => true
            };
        }
    }
}
