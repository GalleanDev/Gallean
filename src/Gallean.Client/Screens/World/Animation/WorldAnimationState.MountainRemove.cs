namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record MountainRemove(WorldPosition Center, int Radius) : WorldAnimationState;
}
