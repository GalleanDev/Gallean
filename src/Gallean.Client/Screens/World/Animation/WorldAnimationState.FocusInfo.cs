namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public readonly record struct FocusInfo(WorldPosition Position, TimeSpan Duration);
}
