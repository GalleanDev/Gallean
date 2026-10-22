namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record FeedbackEntry(
        TimeSpan At,
        TimeSpan Duration,
        BattleUnitState.Feedback Feedback,
        IReadOnlyList<AnimationProperty> Animation) : IBattleAnimationEntry;
}
