using Gallean.Common;

namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record StackMove(WorldStackId StackId, IReadOnlyList<StackMoveStep> Steps) : WorldAnimationState
    {
        public override FocusInfo? GetFocusInfo(TimeSpan time)
        {
            foreach (StackMoveStep step in Steps)
            {
                if (time < step.Start)
                {
                    return new FocusInfo(step.Position, step.Start - time);
                }
            }

            return new FocusInfo(Steps[^1].Position, TimeSpan.Zero);
        }

        public bool Equals(StackMove? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return StackId == other.StackId &&
                   Steps.SequenceEqual(other.Steps);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(StackId);
            hash.AddSequence(Steps);

            return hash.ToHashCode();
        }
    }
}
