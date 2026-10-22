using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class CureRetainedEffectVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<CureRetainedEffectVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => true;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => false;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => true;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => true;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => true;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => true;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => true;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => !effect.IsCurable();
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitCuredEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        ImmutableArray<BattleAbstractStatusEffect>.Builder builder = ImmutableArray.CreateBuilder<BattleAbstractStatusEffect>();

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            if (effect.Accept(CureRetainedEffectVisitor.Instance, Empty.Value))
            {
                builder.Add(effect);
            }
        }

        builder.Add(new BattleCureStatusEffect());

        return state.WithUnit(unit with { StatusEffects = builder.ToImmutable() });
    }
}
