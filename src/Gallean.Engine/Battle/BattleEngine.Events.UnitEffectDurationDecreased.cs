using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitEffectDurationDecreasedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        ImmutableArray<BattleAbstractStatusEffect>.Builder builder = ImmutableArray.CreateBuilder<BattleAbstractStatusEffect>();

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            builder.Add(DecreaseDuration(effect));
        }

        return state.WithUnit(unit with { StatusEffects = builder.ToImmutable() });
    }

    private static BattleAbstractStatusEffect DecreaseDuration(BattleAbstractStatusEffect effect)
    {
        return effect switch
        {
            BattleDefendStatusEffect => effect,
            BattleParalyzeStatusEffect paralyze => DecreaseTurns(paralyze),
            BattlePetrifyStatusEffect petrify => DecreaseTurns(petrify),
            BattlePoisonStatusEffect poison => DecreaseTurns(poison),
            BattleFrostbiteStatusEffect frostbite => DecreaseTurns(frostbite),
            BattleBlisterStatusEffect blister => DecreaseTurns(blister),
            BattleRetreatStatusEffect => effect,
            BattleRetreatedStatusEffect => effect,
            BattleFortificationStatusEffect => effect,
            BattleIncreaseDamageStatusEffect => effect,
            BattleReduceDamageStatusEffect => effect,
            BattleReduceInitiativeStatusEffect => effect,
            BattleShatterArmorStatusEffect => effect,
            BattleCureStatusEffect => effect,
            BattleTransformStatusEffect transform => DecreaseTurns(transform)
        };
    }

    private static BattleAbstractStatusEffect DecreaseTurns(BattleAbstractDisableEffect effect)
    {
        return effect with { RemainingTurns = effect.RemainingTurns - 1 };
    }

    private static BattleAbstractStatusEffect DecreaseTurns(BattleAbstractDamageOverTimeEffect effect)
    {
        return effect with { RemainingTurns = effect.RemainingTurns - 1 };
    }

    private static BattleAbstractStatusEffect DecreaseTurns(BattleTransformStatusEffect effect)
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
