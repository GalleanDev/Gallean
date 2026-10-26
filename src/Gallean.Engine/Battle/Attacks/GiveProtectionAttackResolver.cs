using Gallean.Common;

namespace Gallean.Engine;

internal sealed class GiveProtectionAttackResolver : BattleAbstractAttackResolver, IStaticInstance<GiveProtectionAttackResolver>
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

        IReadOnlyList<BattleUnitWardModifier> wards =
            BattleUnitWardCalculator.GetGrantableWards(target, attackerId, attack, context.Definitions);

        if (wards.Count > 0)
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
        BattleUnit target = state.GetUnit(targetId);
        List<BattleEvent> events = [];

        foreach (BattleUnitWardModifier ward in BattleUnitWardCalculator.GetGrantableWards(target, attackerId, attack, context.Definitions))
        {
            events.Add(new BattleUnitWardGrantedEvent(targetId, ward));
        }

        return events;
    }
}
