using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitEffectAppliedEvent ev, BattleState state)
    {
        BattleUnit target = state.GetUnit(ev.TargetId);

        return state.WithUnit(target with
        {
            StatusEffects = ApplyEffect(ev.Effect, target.StatusEffects)
        });
    }

    private static ImmutableArray<BattleAbstractStatusEffect> ApplyEffect(BattleAbstractStatusEffect effect, ImmutableArray<BattleAbstractStatusEffect> effects)
    {
        return effect switch
        {
            BattleDefendStatusEffect => effects.Add(effect),
            BattleParalyzeStatusEffect paralyze => MergeDisable(effects, effects.OfType<BattleParalyzeStatusEffect>().FirstOrDefault(), paralyze),
            BattlePetrifyStatusEffect petrify => MergeDisable(effects, effects.OfType<BattlePetrifyStatusEffect>().FirstOrDefault(), petrify),
            BattlePoisonStatusEffect poison => MergeDamageOverTime(effects, effects.OfType<BattlePoisonStatusEffect>().FirstOrDefault(), poison),
            BattleFrostbiteStatusEffect frostbite => MergeDamageOverTime(effects, effects.OfType<BattleFrostbiteStatusEffect>().FirstOrDefault(), frostbite),
            BattleBlisterStatusEffect blister => MergeDamageOverTime(effects, effects.OfType<BattleBlisterStatusEffect>().FirstOrDefault(), blister),
            BattleRetreatStatusEffect => effects.Add(effect),
            BattleRetreatedStatusEffect => effects.Add(effect),
            BattleFortificationStatusEffect => effects.Add(effect),
            BattleIncreaseDamageStatusEffect increaseDamage => MergeStatModifier(effects, effects.OfType<BattleIncreaseDamageStatusEffect>().FirstOrDefault(), increaseDamage),
            BattleReduceDamageStatusEffect reduceDamage => MergeStatModifier(effects, effects.OfType<BattleReduceDamageStatusEffect>().FirstOrDefault(), reduceDamage),
            BattleReduceInitiativeStatusEffect reduceInitiative => MergeStatModifier(effects, effects.OfType<BattleReduceInitiativeStatusEffect>().FirstOrDefault(), reduceInitiative),
            BattleShatterArmorStatusEffect shatterArmor => MergeShatterArmor(effects, effects.OfType<BattleShatterArmorStatusEffect>().FirstOrDefault(), shatterArmor),
            BattleCureStatusEffect => effects.Add(effect),
            BattleTransformStatusEffect => effects.Add(effect)
        };
    }

    private static ImmutableArray<BattleAbstractStatusEffect> MergeDisable(
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

    private static ImmutableArray<BattleAbstractStatusEffect> MergeStatModifier(
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

    private static ImmutableArray<BattleAbstractStatusEffect> MergeShatterArmor(
        ImmutableArray<BattleAbstractStatusEffect> effects,
        BattleShatterArmorStatusEffect? existing,
        BattleShatterArmorStatusEffect incoming)
    {
        if (existing is null)
        {
            return effects.Add(incoming);
        }
        else
        {
            return effects.Replace(existing, existing with { Armor = existing.Armor + incoming.Armor });
        }
    }
}
