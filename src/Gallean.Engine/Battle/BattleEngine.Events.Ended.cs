namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleEndedEvent ev, BattleState state)
    {
        foreach (BattleUnitId unitId in state.GetAllUnitIds())
        {
            BattleUnit unit = state.GetUnit(unitId);

            state = state.WithUnit(unit with
            {
                StatusEffects = unit.StatusEffects.RemoveAll(effect => !IsRetained(effect))
            });
        }

        return state with { Outcome = new BattleOutcome(ev.WinningSide) };

        static bool IsRetained(BattleAbstractStatusEffect effect)
        {
            return effect switch
            {
                BattleDefendStatusEffect => false,
                BattleParalyzeStatusEffect => false,
                BattlePetrifyStatusEffect => false,
                BattlePoisonStatusEffect => false,
                BattleFrostbiteStatusEffect => false,
                BattleBlisterStatusEffect => false,
                BattleRetreatStatusEffect => false,
                BattleRetreatedStatusEffect => true,
                BattleFortificationStatusEffect => true,
                BattleIncreaseDamageStatusEffect => false,
                BattleReduceDamageStatusEffect => false,
                BattleReduceInitiativeStatusEffect => false,
                BattleShatterArmorStatusEffect => false,
                BattleCureStatusEffect => false,
                BattleTransformStatusEffect => true
            };
        }
    }
}
