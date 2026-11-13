namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationCapturingEvent ev, WorldState state)
    {
        WorldFortification fortification = state.Fortifications[ev.FortificationId];

        return fortification switch
        {
            WorldCapital capital => CaptureCapital(state, ev, capital),
            WorldCity city => CaptureCity(state, ev, city),
            WorldRuin => state
        };
    }

    private static WorldState CaptureCapital(WorldState state, WorldFortificationCapturingEvent ev, WorldCapital capital)
    {
        WorldStack capturingStack = state.Stacks[ev.CapturingStackId];

        WorldStack updatedStack = capturingStack with
        {
            Inventory = capturingStack.Inventory.AddRange(capital.Inventory.Items)
        };

        WorldCapital updatedCapital = capital with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            Inventory = WorldInventory.Empty,
            CapturedById = ev.NewOwnerId
        };

        return state with
        {
            Fortifications = state.Fortifications.SetItem(capital.Id, updatedCapital),
            Stacks = state.Stacks.SetItem(updatedStack.Id, updatedStack)
        };
    }

    private WorldState CaptureCity(WorldState state, WorldFortificationCapturingEvent ev, WorldCity city)
    {
        WorldStack capturingStack = state.Stacks[ev.CapturingStackId];
        WorldMapTerrain terrain = WorldHelpers.GetNativeTerrain(state.Players[ev.NewOwnerId].Race);

        WorldCity updatedCity = city with
        {
            OwnerId = ev.NewOwnerId,
            SubraceId = capturingStack.SubraceId,
            Garrison = UnitGrid<WorldUnit>.Empty
        };

        int growth = FortificationCalculator.GetCaptureTerrainGrowth(city, Context.Globals);
        WorldMap updatedMap = ExpansionCalculator.ExpandFortificationTerrain(
            state,
            Context,
            city,
            terrain,
            growth);

        return state with
        {
            Fortifications = state.Fortifications.SetItem(city.Id, updatedCity),
            Map = updatedMap
        };
    }
}
