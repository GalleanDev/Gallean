using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class RoundStartExpirationVisitor : IBattleStatusEffectVisitor<BattleRoundType, bool>, IStaticInstance<RoundStartExpirationVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattlePetrifyStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattlePoisonStatusEffect effect, BattleRoundType roundType) => roundType == BattleRoundType.Recovery;
        public bool Visit(BattleFrostbiteStatusEffect effect, BattleRoundType roundType) => roundType == BattleRoundType.Recovery;
        public bool Visit(BattleBlisterStatusEffect effect, BattleRoundType roundType) => roundType == BattleRoundType.Recovery;
        public bool Visit(BattleRetreatStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleRetreatedStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleFortificationStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleReduceDamageStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, BattleRoundType roundType) => !effect.IsInfinite;
        public bool Visit(BattleShatterArmorStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleCureStatusEffect effect, BattleRoundType roundType) => false;
        public bool Visit(BattleTransformStatusEffect effect, BattleRoundType roundType) => false;
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleRoundStartedEvent ev, BattleState state)
    {
        foreach (BattleUnitId unitId in state.GetAllUnitIds())
        {
            BattleUnit unit = state.GetUnit(unitId);

            state = state.WithUnit(unit with
            {
                StatusEffects = unit.StatusEffects.RemoveAll(effect => effect.Accept(RoundStartExpirationVisitor.Instance, ev.Round.Type))
            });
        }

        return state with
        {
            TurnQueue = ev.TurnOrder,
            WaitingQueue = [],
            CurrentRound = ev.Round
        };
    }
}
