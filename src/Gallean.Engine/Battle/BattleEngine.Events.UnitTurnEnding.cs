using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class BattleEngine
{
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
            if (IsRetained(effect))
            {
                builder.Add(effect);
            }
        }

        return state.WithUnit(unit with { StatusEffects = builder.ToImmutable() });

        static bool IsRetained(BattleAbstractStatusEffect effect)
        {
            return effect switch
            {
                BattleDefendStatusEffect => true,
                BattleParalyzeStatusEffect => true,
                BattlePetrifyStatusEffect => true,
                BattlePoisonStatusEffect => true,
                BattleFrostbiteStatusEffect => true,
                BattleBlisterStatusEffect => true,
                BattleRetreatStatusEffect => true,
                BattleRetreatedStatusEffect => true,
                BattleFortificationStatusEffect => true,
                BattleIncreaseDamageStatusEffect increaseDamage => increaseDamage.IsInfinite,
                BattleReduceDamageStatusEffect reduceDamage => reduceDamage.IsInfinite,
                BattleReduceInitiativeStatusEffect => true,
                BattleShatterArmorStatusEffect => true,
                BattleCureStatusEffect => true,
                BattleTransformStatusEffect => true
            };
        }
    }
}
