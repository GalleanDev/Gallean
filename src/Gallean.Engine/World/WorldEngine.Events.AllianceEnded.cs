namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldAllianceEndedEvent ev, WorldState state)
    {
        WorldRelation relation = DiplomacyCalculator.GetRelation(state, ev.PlayerId, ev.TargetPlayerId);
        WorldRelation updatedRelation = relation with { Alliance = false, AllianceTurn = 0 };

        return state with { Relations = state.Relations.SetItem(relation.Id, updatedRelation) };
    }
}
