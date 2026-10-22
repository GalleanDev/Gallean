using System.Collections.Immutable;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public abstract record Screen(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition) : Blocking(Sounds, FocusPosition)
    {
        public sealed override TResult Accept<TResult>(IWorldEventActionVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }

        public abstract TResult Accept<TResult>(IWorldEventScreenVisitor<TResult> visitor);
    }
}
