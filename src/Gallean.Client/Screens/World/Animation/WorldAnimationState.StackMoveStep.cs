namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record StackMoveStep(WorldPosition Position, WorldStackDirection Direction, TimeSpan Start);
}
