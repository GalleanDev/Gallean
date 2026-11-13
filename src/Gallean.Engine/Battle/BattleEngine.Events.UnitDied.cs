namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitDiedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with
        {
            StatusEffects = unit.StatusEffects.RemoveAll(effect => !IsRetained(effect))
        });

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
                BattleShatterArmorStatusEffect => true,
                BattleCureStatusEffect => true,
                BattleTransformStatusEffect => true
            };
        }
    }
}
