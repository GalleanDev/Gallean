using Gallean.Common;

namespace Gallean.Client;

internal sealed record BattleFeedbackViewModel(
    BattleUnitState.FeedbackType Type,
    string Text,
    IReadOnlyList<AnimationProperty> Animation)
{
    public bool Equals(BattleFeedbackViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type == other.Type &&
               Text == other.Text &&
               Animation.SequenceEqual(other.Animation);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Type);
        hash.Add(Text);
        hash.AddSequence(Animation);

        return hash.ToHashCode();
    }
}
