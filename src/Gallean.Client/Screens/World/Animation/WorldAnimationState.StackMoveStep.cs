namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record StackMoveStep(WorldPosition Position, WorldStackDirection Direction, TimeSpan Start);
}
