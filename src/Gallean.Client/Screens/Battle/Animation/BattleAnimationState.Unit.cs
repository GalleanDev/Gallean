using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record Unit(
        BattleUnitId Id,
        IReadOnlyList<BodyEntry> Body,
        IReadOnlyList<FormEntry> Form,
        IReadOnlyList<FeedbackEntry> Feedback,
        IReadOnlyList<MotionEntry> Motion,
        IReadOnlyList<StatusEntry> Status)
    {
        public bool Equals(Unit? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id &&
                   Body.SequenceEqual(other.Body) &&
                   Form.SequenceEqual(other.Form) &&
                   Feedback.SequenceEqual(other.Feedback) &&
                   Motion.SequenceEqual(other.Motion) &&
                   Status.SequenceEqual(other.Status);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Id);
            hash.AddSequence(Body);
            hash.AddSequence(Form);
            hash.AddSequence(Feedback);
            hash.AddSequence(Motion);
            hash.AddSequence(Status);

            return hash.ToHashCode();
        }
    }
}
