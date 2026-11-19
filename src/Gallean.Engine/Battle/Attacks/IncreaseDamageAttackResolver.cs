using Gallean.Common;

namespace Gallean.Engine;

internal sealed class IncreaseDamageAttackResolver : BattleAbstractAttackResolver, IStaticInstance<IncreaseDamageAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (CanIncreaseDamage(context, target, attackStats.IncreaseDamagePercent))
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
        BattleIncreaseDamageStatusEffect statusEffect = new(attackStats.IncreaseDamagePercent, attack.IsInfinite);

        return [new BattleUnitEffectAppliedEvent(attackerId, targetId, statusEffect)];
    }

    private static bool CanIncreaseDamage(IEngineContext context, BattleUnit target, int percent)
    {
        if (target.IsRetreating() || target.IsDisabled())
        {
            return false;
        }

        if (IsDamageIncreased(target, percent))
        {
            return false;
        }

        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);

        return targetDefinition.HasAnyAttackType(UnitAttackType.Damage, UnitAttackType.DrainLife, UnitAttackType.DrainLifeOverflow);
    }

    private static bool IsDamageIncreased(BattleUnit target, int percent)
    {
        return target.StatusEffects.OfType<BattleIncreaseDamageStatusEffect>().Any(effect => effect.Percent >= percent);
    }
}
