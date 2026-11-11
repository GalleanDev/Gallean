using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public closed record Blocking(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition) : WorldEventAction(Sounds);
}
