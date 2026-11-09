using Gallean.Common;

namespace Gallean.Client;

internal sealed record WorldCursor(
    WorldStackId? StackId,
    IReadOnlyList<WorldPosition> ReachablePositions,
    WorldPathSelection? Path,
    WorldPosition? HoverTile,
    WorldPosition? HoverObject,
    WorldCursorAction? Action)
{
    public static readonly WorldCursor Empty = new(null, [], null, null, null, null);

    public bool Equals(WorldCursor? other)
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
               ReachablePositions.SequenceEqual(other.ReachablePositions) &&
               Path == other.Path &&
               HoverTile == other.HoverTile &&
               HoverObject == other.HoverObject &&
               Action == other.Action;
    }

    public WorldCursor SelectStack(WorldStackId? stackId)
    {
        return this with { StackId = stackId, ReachablePositions = [], Path = null };
    }

    public WorldCursor ReconcileWith(WorldState world)
    {
        if (StackId is not null && !world.Stacks.ContainsKey(StackId.Value))
        {
            return SelectStack(null);
        }

        return this;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(StackId);
        hash.AddSequence(ReachablePositions);
        hash.Add(Path);
        hash.Add(HoverTile);
        hash.Add(HoverObject);
        hash.Add(Action);

        return hash.ToHashCode();
    }
}
