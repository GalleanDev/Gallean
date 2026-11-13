using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record HotseatTurn(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        string LordName,
        string RaceName,
        AssetSpriteKey.LordFace PortraitKey) : Screen(Sounds, FocusPosition);
}
