using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class TurnEndExpirationVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<TurnEndExpirationVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => false;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => false;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => !effect.IsInfinite;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => !effect.IsInfinite;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => false;
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitTurnEndingEvent ev, BattleState state)
    {
        state = RemoveTurnEndEffects(state, ev.UnitId);

        return state with { WaitingQueue = state.WaitingQueue.Remove(state.CurrentUnitTurn) };
    }

    private static BattleState RemoveTurnEndEffects(BattleState state, BattleUnitId unitId)
    {
        BattleUnit? unit = state.TryGetUnit(unitId);

        if (unit is null)
        {
            return state;
        }

        ImmutableArray<BattleAbstractStatusEffect>.Builder builder = ImmutableArray.CreateBuilder<BattleAbstractStatusEffect>();

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            if (!effect.Accept(TurnEndExpirationVisitor.Instance, Empty.Value))
            {
                builder.Add(effect);
            }
        }

        return state.WithUnit(unit with { StatusEffects = builder.ToImmutable() });
    }
}
