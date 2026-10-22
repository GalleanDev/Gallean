namespace Gallean.Engine;

public sealed class WorldCommandException(WorldCommandError error)
    : Exception($"Command failed with error '{error}'.")
{
    public WorldCommandError Error { get; } = error;
}
