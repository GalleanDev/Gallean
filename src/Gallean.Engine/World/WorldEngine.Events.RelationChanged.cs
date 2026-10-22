namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldRelationChangedEvent ev, WorldState state)
    {
        WorldRelation relation = DiplomacyCalculator.GetRelation(state, ev.PlayerId, ev.TargetPlayerId);
        WorldRelation updatedRelation = relation with { Current = ev.Relation };

        return state with { Relations = state.Relations.SetItem(relation.Id, updatedRelation) };
    }
}
