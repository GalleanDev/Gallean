using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public closed record Screen(
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
