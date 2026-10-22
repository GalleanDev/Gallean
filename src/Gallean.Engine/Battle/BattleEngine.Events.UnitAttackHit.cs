namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitAttackHitEvent ev, BattleState state)
    {
        BattleUnit target = state.GetUnit(ev.TargetId);
        BattleUnit updatedTarget = target with { Hitpoints = Math.Max(0, target.Hitpoints - ev.Damage) };

        return state.WithUnit(updatedTarget);
    }
}
