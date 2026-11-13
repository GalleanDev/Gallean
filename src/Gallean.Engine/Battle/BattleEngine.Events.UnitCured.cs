using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitCuredEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        ImmutableArray<BattleAbstractStatusEffect>.Builder builder = ImmutableArray.CreateBuilder<BattleAbstractStatusEffect>();

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            if (IsRetained(effect))
            {
                builder.Add(effect);
            }
        }

        builder.Add(new BattleCureStatusEffect());

        return state.WithUnit(unit with { StatusEffects = builder.ToImmutable() });

        static bool IsRetained(BattleAbstractStatusEffect effect)
        {
            return effect switch
            {
                BattleDefendStatusEffect => true,
                BattleParalyzeStatusEffect => false,
                BattlePetrifyStatusEffect => false,
                BattlePoisonStatusEffect => false,
                BattleFrostbiteStatusEffect => false,
                BattleBlisterStatusEffect => false,
                BattleRetreatStatusEffect => true,
                BattleRetreatedStatusEffect => true,
                BattleFortificationStatusEffect => true,
                BattleIncreaseDamageStatusEffect => true,
                BattleReduceDamageStatusEffect => false,
                BattleReduceInitiativeStatusEffect => false,
                BattleShatterArmorStatusEffect => true,
                BattleCureStatusEffect => false,
                BattleTransformStatusEffect transform => !transform.IsCurable()
            };
        }
    }
}
