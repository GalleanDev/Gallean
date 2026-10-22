using System.Collections.Immutable;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public abstract record Blocking(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition) : WorldEventAction(Sounds);
}
