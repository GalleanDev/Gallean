using Gallean.Common;

namespace Gallean.Engine;

internal sealed class TransformSelfAttackResolver : IBattleAttackResolver, IStaticInstance<TransformSelfAttackResolver>
{
    public bool EndsTurn => false;

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

        if (targetUnitId.Value != attackerId)
        {
            return BattleAttackResult.Fail(BattleCommandError.CanOnlyTransformSelf);
        }

        BattleUnit attacker = state.GetUnit(attackerId);

        if (!attacker.IsActive())
        {
            return BattleAttackResult.Fail(BattleCommandError.TargetDead);
        }

        if (attacker.HasStatusEffect<BattleTransformStatusEffect>())
        {
            return BattleAttackResult.Fail(BattleCommandError.UnitAlreadyTransformed);
        }

        UnitDefinition attackerDefinition = context.Definitions.GetUnit(attacker.DefinitionId);
        UnitDefinitionId transformDefinitionId;

        if (attackerDefinition.IsLarge)
        {
            transformDefinitionId = attack.LargeTransformUnitId ?? attack.SmallTransformUnitId!.Value;
        }
        else
        {
            transformDefinitionId = attack.SmallTransformUnitId ?? attack.LargeTransformUnitId!.Value;
        }

        BattleUnit transformed = attacker with { DefinitionId = transformDefinitionId };
        int hitpointsAfter = BattleTransformationCalculator.GetScaledHitpoints(state, context, transformed);

        return new BattleAttackResult(
            [
                new BattleUnitTransformAppliedEvent(
                    attackerId,
                    attackerId,
                    BattleTransformType.TransformSelf,
                    transformDefinitionId,
                    attacker.Level,
                    hitpointsAfter,
                    RemainingTurns: null)
            ],
            DidHit: false,
            Error: null);
    }
}
