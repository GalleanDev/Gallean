using Gallean.Common;

namespace Gallean.Engine;

internal sealed class HealAttackResolver : BattleAbstractAttackResolver, IStaticInstance<HealAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (GetNewHitpoints(state, context, target, attackStats) <= target.Hitpoints)
        {
            return BattleCommandError.TargetNotHealable;
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

        return [new BattleUnitHealedEvent(targetId, GetNewHitpoints(state, context, target, attackStats))];
    }

    private static int GetNewHitpoints(BattleState state, IEngineContext context, BattleUnit target, UnitAttackStats attackStats)
    {
        int maxHitpoints = context.UnitStats.GetBattleUnitStats(state, target.Id).MaxHitpoints;

        return Math.Min(target.Hitpoints + attackStats.Heal, maxHitpoints);
    }
}
