using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ReduceDamageAttackResolver : BattleAbstractAttackResolver, IStaticInstance<ReduceDamageAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack)
    {
        if (target.IsRetreating() || target.IsDisabled())
        {
            return BattleCommandError.TargetIncorrect;
        }

        if (IsDamageReduced(target, GetPercent(context, attack)))
        {
            return BattleCommandError.TargetIncorrect;
        }

        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);

        if (targetDefinition.HasAnyAttackType(UnitAttackType.Damage, UnitAttackType.DrainLife, UnitAttackType.DrainLifeOverflow))
        {
            return null;
        }
        else
        {
            return BattleCommandError.TargetIncorrect;
        }
    }

    protected override IReadOnlyList<BattleEvent> OnHit(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        BattleReduceDamageStatusEffect statusEffect = new(GetPercent(context, attack), attack.IsInfinite);

        return [new BattleUnitEffectAppliedEvent(attackerId, targetId, statusEffect)];
    }

    private static bool IsDamageReduced(BattleUnit target, int percent)
    {
        return target.StatusEffects.OfType<BattleReduceDamageStatusEffect>().Any(effect => effect.Percent >= percent);
    }

    private static int GetPercent(IEngineContext context, UnitAttackDefinition attack)
    {
        return attack.Level switch
        {
            1 => context.Globals.ReduceDamagePercentLevel1,
            2 => context.Globals.ReduceDamagePercentLevel2,
            _ => throw new ArgumentOutOfRangeException(nameof(attack), attack, null)
        };
    }
}
