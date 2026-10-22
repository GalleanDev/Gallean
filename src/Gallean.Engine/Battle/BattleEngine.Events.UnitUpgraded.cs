namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitUpgradedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);
        BattleUnit upgradedUnit = unit with
        {
            DefinitionId = ev.ToDefinitionId,
            Level = ev.Level,
            Hitpoints = ev.Hitpoints,
            Experience = 0,
            ExperienceGained = 0
        };

        return state.WithUnit(upgradedUnit);
    }
}
