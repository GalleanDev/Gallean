using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFogConcealedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];
        ImmutableBitmap.Builder builder = player.FogOfWar.ToBuilder();

        foreach (WorldPosition position in ev.Positions)
        {
            builder.Clear(position.X, position.Y);
        }

        WorldPlayer updatedPlayer = player with { FogOfWar = builder.ToImmutable() };

        return state with { Players = state.Players.SetItem(ev.PlayerId, updatedPlayer) };
    }
}
