using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

[method: JsonConstructor]
public sealed record WorldAiMemory(
    WorldAiTurnMemory Turn,
    ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory> Stacks)
{
    public WorldAiMemory(WorldAiTurnMemory turn)
        : this(turn, ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty)
    {
    }

    public static WorldAiMemory Empty { get; } = new(
        new WorldAiTurnMemory(0, default),
        ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty);

    public bool Equals(WorldAiMemory? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Turn == other.Turn &&
               Stacks.SequenceEqual(other.Stacks);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Turn);
        hash.AddSequence(Stacks);

        return hash.ToHashCode();
    }
}
