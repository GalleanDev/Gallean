namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleRoundStartedEvent ev, BattleState state)
    {
        foreach (BattleUnitId unitId in state.GetAllUnitIds())
        {
            BattleUnit unit = state.GetUnit(unitId);

            state = state.WithUnit(unit with
            {
                StatusEffects = unit.StatusEffects.RemoveAll(effect => !IsRetained(effect, ev.Round.Type))
            });
        }

        return state with
        {
            TurnQueue = ev.TurnOrder,
            WaitingQueue = [],
            CurrentRound = ev.Round
        };

        static bool IsRetained(BattleAbstractStatusEffect effect, BattleRoundType roundType)
        {
            return effect switch
            {
                BattleDefendStatusEffect => true,
                BattleParalyzeStatusEffect => true,
                BattlePetrifyStatusEffect => true,
                BattlePoisonStatusEffect => roundType != BattleRoundType.Recovery,
                BattleFrostbiteStatusEffect => roundType != BattleRoundType.Recovery,
                BattleBlisterStatusEffect => roundType != BattleRoundType.Recovery,
                BattleRetreatStatusEffect => true,
                BattleRetreatedStatusEffect => true,
                BattleFortificationStatusEffect => true,
                BattleIncreaseDamageStatusEffect => true,
                BattleReduceDamageStatusEffect => true,
                BattleReduceInitiativeStatusEffect reduceInitiative => reduceInitiative.IsInfinite,
                BattleShatterArmorStatusEffect => true,
                BattleCureStatusEffect => true,
                BattleTransformStatusEffect => true
            };
        }
    }
}
