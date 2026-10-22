using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Client;

internal abstract partial record WorldEventAction(ImmutableArray<AssetSoundKey> Sounds)
{
    public static WorldEventAction None { get; } = new OneShot([]);

    public abstract TResult Accept<TResult>(IWorldEventActionVisitor<TResult> visitor);

    public virtual bool Equals(WorldEventAction? other)
    {
        if (other is null)
        {
            return false;
        }

        return Sounds.SequenceEqual(other.Sounds);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Sounds);

        return hash.ToHashCode();
    }
}
