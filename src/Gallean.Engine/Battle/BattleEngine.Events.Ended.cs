using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class BattleEndRetainedEffectVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<BattleEndRetainedEffectVisitor>
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
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => true;
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleEndedEvent ev, BattleState state)
    {
        foreach (BattleUnitId unitId in state.GetAllUnitIds())
        {
            BattleUnit unit = state.GetUnit(unitId);

            state = state.WithUnit(unit with
            {
                StatusEffects = unit.StatusEffects.RemoveAll(effect => !effect.Accept(BattleEndRetainedEffectVisitor.Instance, Empty.Value))
            });
        }

        return state with { Outcome = new BattleOutcome(ev.WinningSide) };
    }
}
