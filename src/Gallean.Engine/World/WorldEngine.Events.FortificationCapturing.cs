using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class FortificationCaptureResolver :
        IWorldFortificationVisitor<FortificationCaptureResolver.Context, WorldState>,
        IStaticInstance<FortificationCaptureResolver>
    {
        public sealed record Context(WorldState State, WorldPlayerId NewOwnerId, WorldStackId CapturingStackId, IEngineContext EngineContext);

        public WorldState Visit(WorldCapital capital, Context context)
        {
            WorldStack capturingStack = context.State.Stacks[context.CapturingStackId];

            WorldStack updatedStack = capturingStack with
            {
                Inventory = capturingStack.Inventory.AddRange(capital.Inventory.Items)
            };

            WorldCapital updatedCapital = capital with
            {
                Garrison = UnitGrid<WorldUnit>.Empty,
                Inventory = WorldInventory.Empty,
                CapturedById = context.NewOwnerId
            };

            return context.State with
            {
                Fortifications = context.State.Fortifications.SetItem(capital.Id, updatedCapital),
                Stacks = context.State.Stacks.SetItem(updatedStack.Id, updatedStack)
            };
        }

        public WorldState Visit(WorldCity city, Context context)
        {
            WorldStack capturingStack = context.State.Stacks[context.CapturingStackId];
            WorldMapTerrain terrain = WorldHelpers.GetNativeTerrain(context.State.Players[context.NewOwnerId].Race);

            WorldCity updatedCity = city with
            {
                OwnerId = context.NewOwnerId,
                SubraceId = capturingStack.SubraceId,
                Garrison = UnitGrid<WorldUnit>.Empty
            };

            int growth = FortificationCalculator.GetCaptureTerrainGrowth(city, context.EngineContext.Globals);
            WorldMap updatedMap = ExpansionCalculator.ExpandFortificationTerrain(
                context.State,
                context.EngineContext,
                city,
                terrain,
                growth);

            return context.State with
            {
                Fortifications = context.State.Fortifications.SetItem(city.Id, updatedCity),
                Map = updatedMap
            };
        }

        public WorldState Visit(WorldRuin ruin, Context context)
        {
            return context.State;
        }
    }

    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationCapturingEvent ev, WorldState state)
    {
        WorldFortification fortification = state.Fortifications[ev.FortificationId];
        FortificationCaptureResolver.Context context = new(state, ev.NewOwnerId, ev.CapturingStackId, Context);

        return fortification.Accept(FortificationCaptureResolver.Instance, context);
    }
}
