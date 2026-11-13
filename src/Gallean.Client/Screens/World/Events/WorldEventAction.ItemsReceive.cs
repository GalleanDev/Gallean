using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record ItemsReceive(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldInventory Items) : Screen(Sounds, FocusPosition);
}
