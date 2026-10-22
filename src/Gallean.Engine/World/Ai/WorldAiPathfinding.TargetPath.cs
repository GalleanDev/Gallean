namespace Gallean.Engine;

internal sealed partial class WorldAiPathfinding
{
    public sealed record TargetPath(WorldAiTarget Target, WorldPosition Position, WorldPath Path);
}
