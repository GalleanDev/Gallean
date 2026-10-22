using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class EffectApplyVisitor : IBattleStatusEffectVisitor<ImmutableArray<BattleAbstractStatusEffect>, ImmutableArray<BattleAbstractStatusEffect>>, IStaticInstance<EffectApplyVisitor>
    {
        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleDefendStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return effects.Add(effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleParalyzeStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeEffects(effects, effects.OfType<BattleParalyzeStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattlePetrifyStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeEffects(effects, effects.OfType<BattlePetrifyStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattlePoisonStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeDamageOverTime(effects, effects.OfType<BattlePoisonStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleFrostbiteStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeDamageOverTime(effects, effects.OfType<BattleFrostbiteStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleBlisterStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeDamageOverTime(effects, effects.OfType<BattleBlisterStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleRetreatStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return effects.Add(effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleRetreatedStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return effects.Add(effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleFortificationStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return effects.Add(effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleIncreaseDamageStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeStatModifiers(effects, effects.OfType<BattleIncreaseDamageStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleReduceDamageStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeStatModifiers(effects, effects.OfType<BattleReduceDamageStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleReduceInitiativeStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return MergeStatModifiers(effects, effects.OfType<BattleReduceInitiativeStatusEffect>().FirstOrDefault(), effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleShatterArmorStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            BattleShatterArmorStatusEffect? existing = effects.OfType<BattleShatterArmorStatusEffect>().FirstOrDefault();

            if (existing is null)
            {
                return effects.Add(effect);
            }
            else
            {
                return effects.Replace(existing, existing with { Armor = existing.Armor + effect.Armor });
            }
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleCureStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return effects.Add(effect);
        }

        public ImmutableArray<BattleAbstractStatusEffect> Visit(BattleTransformStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
        {
            return effects.Add(effect);
        }

        private static ImmutableArray<BattleAbstractStatusEffect> MergeEffects(
            ImmutableArray<BattleAbstractStatusEffect> effects,
            BattleAbstractDisableEffect? existing,
            BattleAbstractDisableEffect incoming)
        {
            if (existing is null)
            {
                return effects.Add(incoming);
            }
            else if (existing.RemainingTurns >= incoming.RemainingTurns)
            {
                return effects;
            }
            else
            {
                return effects.Replace(existing, incoming);
            }
        }

        private static ImmutableArray<BattleAbstractStatusEffect> MergeStatModifiers(
            ImmutableArray<BattleAbstractStatusEffect> effects,
            BattleAbstractStatModifierEffect? existing,
            BattleAbstractStatModifierEffect incoming)
        {
            if (existing is null)
            {
                return effects.Add(incoming);
            }
            else if (existing.Percent > incoming.Percent)
            {
                return effects;
            }
            else if (existing.Percent == incoming.Percent && (existing.IsInfinite || !incoming.IsInfinite))
            {
                return effects;
            }
            else
            {
                return effects.Replace(existing, incoming);
            }
        }

        private static ImmutableArray<BattleAbstractStatusEffect> MergeDamageOverTime(
            ImmutableArray<BattleAbstractStatusEffect> effects,
            BattleAbstractDamageOverTimeEffect? existing,
            BattleAbstractDamageOverTimeEffect incoming)
        {
            if (existing is null)
            {
                return effects.Add(incoming);
            }
            else if (existing.Damage > incoming.Damage)
            {
                return effects;
            }
            else if (existing.Damage == incoming.Damage && existing.RemainingTurns >= incoming.RemainingTurns)
            {
                return effects;
            }
            else
            {
                return effects.Replace(existing, incoming);
            }
        }
    }

    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitEffectAppliedEvent ev, BattleState state)
    {
        BattleUnit target = state.GetUnit(ev.TargetId);

        return state.WithUnit(target with
        {
            StatusEffects = ev.Effect.Accept(EffectApplyVisitor.Instance, target.StatusEffects)
        });
    }
}
