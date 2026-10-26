using Gallean.Common;

namespace Gallean.Engine;

internal sealed class GiveAdditionalAttackResolver : BattleAbstractAttackResolver, IStaticInstance<GiveAdditionalAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (target.Id == attackerId)
        {
            return BattleCommandError.CannotGiveAttackToSelf;
        }

        if (target.IsRetreating())
        {
            return BattleCommandError.TargetIncorrect;
        }

        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);
        if (targetDefinition.HasAttackType(UnitAttackType.GiveAdditionalAttack))
        {
            return BattleCommandError.CannotGiveAttackToTarget;
        }

        return null;
    }

    protected override IReadOnlyList<BattleEvent> OnHit(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        return [new BattleUnitAttackGrantedEvent(targetId)];
    }
}
