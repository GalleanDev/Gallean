namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldCityRiotTurnsDecreasedEvent ev, WorldState state)
    {
        if (state.Fortifications[ev.CityId] is not WorldCity city)
        {
            throw new InvalidOperationException($"Unexpected type of fortification '{ev.CityId}'.");
        }

        return state with
        {
            Fortifications = state.Fortifications.SetItem(city.Id, city with { RiotTurns = ev.Turns })
        };
    }
}
