using Avalonia;

namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record MotionEntry(
        TimeSpan At,
        TimeSpan Duration,
        Vector Direction,
        double Distance) : IBattleAnimationEntry;
}
