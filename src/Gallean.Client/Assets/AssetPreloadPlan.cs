using Gallean.Common;

namespace Gallean.Client;

internal sealed record AssetPreloadPlan(
    IReadOnlyList<AssetPreloadItem> InitialRequired,
    IReadOnlyList<AssetPreloadItem> InitialOptional,
    IReadOnlyList<AssetPreloadItem> Remaining)
{
    public bool Equals(AssetPreloadPlan? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return InitialRequired.SequenceEqual(other.InitialRequired) &&
               InitialOptional.SequenceEqual(other.InitialOptional) &&
               Remaining.SequenceEqual(other.Remaining);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(InitialRequired);
        hash.AddSequence(InitialOptional);
        hash.AddSequence(Remaining);

        return hash.ToHashCode();
    }
}
