using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record Loading(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition) : Screen(Sounds, FocusPosition);
}
