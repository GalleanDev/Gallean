using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class PlayerBuildingsCalculator
{
    public sealed record BuildingInfo(
        WorldPlayerBuildingStatus Status,
        ImmutableArray<WorldPlayerBuildingDefinitionId> LockedIds)
    {
        public bool Equals(BuildingInfo? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Status == other.Status &&
                   LockedIds.SequenceEqual(other.LockedIds);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Status);
            hash.AddSequence(LockedIds);

            return hash.ToHashCode();
        }
    }
}
