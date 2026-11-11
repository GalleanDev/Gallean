namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record MountainRemove(WorldPosition Center, int Radius) : WorldAnimationState;
}
