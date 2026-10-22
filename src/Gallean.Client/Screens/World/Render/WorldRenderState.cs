using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record WorldRenderState(
    WorldRenderState.MapTerrain Terrain,
    IReadOnlyList<WorldRenderState.MapObject> MapObjects,
    IReadOnlyList<WorldRenderState.MapObject> Stacks,
    WorldRenderState.WorldAnimation? Animation,
    IReadOnlyList<WorldRenderState.Cloud> Clouds,
    IReadOnlyList<WorldRenderState.FogTile> FogTiles,
    IReadOnlyList<WorldRenderState.ObjectiveMarker> ObjectiveMarkers,
    IReadOnlyList<WorldRenderState.AmbientSound> AmbientSounds,
    IReadOnlyList<WorldRenderState.Cursor> Cursors,
    WorldRenderState.ReachOverlay? Reach,
    WorldRenderState.HighlightOverlay Highlights,
    WorldRenderState.PathOverlay? SelectedPath,
    MinimapState Minimap)
{
    public bool Equals(WorldRenderState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Terrain == other.Terrain &&
               MapObjects.SequenceEqual(other.MapObjects) &&
               Stacks.SequenceEqual(other.Stacks) &&
               Animation == other.Animation &&
               Clouds.SequenceEqual(other.Clouds) &&
               FogTiles.SequenceEqual(other.FogTiles) &&
               ObjectiveMarkers.SequenceEqual(other.ObjectiveMarkers) &&
               AmbientSounds.SequenceEqual(other.AmbientSounds) &&
               Cursors.SequenceEqual(other.Cursors) &&
               Reach == other.Reach &&
               Highlights == other.Highlights &&
               SelectedPath == other.SelectedPath &&
               Minimap == other.Minimap;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Terrain);
        hash.AddSequence(MapObjects);
        hash.AddSequence(Stacks);
        hash.Add(Animation);
        hash.AddSequence(Clouds);
        hash.AddSequence(FogTiles);
        hash.AddSequence(ObjectiveMarkers);
        hash.AddSequence(AmbientSounds);
        hash.AddSequence(Cursors);
        hash.Add(Reach);
        hash.Add(Highlights);
        hash.Add(SelectedPath);
        hash.Add(Minimap);

        return hash.ToHashCode();
    }
}
