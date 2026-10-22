using System.Collections.Immutable;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record Battle(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldPosition Position) : Blocking(Sounds, FocusPosition)
    {
        public override TResult Accept<TResult>(IWorldEventActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
