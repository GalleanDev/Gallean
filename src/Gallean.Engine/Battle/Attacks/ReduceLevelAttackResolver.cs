using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ReduceLevelAttackResolver : BattleAbstractAttackResolver, IStaticInstance<ReduceLevelAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (target.IsRetreating())
        {
            return BattleCommandError.TargetIncorrect;
        }

        if (GetReducedUnit(context, target) is null)
        {
            return BattleCommandError.TargetAtMinimumLevel;
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

        (UnitDefinitionId DefinitionId, int Level)? unit = GetReducedUnit(context, target);

        if (unit is not null)
        {
            (UnitDefinitionId definitionId, int level) = unit.Value;
            int hitpointsAfter = BattleTransformationCalculator.GetTransformedHitpoints(
                state,
                context,
                targetId,
                BattleTransformType.LevelDrain,
                definitionId,
                level);

            int? remainingTurns = null;
            if (!attack.IsInfinite)
            {
                remainingTurns = context.Globals.ReduceLevelEffectDurationTurns;
            }

            return
            [
                new BattleUnitTransformAppliedEvent(
                    attackerId,
                    targetId,
                    BattleTransformType.LevelDrain,
                    definitionId,
                    level,
                    hitpointsAfter,
                    remainingTurns)
            ];
        }

        return [];
    }

    private static (UnitDefinitionId DefinitionId, int Level)? GetReducedUnit(IEngineContext context, BattleUnit target)
    {
        if (target.Level <= 1)
        {
            return null;
        }

        UnitDefinition definition = context.Definitions.GetUnit(target.DefinitionId);

        if (target.Level > definition.Level)
        {
            return (target.DefinitionId, target.Level - 1);
        }

        if (definition.PreviousUnitId is null)
        {
            return null;
        }

        UnitDefinition previousDefinition = context.Definitions.GetUnit(definition.PreviousUnitId.Value);

        return (previousDefinition.Id, previousDefinition.Level);
    }
}
