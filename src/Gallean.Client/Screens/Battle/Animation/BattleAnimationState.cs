using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record BattleAnimationState(
    AnimationSequence Sequence,
    IReadOnlyList<BattleAnimationState.Unit> Units,
    IReadOnlyList<BattleAnimationState.EffectEntry> Effects,
    IReadOnlyList<AssetAnimationKey> Animations,
    IReadOnlyList<BattleAnimationState.ShakeEntry> Shakes,
    AnimationRate Rate)
{
    public bool Equals(BattleAnimationState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Sequence == other.Sequence &&
               Units.SequenceEqual(other.Units) &&
               Effects.SequenceEqual(other.Effects) &&
               Animations.SequenceEqual(other.Animations) &&
               Shakes.SequenceEqual(other.Shakes) &&
               Rate == other.Rate;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Sequence);
        hash.AddSequence(Units);
        hash.AddSequence(Effects);
        hash.AddSequence(Animations);
        hash.AddSequence(Shakes);
        hash.Add(Rate);

        return hash.ToHashCode();
    }
}
