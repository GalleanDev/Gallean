using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Client;

internal abstract partial record WorldEventAction
{
    public sealed record ScenarioPopup(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        ImmutableArray<ScenarioEventPopupMessage> Messages) : Screen(Sounds, FocusPosition)
    {
        public bool Equals(ScenarioPopup? other)
        {
            return base.Equals(other) &&
                   Messages.SequenceEqual(other.Messages);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(base.GetHashCode());
            hash.AddSequence(Messages);

            return hash.ToHashCode();
        }

        public override TResult Accept<TResult>(IWorldEventScreenVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
