namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldAllianceStartedEvent ev, WorldState state)
    {
        WorldRelation relation = DiplomacyCalculator.GetRelation(state, ev.PlayerId, ev.TargetPlayerId);
        WorldRelation updatedRelation = relation with
        {
            Alliance = true,
            AllianceTurn = state.TurnInfo.TurnNumber,
            PermanentAlliance = ev.Permanent
        };

        return state with { Relations = state.Relations.SetItem(relation.Id, updatedRelation) };
    }
}
