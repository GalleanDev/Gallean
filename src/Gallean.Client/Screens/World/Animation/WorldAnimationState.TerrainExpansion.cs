namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record TerrainExpansion(WorldMap OldMap) : WorldAnimationState;
}
