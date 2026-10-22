using Avalonia.Media;
using Gallean.Common;

namespace Gallean.Client;

internal sealed record MinimapState(
    int MapWidth,
    int MapHeight,
    IReadOnlyList<WorldPosition> HiddenPositions,
    IReadOnlyList<MinimapState.Marker> Markers)
{
    public enum MarkerType
    {
        Capital,
        City,
        Stack
    }

    public sealed record Marker(
        WorldPosition Position,
        Color Color,
        MarkerType Type);

    public bool Equals(MinimapState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return MapWidth == other.MapWidth &&
               MapHeight == other.MapHeight &&
               HiddenPositions.SequenceEqual(other.HiddenPositions) &&
               Markers.SequenceEqual(other.Markers);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(MapWidth);
        hash.Add(MapHeight);
        hash.AddSequence(HiddenPositions);
        hash.AddSequence(Markers);

        return hash.ToHashCode();
    }
}
