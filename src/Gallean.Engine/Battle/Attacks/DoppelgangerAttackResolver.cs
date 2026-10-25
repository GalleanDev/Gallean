using Gallean.Common;

namespace Gallean.Engine;

internal sealed class DoppelgangerAttackResolver : IBattleAttackResolver, IStaticInstance<DoppelgangerAttackResolver>
{
    public BattleAttackResult Resolve(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        BattleUnitId? targetUnitId = target.GetUnitId();
        if (targetUnitId is null)
        {
            return BattleAttackResult.Fail(BattleCommandError.AttackRequiresUnitTarget);
        }

        BattleUnit attacker = state.GetUnit(attackerId);
        BattleUnit targetUnit = state.GetUnit(targetUnitId.Value);

        if (!targetUnit.IsActive())
        {
            return BattleAttackResult.Fail(BattleCommandError.TargetDead);
        }

        if (!CanTransformInto(context, targetUnit, attack))
        {
            return BattleAttackResult.Fail(BattleCommandError.DoppelgangerTargetInvalid);
        }

        UnitDefinition transformDefinition = context.Definitions.GetUnit(targetUnit.DefinitionId);
        int hitpointsAfter = BattleTransformationCalculator.GetTransformedHitpoints(
            state,
            context,
            attackerId,
            BattleTransformType.Doppelganger,
            transformDefinition.Id,
            transformDefinition.Level);

        int? remainingTurns = null;
        if (!attack.IsInfinite)
        {
            remainingTurns = context.Globals.DoppelgangerEffectDurationTurns;
        }

        return new BattleAttackResult(
            [
                new BattleUnitTransformAppliedEvent(
                    attackerId,
                    attackerId,
                    BattleTransformType.Doppelganger,
                    transformDefinition.Id,
                    transformDefinition.Level,
                    hitpointsAfter,
                    remainingTurns)
            ],
            DidHit: false,
            Error: null);
    }

    public bool CanUseAlternativeAttack(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        UnitAttackDefinition attack)
    {
        foreach (BattleUnitId unitId in state.GetAllUnitIds())
        {
            BattleUnit unit = state.GetUnit(unitId);

            if (unit.IsActive() && CanTransformInto(context, unit, attack))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanTransformInto(IEngineContext context, BattleUnit target, UnitAttackDefinition attack)
    {
        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);

        if (context.Globals.DoppelgangerBlockList.Contains(targetDefinition.Id))
        {
            return false;
        }

        UnitWard? blockingWard = BattleUnitWardCalculator.FindBlockingWard(target, attack);

        return !targetDefinition.IsLarge &&
               BattleTransformationCalculator.IsTransformableCategory(targetDefinition.Category) &&
               targetDefinition.Attack.Type != UnitAttackType.Doppelganger &&
               blockingWard is null;
    }
}
