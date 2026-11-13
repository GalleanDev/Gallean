using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record ThiefAction(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldStackId StackId,
        WorldPosition TargetPosition) : Screen(Sounds, FocusPosition);
}
