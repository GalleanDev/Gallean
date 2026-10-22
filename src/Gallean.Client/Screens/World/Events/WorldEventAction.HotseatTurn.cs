using System.Collections.Immutable;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record HotseatTurn(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        string LordName,
        string RaceName,
        AssetSpriteKey.LordFace PortraitKey) : Screen(Sounds, FocusPosition)
    {
        public override TResult Accept<TResult>(IWorldEventScreenVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
