using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ReduceDamageAttackResolver : BattleAbstractAttackResolver, IStaticInstance<ReduceDamageAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
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
        UnitAttackDefinitionId? alternativeAttackId = targetDefinition.Attack.AlternativeAttackId;
        bool dealsDamage;

        if (alternativeAttackId is null)
        {
            dealsDamage = targetDefinition.HasAnyAttackType(UnitAttackType.Damage, UnitAttackType.DrainLife, UnitAttackType.DrainLifeOverflow);
        }
        else
        {
            UnitAttackDefinition alternativeAttack = context.Definitions.GetAttack(alternativeAttackId.Value);
            dealsDamage = alternativeAttack.Type is UnitAttackType.Damage or UnitAttackType.DrainLife or UnitAttackType.DrainLifeOverflow;
        }

        if (dealsDamage)
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
