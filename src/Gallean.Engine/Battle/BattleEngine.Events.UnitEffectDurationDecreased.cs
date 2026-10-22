using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class EffectDurationDecreaseVisitor : IBattleStatusEffectVisitor<Empty, BattleAbstractStatusEffect>, IStaticInstance<EffectDurationDecreaseVisitor>
    {
        public BattleAbstractStatusEffect Visit(BattleDefendStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleParalyzeStatusEffect effect, Empty context) => Decrease(effect);
        public BattleAbstractStatusEffect Visit(BattlePetrifyStatusEffect effect, Empty context) => Decrease(effect);
        public BattleAbstractStatusEffect Visit(BattlePoisonStatusEffect effect, Empty context) => Decrease(effect);
        public BattleAbstractStatusEffect Visit(BattleFrostbiteStatusEffect effect, Empty context) => Decrease(effect);
        public BattleAbstractStatusEffect Visit(BattleBlisterStatusEffect effect, Empty context) => Decrease(effect);
        public BattleAbstractStatusEffect Visit(BattleRetreatStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleRetreatedStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleFortificationStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleReduceDamageStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleShatterArmorStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleCureStatusEffect effect, Empty context) => effect;
        public BattleAbstractStatusEffect Visit(BattleTransformStatusEffect effect, Empty context) => Decrease(effect);

        private static BattleAbstractStatusEffect Decrease(BattleAbstractDisableEffect effect)
        {
            return effect with { RemainingTurns = effect.RemainingTurns - 1 };
        }

        private static BattleAbstractStatusEffect Decrease(BattleAbstractDamageOverTimeEffect effect)
        {
            return effect with { RemainingTurns = effect.RemainingTurns - 1 };
        }

        private static BattleAbstractStatusEffect Decrease(BattleTransformStatusEffect effect)
        {
            if (effect.RemainingTurns is null)
            {
                return effect;
            }
            else
            {
                return effect with { RemainingTurns = effect.RemainingTurns - 1 };
            }
        }
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitEffectDurationDecreasedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        ImmutableArray<BattleAbstractStatusEffect>.Builder builder = ImmutableArray.CreateBuilder<BattleAbstractStatusEffect>();

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            builder.Add(effect.Accept(EffectDurationDecreaseVisitor.Instance, Empty.Value));
        }

        return state.WithUnit(unit with { StatusEffects = builder.ToImmutable() });
    }
}
