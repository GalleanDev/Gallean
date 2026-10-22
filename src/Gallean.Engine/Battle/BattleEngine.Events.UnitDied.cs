using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class DeathRetainedEffectVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<DeathRetainedEffectVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => false;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => true;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => true;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => true;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => true;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => true;
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitDiedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with
        {
            StatusEffects = unit.StatusEffects.RemoveAll(effect => !effect.Accept(DeathRetainedEffectVisitor.Instance, Empty.Value))
        });
    }
}
