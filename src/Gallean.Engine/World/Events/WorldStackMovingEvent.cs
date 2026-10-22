using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldStackMovingEvent(
    WorldStackId StackId,
    WorldPosition SourcePosition,
    WorldPosition TargetPosition,
    WorldStackDirection NewDirection,
    int NewMovementPoints,
    ImmutableArray<WorldPosition> RemainingPath,
    WorldPosition? EncounterPosition) : WorldEvent, IScopeBeginEvent<WorldStackMovedEvent>
{
    public WorldStackMovedEvent CreateScopeEndEvent()
    {
        return new WorldStackMovedEvent(
            StackId,
            SourcePosition,
            TargetPosition,
            NewDirection,
            NewMovementPoints,
            RemainingPath,
            EncounterPosition);
    }

    public bool Equals(WorldStackMovingEvent? other)
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
               SourcePosition == other.SourcePosition &&
               TargetPosition == other.TargetPosition &&
               NewDirection == other.NewDirection &&
               NewMovementPoints == other.NewMovementPoints &&
               RemainingPath.SequenceEqual(other.RemainingPath) &&
               EncounterPosition == other.EncounterPosition;
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(StackId);
        hash.Add(SourcePosition);
        hash.Add(TargetPosition);
        hash.Add(NewDirection);
        hash.Add(NewMovementPoints);
        hash.AddSequence(RemainingPath);
        hash.Add(EncounterPosition);

        return hash.ToHashCode();
    }
}
