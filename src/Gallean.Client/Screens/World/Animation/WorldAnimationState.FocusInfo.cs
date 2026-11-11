namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public readonly record struct FocusInfo(WorldPosition Position, TimeSpan Duration);
}
