using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record SiteEnter(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldStackId StackId,
        WorldSiteId SiteId) : Screen(Sounds, FocusPosition);
}
