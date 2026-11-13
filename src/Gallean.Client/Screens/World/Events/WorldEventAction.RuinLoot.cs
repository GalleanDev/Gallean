using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record RuinLoot(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldResourceSet Resources,
        WorldItem? Item) : Screen(Sounds, FocusPosition);
}
