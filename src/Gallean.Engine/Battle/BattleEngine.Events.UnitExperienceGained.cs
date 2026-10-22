namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitExperienceGainedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);
        BattleUnit updatedUnit = unit with
        {
            ExperienceGained = ev.ExperienceGained
        };

        return state.WithUnit(updatedUnit);
    }
}
