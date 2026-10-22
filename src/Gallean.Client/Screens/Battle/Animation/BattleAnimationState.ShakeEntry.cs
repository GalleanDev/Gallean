namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record ShakeEntry(
        TimeSpan At,
        TimeSpan Duration,
        double Amplitude) : IBattleAnimationEntry;
}
