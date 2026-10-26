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
        if (CanIncreaseDamage(context, target, GetPercent(context, attack)))
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
        int percent = GetPercent(context, attack);

        return [new BattleUnitEffectAppliedEvent(attackerId, targetId, new BattleIncreaseDamageStatusEffect(percent, attack.IsInfinite))];
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

    private static int GetPercent(IEngineContext context, UnitAttackDefinition attack)
    {
        return attack.Level switch
        {
            1 => context.Globals.IncreaseDamagePercentLevel1,
            2 => context.Globals.IncreaseDamagePercentLevel2,
            3 => context.Globals.IncreaseDamagePercentLevel3,
            4 => context.Globals.IncreaseDamagePercentLevel4,
            _ => throw new ArgumentOutOfRangeException(nameof(attack), attack, null)
        };
    }
}
