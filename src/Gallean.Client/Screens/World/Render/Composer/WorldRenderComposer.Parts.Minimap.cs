using System.Collections.Immutable;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Minimap
    {
        public readonly record struct Input(
            WorldMap Map,
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> Fortifications,
            ImmutableSortedDictionary<WorldStackId, WorldStack> Stacks,
            PlayerRaces PlayerRaces,
            WorldVisibility Visibility,
            WorldPlayerId ViewPlayerId,
            bool FogEnabled);

        public static MinimapState Build(Input input)
        {
            return MinimapBuilder.Build(
                input.Map,
                input.Fortifications,
                input.Stacks,
                input.PlayerRaces,
                input.Visibility,
                input.ViewPlayerId,
                input.FogEnabled);
        }
    }
}
