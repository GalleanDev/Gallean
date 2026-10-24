using Gallean.Common;

namespace Gallean.Engine;

internal sealed class TransformOtherAttackResolver : BattleAbstractAttackResolver, IStaticInstance<TransformOtherAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack)
    {
        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);

        if (!BattleTransformationCalculator.IsTransformableCategory(targetDefinition.Category))
        {
            return BattleCommandError.TargetIncorrect;
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
        BattleUnit target = state.GetUnit(targetId);
        UnitDefinition targetDefinition = context.Definitions.GetUnit(target.DefinitionId);
        UnitDefinitionId transformDefinitionId;

        if (targetDefinition.IsLarge)
        {
            transformDefinitionId = attack.LargeTransformUnitId!.Value;
        }
        else
        {
            transformDefinitionId = attack.SmallTransformUnitId!.Value;
        }

        BattleUnit transformed = target with { DefinitionId = transformDefinitionId };
        int hitpointsAfter = BattleTransformationCalculator.GetScaledHitpoints(state, context, transformed);

        int? remainingTurns = null;
        if (attack.IsInfinite)
        {
            remainingTurns = context.Random.Next(
                context.Globals.TransformEffectMinDurationTurns,
                context.Globals.TransformEffectMaxDurationTurns + 1);
        }

        return
        [
            new BattleUnitTransformAppliedEvent(
                attackerId,
                targetId,
                BattleTransformType.Transform,
                transformDefinitionId,
                target.Level,
                hitpointsAfter,
                remainingTurns)
        ];
    }
}
