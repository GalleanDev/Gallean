using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ReviveAttackResolver : BattleAbstractAttackResolver, IStaticInstance<ReviveAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack)
    {
        if (target.IsRevived)
        {
            return BattleCommandError.TargetAlreadyRevived;
        }

        if (GetHitpointsAfter(state, context, target, attack) <= 0)
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

        return [new BattleUnitRevivedEvent(targetId, GetHitpointsAfter(state, context, target, attack))];
    }

    private static int GetHitpointsAfter(BattleState state, IEngineContext context, BattleUnit target, UnitAttackDefinition attack)
    {
        int maxHitpoints = context.UnitStats.GetBattleUnitStats(state, target.Id).MaxHitpoints;

        return Math.Min(maxHitpoints * attack.Heal / 100, maxHitpoints);
    }
}
