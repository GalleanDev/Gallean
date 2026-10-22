using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class EffectExpirationVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<EffectExpirationVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => true;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => false;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => false;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => false;
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitTurnStartedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        BattleUnit updatedUnit = unit with
        {
            StatusEffects = unit.StatusEffects.RemoveAll(effect => effect.Accept(EffectExpirationVisitor.Instance, Empty.Value))
        };

        return state.WithUnit(updatedUnit) with
        {
            CurrentUnitTurn = ev.Turn,
            TurnQueue = state.TurnQueue.Remove(ev.Turn)
        };
    }
}
