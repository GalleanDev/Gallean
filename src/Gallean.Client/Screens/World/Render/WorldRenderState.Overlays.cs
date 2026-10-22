using Avalonia.Media;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record WorldRenderState
{
    public sealed record Cloud(
        AssetAnimationKey AssetKey,
        int FrameOffset,
        double HeightFraction,
        double PeriodSeconds,
        double Phase,
        bool LeftToRight);

    public sealed record FogTile(WorldPosition Position);

    public sealed record ObjectiveMarker(WorldFootprint Footprint, bool HasRunes);

    public sealed record PathStep(
        WorldPosition Position,
        Color InnerColor,
        Color OuterColor,
        string CostLabel);

    public sealed record ReachOverlay(IReadOnlyList<WorldPosition> Tiles)
    {
        public bool Equals(ReachOverlay? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Tiles.SequenceEqual(other.Tiles);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Tiles);

            return hash.ToHashCode();
        }
    }

    public sealed record Highlight(MapObject Target, Color Color);

    public sealed record HighlightOverlay(IReadOnlyList<Highlight> Items)
    {
        public bool Equals(HighlightOverlay? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Items.SequenceEqual(other.Items);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Items);

            return hash.ToHashCode();
        }
    }

    public sealed record PathOverlay(WorldPosition Origin, IReadOnlyList<PathStep> Steps)
    {
        public bool Equals(PathOverlay? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Origin == other.Origin && Steps.SequenceEqual(other.Steps);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Origin);
            hash.AddSequence(Steps);

            return hash.ToHashCode();
        }
    }
}
